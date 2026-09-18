# [AGENTS.md](http://AGENTS.md)

面向后续在本仓库工作的编码助手。

## 项目要点

- **Astrox.Blog**：ASP.NET Core 10（`net10.0`）Razor Pages + SQLite（EF Core）+ Identity（单所有者）+ Markdig + Minimal API（`/api/posts`）。
- 公开读者只读已发布文章；写操作需 Cookie 登录（`/Admin`）或 Bearer API Key。
- **zip 导入**：`POST /api/posts/from-zip`（multipart 字段 `file`）解压后 `.md` 入库，图片写入站点外 `Blog:MediaRoot`（生产默认 `D:/IIS/astrox-blog-media`），经 `/media/{包名}/` 提供；相对图片路径会改写。部署清空站点目录不影响该媒体目录。
- 用户说「将 Docs 下某子文件夹上传到阿里云」时，按下方「Docs 子文件夹上传到阿里云」立即执行，不要只给步骤说明。
- 中文 UI 文案为主；视觉为深色青霓虹科技风（`wwwroot/css/site.css`）。
- SEO：`/robots.txt`、`/sitemap.xml`、canonical / OG / Twitter / JSON-LD（`Blog:PublicBaseUrl`）。
- 外部集成（可选）：Umami（`Blog:Umami*`）；未配置则不注入。与站内访问计数无关。
- **站内评论**：`Comment` 实体写入 SQLite；提交即公开、无审核；防刷见 `CommentAntiSpamService`（限流 / 蜜罐 / 最短填写时间 / 正文限制）。所有者可在文章页软删。无 Giscus / 第三方评论 SaaS。
- **站内访问统计**：`SiteStats`（总 PV）+ `PostViewCounts`（文章阅读）；`PageViewMiddleware` 对公开 GET 累加；冷却与爬虫过滤见 `PageViewService`。侧栏展示总访问，文章页展示阅读次数。



## 常用命令

```bash
cd Astrox.Blog
dotnet build
dotnet test
dotnet run --urls http://127.0.0.1:43147
```

开发凭据在 `appsettings.Development.json`；生产用环境变量 `Blog__AdminEmail` / `Blog__AdminPassword` / `Blog__ApiKey`。

## 约定

- 不要提交真实密钥或生产 `*.db`。
- 文章模型：`Post`（`Title` / `Slug` / `Summary` / `Markdown` / `Tags` 逗号串 / `IsPublished` / 时间戳）。
- 评论模型：`Comment`（`PostId` / `AuthorName` / `AuthorEmail?` / `Body` / `CreatedAt` / `IpHash` / `UserAgent?` / `IsDeleted`）。
- 访问统计：`SiteStat`（`Key` / `Value`）与 `PostViewCount`（`PostId` / `Count`）；启动时 `CREATE TABLE IF NOT EXISTS` 补表。
- Markdown 经 `MarkdownService` 渲染，已 `DisableHtml()`；公式写法见下方「Markdown 公式」。评论正文为纯文本并 HTML 转义。
- API 认证方案名：`ApiKey`（`Services/ApiKeyAuthenticationHandler.cs`）。
- 新增管理页放在 `Pages/Admin`，并保持 `[Authorize]`。
- 优先小改动、可运行；勿引入多作者 CMS、第三方评论 SaaS 或 OAuth，除非用户明确要求。



## Markdown 公式

用户说「把 Docs 里公式改到能显示」或后续新 md 要对齐公式时，**按本节统一改源文并核对页面**，不要另发明一种分隔符。

### 站点怎么渲染

1. 文稿只用 `$...$`（行内）和 `$$...$$`（独立成行，块级）。不要在 md 里写 `\(` `\)` / `\[` `\]`：反斜杠会被 Markdown 吃掉。
2. `MarkdownService` 把公式收成 `.math`，并输出 `\(...\)` / `\[...\]`。已去掉 Markdig GenericAttributes（避免 `{ITRS}` 变成 HTML 属性）；行内 `$` 用 `CjkAwareMathInlineParser`，紧贴汉字也可以（`计算$R(t)$和$Q(t)$`）。
3. 页面 KaTeX（`wwwroot/lib/katex`，`site.js` 的 `renderMathInElement`）只认 `\(` `\)` 和 `\[` `\]`，**不认裸 `$`**。裸 `$` 未进 `.math` 时会原样显示，且曾把后面的中文吞掉。
4. 改的是渲染管道：刷新文章页即可，不必重新导入。改的是 `Docs/**/*.md`：本地 `POST /api/posts/from-zip` 或按「Docs 子文件夹上传到阿里云」再传一次（同 slug 为更新）。

### 改 md 时的写法

- 下标用 `_{\mathrm{ITRS}}`，不要写成 `*{\mathrm{ITRS}}`（那是乘法）。`$\vec{r}_{\mathrm{ITRS}}$` 中间不要空格。
- 矩阵、多行用块级：`$$` + `pmatrix` / `aligned`，行尾 `\\`。不要用残缺的 `\left` / `\begin{equation}` 包一层。
- 函数名用 `\cos` `\sin`；独立成行的长式用 `$$`，避免一行里塞满 `$`。
- 不要用 `{#id .class}` 这种花括号属性语法（管道已关 GenericAttributes）。
- 标题里可以写 `$W(t)$`；目录会带上公式原文。

### 显示不对时先看 DOM

