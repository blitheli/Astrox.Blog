# [Astrox.Blog](http://Astrox.Blog)

Yunfei Li / **Astrox** 的个人博客：科幻科技风界面、所有者登录发布、支持通过 API 远程推送 Markdown。

- **公开**：浏览已发布文章、按标签筛选、按 slug 阅读详情、站内评论（提交即公开）、站内访问计数
- **管理**：Cookie 登录后创建 / 编辑 / 删除草稿与已发布文章（Markdown + 预览）；文章页可软删垃圾评论
- **远程发布**：`Bearer` API Key 调用 `/api/posts`；也可上传 zip（Markdown + 图片）

默认使用 **SQLite**（EF Core），无需外部数据库。**库文件与上传图片必须放在站点程序目录外面**，否则 IIS 部署清空 `D:/IIS/Astrox.Blog` 时会被覆盖。见下方「数据目录」。

写文章请先在仓库 `Docs/<子文件夹>/` 写好 Markdown 和配图，再让助手自动上传（见「写文章：先写 Docs」）。

背景图致谢 [NASA SVS Deep Star Maps](https://svs.gsfc.nasa.gov/3895)（公有领域）。

## 要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（本项目 TargetFramework 为 `net10.0`）



## 快速运行

```bash
cd Astrox.Blog
dotnet restore
dotnet run --urls http://127.0.0.1:43147
```

浏览器打开：[http://127.0.0.1:43147](http://127.0.0.1:43147)

开发环境默认账号（见 `appsettings.Development.json`，**勿用于生产**）：


| 项       | 值                              |
| ------- | ------------------------------ |
| 邮箱      | `admin@astrox.local`           |
| 密码      | `ChangeMe123!`                 |
| API Key | `dev-astrox-api-key-change-me` |


首次启动会自动：

1. 按连接字符串创建 SQLite（开发默认在项目目录 `Astrox.Blog/astrox-blog.db`；生产必须在站点外，见「数据目录」）
2. 若配置了 `Blog:AdminEmail` / `Blog:AdminPassword` 且库中无该用户，则创建所有者账号；若该邮箱已存在，则把密码同步为当前配置值
3. 写入一篇示例文章 `welcome-to-astrox-blog`



## 数据目录（勿放程序文件夹内）

每次部署会**清空并替换**站点目录 `D:/IIS/Astrox.Blog`。SQLite、文章配图、日志若写在这个目录里，更新后全部丢失。它们必须和站点目录**并列**，不要放在 `Astrox.Blog` 程序文件夹内。


| 数据     | 生产路径（默认）                   | 配置项                                   | 部署时          |
| ------ | -------------------------- | ------------------------------------- | ------------ |
| 站点程序   | `D:/IIS/Astrox.Blog`       | IIS 物理路径                              | **整目录清空后重传** |
| SQLite | `D:/IIS/astrox-blog.db`    | `ConnectionStrings:DefaultConnection` | 不动           |
| 文章媒体   | `D:/IIS/astrox-blog-media` | `Blog:MediaRoot`                      | 不动           |
| 日志     | `D:/IIS/astrox-blog-logs`  | `Logging:Log4Net:Directory`           | 不动           |


对外图片 URL：`/media/{Docs子文件夹名}/axis.png`，对应磁盘 `D:/IIS/astrox-blog-media/{Docs子文件夹名}/axis.png`。

开发环境可以把库和媒体放在项目旁（`Astrox.Blog/astrox-blog.db`、`astrox-blog-media/`，已 gitignore），只用于本机。**不要把生产库或媒体拷进仓库或 publish 输出。**

IIS 也可用环境变量覆盖（推荐，避免改会随部署被冲掉的 `appsettings.json`）：

```bash
export ConnectionStrings__DefaultConnection="Data Source=D:/IIS/astrox-blog.db"
export Blog__MediaRoot="D:/IIS/astrox-blog-media"
```



## 写文章：先写 Docs，再让助手上传

推荐流程：**在仓库** `Docs/` **下写好整篇文章（md + 图片），不要先往生产库里手搓。** 一个子文件夹一篇（或一组）稿：

```
Docs/ITRS-GCRS-J2000/
  ITRS-GCRS-J2000坐标系的相互转换.md
  axis.png
  EOP.png
```

约定：

- 子文件夹名即媒体包名（上传后图片在 `/media/ITRS-GCRS-J2000/`）。
- Markdown 里用相对路径引用同目录图片：`![地轴](axis.png)`。
- 公式用 `$...$` / `$$...$$`（见 `AGENTS.md`「Markdown 公式」）。
- 分类标签用固定几种：`STK` / `Cesium` / `轨道力学` / `AI` / `Web` / `GIS`。

写完后对助手说一句即可自动上传到阿里云，例如：

- 将 Docs 文件夹下子文件夹 `ITRS-GCRS-J2000` 上传到阿里云网站
- 把 `Docs/ITRS-GCRS-J2000` 发到生产

助手会按 `AGENTS.md` 打 zip、调用 `POST /api/posts/from-zip`。同一文件夹再传一次是**更新**（按 md 文件名生成的 slug），不是新建。本机排障可用 `scripts/upload-docs-folder.ps1`。

不要把生成的 zip、`appsettings.Production.local.json`、生产 `.db` 或媒体文件提交进 git。

生产环境修改 `Blog__AdminPassword` 后，重启应用或回收 IIS 应用程序池会将该邮箱账号密码重置为新值（不合规密码会在启动日志中打出 Identity 错误）。

无公开注册入口。

## 配置

可通过 `appsettings.json`、`appsettings.Development.json` 覆盖。部署在 IIS 时，密钥和路径**优先用环境变量**（站点目录会被清空，写在程序文件夹里的 json 也会被冲掉）：

```bash
环境变量设置:
Blog__AdminEmail="you@example.com"
Blog__AdminPassword="YourStrongPass1"
Blog__ApiKey="a-long-random-secret"
Blog__MediaRoot="D:/IIS/astrox-blog-media"
ConnectionStrings__DefaultConnection="Data Source=D:/IIS/astrox-blog.db"
```

对应 JSON：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=D:/IIS/astrox-blog.db"
  },
  "Blog": {
    "Title": "Astrox.Blog",
    "OwnerName": "Yunfei Li",
    "Tagline": "航天 · 技术 · 星辰",
    "AdminEmail": "",
    "AdminPassword": "",
    "ApiKey": "",
    "MediaRoot": "D:/IIS/astrox-blog-media",
    "PublicBaseUrl": "https://blog.example.com",
    "UmamiScriptUrl": "",
    "UmamiWebsiteId": "",
    "ExtraHeadSnippet": ""
  }
}
```

生产环境请用环境变量注入密钥，不要把真实密码 / API Key 提交进仓库。

## SEO（站内）

配置对外规范域名（无尾部斜杠）：

```bash
export Blog__PublicBaseUrl="https://blog.example.com"
```

未配置时，canonical / sitemap 会回退到当前请求的 `Scheme://Host`。


