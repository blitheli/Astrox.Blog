# 文章可下载资源（`/resources`）

把需要在文章里提供下载的文件放在本目录（或子目录），随站点一起发布。

## URL

生产（nginx → IIS）：

```text
https://blog.astrox.cn/resources/<文件名>
https://blog.astrox.cn/resources/<子目录>/<文件名>
```

本地：

```text
http://127.0.0.1:43147/resources/<文件名>
```

## Markdown 示例

```markdown
[下载示例数据](/resources/sample-download.txt)

[完整路径也可](https://blog.astrox.cn/resources/sample-download.txt)
```

## 约定

- **小文件**（建议单个 ≤ 20MB，仓库总体保持精简）可直接提交进本目录，随 `dotnet publish` / GitHub Actions 部署。
- **大文件**不要进 git：可手工拷到生产机 `D:/IIS/Astrox.Blog/wwwroot/resources/`。部署会清空站点其它文件，但会**合并保留**该目录中服务器上已有、仓库未覆盖的文件。
- 支持常见扩展名（`.zip` `.7z` `.rar` `.pdf` `.json` `.czml` `.csv` `.txt` `.py` `.cs` `.hgt` `.tif` 等）；未知类型以 `application/octet-stream` 提供下载。
- 文章配图仍走 Docs zip 导入 → `/media/...`，与本目录无关。

详见仓库根目录 `AGENTS.md` / `README.md` 中的「静态资源下载」说明。