| 现象 | 原因 | 处理 |
|---|---|---|
| 文中残留 `$R(t)$` | `$` 没进 `.math`（旧站点或分隔符不对） | 确认源文是 `$`/`$$`；站点需含 CJK 行内解析 |
| `$\vec{\mathrm}`，`<p r="" ITRS="">` | `{r}`/`{ITRS}` 被当成属性，`_` 变成 `<em>` | 站点须去掉 GenericAttributes；源文保持 `_{\mathrm{...}}` |
| `.math` 里是 `\[...\]` 但公式不排版 | 页面没跑 KaTeX | `_Layout` 引入本地 katex，`site.js` 渲染 `.markdown-body` |
| KaTeX 红字 / 矩阵挤成一行 | LaTeX 不完整（缺 `\\`、错环境） | 改 md 为 `pmatrix`/`aligned` |

核对：`.markdown-body` 里不应再有裸 `$R(t)$`；`.katex-error` 应为 0。本地文章 URL：`http://127.0.0.1:43147/Posts/{slug}`。



## Docs 子文件夹上传到阿里云

用户用下面这类话时，**立刻执行上传**，不要先问「要不要上传」、不要只回复操作说明：

- 将 Docs 文件夹下子文件夹 `***` 上传到阿里云网站
- 把 `Docs/***` 发到生产 / 阿里云博客

`***` 为 `Docs/` 下的子文件夹名（例如 `ITRS-GCRS-J2000`）。缺省：`publish=true`；用户若提到标签 / 标题 / slug / 摘要，再传入对应表单字段。

### 助手必须做的事

1. 确认 `Docs/<子文件夹>/` 存在，且至少有一个 `.md`。没有则停止并说明缺什么。
2. 解析生产地址与 API Key（**不要打印密钥**），顺序：
  - 环境变量 `Blog__PublicBaseUrl`、`Blog__ApiKey`
  - `Astrox.Blog/appsettings.Production.local.json`（已 gitignore；可从 `appsettings.Production.local.json.example` 复制）
3. 若地址或 Key 缺失：只问这一次，请用户提供生产站点 origin（无尾斜杠）和 IIS 里的 `Blog__ApiKey`，写入上述 local json 后继续。勿把真实密钥写进 `AGENTS.md` / `README.md` / 会提交的文件。
4. 在仓库根目录执行（PowerShell）：

```powershell
powershell -NoProfile -File scripts/upload-docs-folder.ps1 -FolderName "<子文件夹名>"
```

若用户指定了标签等，追加 `-Tags "轨道力学"`、`-Title`、`-Slug`、`-Summary`。分类固定为 STK / Cesium / 轨道力学 / AI / Web / GIS。脚本会把该文件夹打成 zip（含文件夹名作为根目录），`POST {PublicBaseUrl}/api/posts/from-zip`。

1. 根据结果处理：
  - **HTTP 404**：生产还没有 `/api/posts/from-zip`。先把含该接口的提交推到 `main`，等 `.github/workflows/deploy-aliyun-iis.yml` 部署完成后再重试上传。部署会清空 `D:/IIS/Astrox.Blog`，但不会清 `D:/IIS/astrox-blog.db` 与 `D:/IIS/astrox-blog-media`。
  - **401 / 403**：Key 不对。请用户核对 IIS 环境变量，不要在回复里回显密钥。
  - **2xx**：向用户报告 `created` / `updated`、文章 URL `{PublicBaseUrl}/Posts/{slug}`、图片 `{PublicBaseUrl}/media/{包名}/...`。用 curl 或浏览器抽查文章页与至少一张图片是否 200。
2. 同一文件夹再传一次是更新（按 md 文件名生成的 slug），不是新建。
3. 不要把生成的 zip、local json、生产库或媒体文件提交进 git。

手动等价命令（仅排障；日常走脚本）：

```powershell
$folder = "Docs/<子文件夹>"
$zip = Join-Path $env:TEMP "astrox-blog-<子文件夹>.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path $folder -DestinationPath $zip
curl.exe -sS -X POST "https://<生产域名>/api/posts/from-zip" `
  -H "Authorization: Bearer <Blog__ApiKey>" `
  -F "file=@$zip" `
  -F "publish=true"
```



## 部署（阿里云 IIS CI）

- Workflow：`.github/workflows/deploy-aliyun-iis.yml` → 目标目录 `D:/IIS/Astrox.Blog`。
- Repository secrets（与 RocketSim3D / ASTROX.Docs 同名）：`ALIYUN_HOST`、`ALIYUN_USERNAME`、`ALIYUN_PASSWORD`。
- 服务器需 .NET 10 ASP.NET Core Hosting Bundle；部署会清空站点目录后上传 publish 输出（含 `web.config`）。
- 勿在日志或文档中打印 Secret 值；`appsettings.json` 默认库路径为站点外 `D:/IIS/astrox-blog.db`，文章媒体为 `D:/IIS/astrox-blog-media`；生产 `Blog__*` 等仍可用 IIS 环境变量覆盖，库文件与上传图片勿放在会被清空的站点目录内。



## 种子与首次运行

`Data/DbSeeder.cs` 在启动时 `EnsureCreated`、补齐 `Comments` / `SiteStats` / `PostViewCounts` 表（`CREATE TABLE IF NOT EXISTS`，已有库无需删库）、创建所有者（若配置齐全）、写入示例文章 `welcome-to-astrox-blog`。若配置邮箱已存在，启动时会把密码同步为当前 `Blog__AdminPassword`（生产改密码后重启/回收池即可生效）。修改模型后若本地库结构过旧且升级脚本未覆盖，可删除 `astrox-blog.db*` 后重启（开发环境可接受）。