| URL            | 说明                                                              |
| -------------- | --------------------------------------------------------------- |
| `/robots.txt`  | 允许公开页；禁止 `/Admin`、`/Account`、`/api/`；指向 sitemap                 |
| `/sitemap.xml` | 首页 + 所有**已发布**文章                                                |
| 页面 `<head>`    | `canonical`、Open Graph、Twitter Card；文章页另有 JSON-LD `BlogPosting` |


管理与登录页默认 `noindex, nofollow`。草稿文章详情同样 noindex。

## 访问统计（站内）

本站自建轻量 PV / 阅读计数，写入 SQLite，**不依赖** Umami：


| 指标    | 说明                                 |
| ----- | ---------------------------------- |
| 站点总访问 | 公开页（首页、文章详情）每次成功 GET +1；侧栏「访问统计」展示 |
| 文章阅读  | `/Posts/{slug}` 单独累加；文章页展示「阅读 N 次」 |


**不计**：`/Admin`、`/Account`、`/api/`、SEO 端点与错误页。

**防刷（轻量）**：同一访客（`IpHash` + URL）约 45 秒冷却（`IMemoryCache`）；User-Agent 含 `bot` / `crawler` / `spider` 等则跳过。IP 只存哈希，与评论一致。

表结构：`SiteStats`（键值，如 `TotalPv`）+ `PostViewCounts`（`PostId` / `Count`）。已有库启动时 `CREATE TABLE IF NOT EXISTS`，无需删库。

