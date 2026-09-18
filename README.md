# Astrox.Blog

Yunfei Li / **Astrox** 的个人博客：科幻科技风界面、所有者登录发布、支持通过 API 远程推送 Markdown。

- **公开**：浏览已发布文章、按标签筛选、按 slug 阅读详情
- **管理**：Cookie 登录后创建 / 编辑 / 删除草稿与已发布文章（Markdown + 预览）
- **远程发布**：`Bearer` API Key 调用 `/api/posts`

默认使用 **SQLite**（EF Core），无需外部数据库。

## 要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（本项目 TargetFramework 为 `net10.0`）

## 快速运行

```bash
cd Astrox.Blog
dotnet restore
dotnet run --urls http://127.0.0.1:43147
```

浏览器打开：<http://127.0.0.1:43147>

开发环境默认账号（见 `appsettings.Development.json`，**勿用于生产**）：

| 项 | 值 |
| --- | --- |
| 邮箱 | `admin@astrox.local` |
| 密码 | `ChangeMe123!` |
| API Key | `dev-astrox-api-key-change-me` |

首次启动会自动：

1. 创建 SQLite 文件 `astrox-blog.db`
2. 若配置了 `Blog:AdminEmail` / `Blog:AdminPassword` 且库中无该用户，则创建所有者账号
3. 写入一篇示例文章 `welcome-to-astrox-blog`

无公开注册入口。

## 配置

可通过 `appsettings.json`、`appsettings.Development.json` 或环境变量设置：

```bash
export Blog__AdminEmail="you@example.com"
export Blog__AdminPassword="YourStrongPass1"
export Blog__ApiKey="a-long-random-secret"
export ConnectionStrings__DefaultConnection="Data Source=/data/astrox-blog.db"
```

对应 JSON：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=astrox-blog.db"
  },
  "Blog": {
    "Title": "Astrox.Blog",
    "OwnerName": "Yunfei Li",
    "Tagline": "航天 · 技术 · 星辰",
    "AdminEmail": "",
    "AdminPassword": "",
    "ApiKey": "",
    "PublicBaseUrl": "https://blog.example.com",
    "UmamiScriptUrl": "",
    "UmamiWebsiteId": "",
    "ExtraHeadSnippet": "",
    "Giscus": {
      "Repo": "",
      "RepoId": "",
      "Category": "",
      "CategoryId": ""
    }
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

| URL | 说明 |
| --- | --- |
| `/robots.txt` | 允许公开页；禁止 `/Admin`、`/Account`、`/api/`；指向 sitemap |
| `/sitemap.xml` | 首页 + 所有**已发布**文章 |
| 页面 `<head>` | `canonical`、Open Graph、Twitter Card；文章页另有 JSON-LD `BlogPosting` |

管理与登录页默认 `noindex, nofollow`。草稿文章详情同样 noindex。

## 分析（Umami，外部）

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

## 评论（Giscus，外部，无站内审核）

评论使用 [Giscus](https://giscus.app/)（GitHub Discussions），**本站不提供审核/屏蔽 UI**——访客评论按 Giscus / Discussions 默认规则直接出现。

**重要**：Giscus 需要一个已开启 Discussions 的 **公开 GitHub 仓库**。仅有 Origin 私有仓库不够，请另行创建或关联一个 public GitHub repo 专用于评论。

1. 在目标 GitHub 仓库启用 Discussions  
2. 打开 <https://giscus.app/>，按向导取得 `repo` / `repoId` / `category` / `categoryId`  
3. 写入配置（映射使用 `pathname`，主题默认 `noborder_dark` 以配合深色 UI）：

```json
"Blog": {
  "Giscus": {
    "Repo": "your-github-user/astrox-blog-comments",
    "RepoId": "R_kgDO_EXAMPLE",
    "Category": "Announcements",
    "CategoryId": "DIC_kwDO_EXAMPLE",
    "Mapping": "pathname",
    "Theme": "noborder_dark",
    "ReactionsEnabled": true,
    "Lang": "zh-CN"
  }
}
```

四个关键字段（Repo / RepoId / Category / CategoryId）任一为空则**不渲染**评论区块。仅已发布文章详情页显示评论。

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

## 部署备忘

- **Kestrel**：`dotnet publish -c Release -o ./publish`，再 `ASPNETCORE_URLS=http://0.0.0.0:8080 dotnet Astrox.Blog.dll`
- **IIS**：发布后用 ASP.NET Core Module 托管；配置环境变量注入 `Blog__*`
- **Docker**（可选）：仓库根目录提供了简易 `Dockerfile`，构建时请用环境变量传入管理员与 API Key
- 将 SQLite 文件挂到持久卷，避免容器重建丢数据

## 项目结构

```
Astrox.Blog/
  Data/           # DbContext、种子数据
  Models/         # Post、配置、API DTO
  Pages/          # 公开页、登录、管理后台
  Services/       # Markdown、Slug、API Key、站点 URL
  wwwroot/css/    # 科幻科技风样式
  PostsApi.cs     # /api/posts
  SeoEndpoints.cs # /robots.txt、/sitemap.xml
  Program.cs
```

## 许可证

个人项目模板，可按需修改使用。
