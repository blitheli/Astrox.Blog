# Astrox.Blog

Yunfei Li / **Astrox** 的个人博客：科幻科技风界面、所有者登录发布、支持通过 API 远程推送 Markdown。

- **公开**：浏览已发布文章、按标签筛选、按 slug 阅读详情
- **管理**：Cookie 登录后创建 / 编辑 / 删除草稿与已发布文章（Markdown + 预览）
- **远程发布**：`Bearer` API Key 调用 `/api/posts`

默认使用 **SQLite**（EF Core），无需外部数据库。

## 要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高 LTS

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
    "ApiKey": ""
  }
}
```

生产环境请用环境变量注入密钥，不要把真实密码 / API Key 提交进仓库。

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
  Services/       # Markdown、Slug、API Key 认证
  wwwroot/css/    # 科幻科技风样式
  PostsApi.cs     # /api/posts
  Program.cs
```

## 许可证

个人项目模板，可按需修改使用。