## 分析（Umami，外部可选）

Umami 是**可选**的外部分析脚本，与上方站内计数互相独立：站内计数始终可用；Umami 仅在配置齐全时额外注入 tracker。

在 Umami（自建或 [Umami Cloud](https://umami.is/)）创建站点后，填入脚本地址与 Website ID：

```json
"Blog": {
  "UmamiScriptUrl": "https://umami.example.com/script.js",
  "UmamiWebsiteId": "00000000-0000-4000-8000-000000000000"
}
```

或环境变量：

```bash
export Blog__UmamiScriptUrl="https://umami.example.com/script.js"
export Blog__UmamiWebsiteId="00000000-0000-4000-8000-000000000000"
```

两项都非空时才会注入 `<script defer … data-website-id="…">`；留空则完全不加载。

可选：`Blog:ExtraHeadSnippet` 可写入额外的 head HTML（例如其它统计片段），留空则跳过。

## 评论（站内，无审核）

评论写入本站 SQLite（`Comments` 表），与文章关联；**提交后立即公开显示，无站内审核队列**。正文为纯文本（HTML 转义），邮箱可选且前台不展示。

已发布文章详情页 `/Posts/{slug}` 底部提供评论列表与发表表单。所有者登录后可对单条评论执行**软删**（`IsDeleted`），前台不再显示。

### 防刷（轻量组合）


| 手段     | 说明                                                        |
| ------ | --------------------------------------------------------- |
| 速率限制   | 同一 IP（存 `IpHash`）每分钟 ≤3、每小时 ≤20（`IMemoryCache`，单机 IIS 足够） |
| 蜜罐字段   | 隐藏字段有填写则静默丢弃                                              |
| 最短填写时间 | 表单带服务端 token，<3 秒提交拒绝                                     |
| 正文限制   | 最长 2000 字；空白/重复字符拒绝；外链过多拒绝                                |
| IP 哈希  | 只存哈希，不存明文 IP                                              |


启动时若库已存在但尚无 `Comments` 表，会执行 `CREATE TABLE IF NOT EXISTS`（无需手工删库）。删除文章时评论级联删除。访问统计表（`SiteStats` / `PostViewCounts`）同样在启动时补齐。

## 远程 Markdown 发布（API）

认证：`Authorization: Bearer <Blog:ApiKey>`  
`Content-Type: application/json`

### 创建

```bash
curl -sS -X POST "http://127.0.0.1:43147/api/posts" \
  -H "Authorization: Bearer dev-astrox-api-key-change-me" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "来自 CI 的文章",
    "slug": "from-ci",
    "summary": "通过 API 推送的 Markdown",
    "tags": ["CI", "自动化"],
    "publish": true,
    "markdown": "# 你好\n\n这是 **Markdown** 正文。"
  }'
```



### 按 slug 更新

```bash
curl -sS -X PUT "http://127.0.0.1:43147/api/posts/from-ci" \
  -H "Authorization: Bearer dev-astrox-api-key-change-me" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "来自 CI 的文章（已更新）",
    "markdown": "# 更新\n\n正文已刷新。",
    "tags": "CI,自动化",
    "publish": true
  }'
```



### 删除

```bash
curl -sS -X DELETE "http://127.0.0.1:43147/api/posts/from-ci" \
  -H "Authorization: Bearer dev-astrox-api-key-change-me"
```



### 列表 / 获取

```bash
curl -sS "http://127.0.0.1:43147/api/posts" \
  -H "Authorization: Bearer dev-astrox-api-key-change-me"

curl -sS "http://127.0.0.1:43147/api/posts/welcome-to-astrox-blog" \
  -H "Authorization: Bearer dev-astrox-api-key-change-me"
```

`tags` 可为字符串数组，或逗号分隔字符串。`slug` 可省略，将由标题生成。

### 上传 zip（Markdown + 图片）

日常请走「写文章：先写 Docs」；下面是等价的手工接口。`Content-Type: multipart/form-data`。解压后：`.md` 写入**站点外** SQLite；图片写入 `Blog:MediaRoot`**（生产** `D:/IIS/astrox-blog-media`**，禁止放进** `D:/IIS/Astrox.Blog`**）**，经 `/media/...` 访问。相对图片路径会改写成该前缀。同一 slug 再次上传则更新文章。

zip 可为「文件夹打包」（`Docs/ITRS-GCRS-J2000/*.md` + 图片）或扁平结构。最大 20 MB。

```bash
curl -sS -X POST "http://127.0.0.1:43147/api/posts/from-zip" \
  -H "Authorization: Bearer dev-astrox-api-key-change-me" \
  -F "file=@ITRS-GCRS-J2000.zip" \
  -F "publish=true" \
  -F "tags=轨道力学"
```

可选表单字段（仅 zip 内只有一篇 Markdown 时生效）：`title`、`slug`、`summary`。`publish` 默认否。

## 部署备忘



### GitHub Actions → 阿里云 IIS（自动）

工作流：`.github/workflows/deploy-aliyun-iis.yml`（对齐 RocketSim3D / ASTROX.Docs）。

- **触发**：推送到 `main`（路径含 `Astrox.Blog/`**、`Dockerfile` 或该 workflow），或手动 `workflow_dispatch`
- **构建**：`ubuntu-latest` + .NET 10，`dotnet publish … -o _deploy`（自动生成 `web.config` / AspNetCoreModuleV2）
- **同步**：`appleboy/ssh-action@v1.2.0` 清空并准备目录，再 `appleboy/scp-action@v0.1.7` 上传到 `D:/IIS/Astrox.Blog`（port 22）

在 GitHub 仓库 **Settings → Secrets and variables → Actions → Repository secrets** 配置（名称与 Docs/RocketSim3D 相同，**不要**用 Environment secrets）：


| Secret            | 含义             |
| ----------------- | -------------- |
| `ALIYUN_HOST`     | 阿里云 Windows 主机 |
| `ALIYUN_USERNAME` | SSH 用户名        |
| `ALIYUN_PASSWORD` | SSH 密码         |


缺少任一 Secret 时，workflow 会以**中文** `::error::` 失败，且不会打印 Secret 值。

**IIS 前置**：服务器需安装 [.NET 10 ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)，站点物理路径指向 `D:\IIS\Astrox.Blog`，应用程序池为「无托管代码」。

**注意**：每次部署会**清空** `D:/IIS/Astrox.Blog` 后再上传。库与媒体必须在站点外（`D:/IIS/astrox-blog.db`、`D:/IIS/astrox-blog-media`），详见「数据目录」。仍可用 IIS 环境变量覆盖连接串、`Blog__MediaRoot`，并注入 `Blog__AdminEmail`、`Blog__AdminPassword`、`Blog__ApiKey`、`Blog__PublicBaseUrl` 等。

### 其他托管

- **Kestrel**：`dotnet publish -c Release -o ./publish`，再 `ASPNETCORE_URLS=http://0.0.0.0:8080 dotnet Astrox.Blog.dll`
- **IIS（手动）**：同上 Hosting Bundle；发布输出拷到站点目录
- **Docker**（可选）：仓库根目录 `Dockerfile`（.NET 10 镜像）；用环境变量传入管理员与 API Key；SQLite 与 `Blog__MediaRoot` 挂持久卷



## 项目结构

```
Docs/                 # 文稿源（md + 图片），写完让助手上传
Astrox.Blog/
  Data/               # DbContext、种子数据
  Models/             # Post、配置、API DTO
  Pages/              # 公开页、登录、管理后台
  Services/           # Markdown、Slug、API Key、站点 URL、zip 导入
  wwwroot/css/        # 科幻科技风样式
  PostsApi.cs         # /api/posts、/api/posts/from-zip
  SeoEndpoints.cs     # /robots.txt、/sitemap.xml
  Program.cs
Astrox.Blog.Tests/    # zip 导入与图片路径改写测试
scripts/              # upload-docs-folder.ps1
```

生产运行时数据（**不在仓库、不在站点目录内**）：`D:/IIS/astrox-blog.db`、`D:/IIS/astrox-blog-media/`。

## 许可证

个人项目模板，可按需修改使用。