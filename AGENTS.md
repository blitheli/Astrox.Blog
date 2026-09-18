# AGENTS.md

面向后续在本仓库工作的编码助手。

## 项目要点

- **Astrox.Blog**：ASP.NET Core 10（`net10.0`）Razor Pages + SQLite（EF Core）+ Identity（单所有者）+ Markdig + Minimal API（`/api/posts`）。
- 公开读者只读已发布文章；写操作需 Cookie 登录（`/Admin`）或 Bearer API Key。
- 中文 UI 文案为主；视觉为深色青霓虹科技风（`wwwroot/css/site.css`）。
- SEO：`/robots.txt`、`/sitemap.xml`、canonical / OG / Twitter / JSON-LD（`Blog:PublicBaseUrl`）。
- 外部集成（可选）：Umami（`Blog:Umami*`）；未配置则不注入。
- **站内评论**：`Comment` 实体写入 SQLite；提交即公开、无审核；防刷见 `CommentAntiSpamService`（限流 / 蜜罐 / 最短填写时间 / 正文限制）。所有者可在文章页软删。无 Giscus / 第三方评论 SaaS。

## 常用命令

```bash
cd Astrox.Blog
dotnet build
dotnet run --urls http://127.0.0.1:43147
```

开发凭据在 `appsettings.Development.json`；生产用环境变量 `Blog__AdminEmail` / `Blog__AdminPassword` / `Blog__ApiKey`。

## 约定

- 不要提交真实密钥或生产 `*.db`。
- 文章模型：`Post`（`Title` / `Slug` / `Summary` / `Markdown` / `Tags` 逗号串 / `IsPublished` / 时间戳）。
- 评论模型：`Comment`（`PostId` / `AuthorName` / `AuthorEmail?` / `Body` / `CreatedAt` / `IpHash` / `UserAgent?` / `IsDeleted`）。
- Markdown 经 `MarkdownService` 渲染，已 `DisableHtml()`；评论正文为纯文本并 HTML 转义。
- API 认证方案名：`ApiKey`（`Services/ApiKeyAuthenticationHandler.cs`）。
- 新增管理页放在 `Pages/Admin`，并保持 `[Authorize]`。
- 优先小改动、可运行；勿引入多作者 CMS、第三方评论 SaaS 或 OAuth，除非用户明确要求。

## 部署（阿里云 IIS CI）

- Workflow：`.github/workflows/deploy-aliyun-iis.yml` → 目标目录 `D:/IIS/Astrox.Blog`。
- Repository secrets（与 RocketSim3D / ASTROX.Docs 同名）：`ALIYUN_HOST`、`ALIYUN_USERNAME`、`ALIYUN_PASSWORD`。
- 服务器需 .NET 10 ASP.NET Core Hosting Bundle；部署会清空站点目录后上传 publish 输出（含 `web.config`）。
- 勿在日志或文档中打印 Secret 值；`appsettings.json` 默认库路径为站点外 `D:/IIS/astrox-blog.db`；生产 `Blog__*` 等仍可用 IIS 环境变量覆盖，库文件勿放在会被清空的站点目录内。

## 种子与首次运行

`Data/DbSeeder.cs` 在启动时 `EnsureCreated`、补齐 `Comments` 表（`CREATE TABLE IF NOT EXISTS`，已有库无需删库）、创建所有者（若配置齐全）、写入示例文章 `welcome-to-astrox-blog`。若配置邮箱已存在，启动时会把密码同步为当前 `Blog__AdminPassword`（生产改密码后重启/回收池即可生效）。修改模型后若本地库结构过旧且升级脚本未覆盖，可删除 `astrox-blog.db*` 后重启（开发环境可接受）。
