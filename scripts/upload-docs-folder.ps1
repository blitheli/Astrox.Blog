# 将 Docs/<子文件夹> 打成 zip，经 API Key 上传到博客（默认生产站点）。
# 用法（仓库根目录）：
#   pwsh -File scripts/upload-docs-folder.ps1 -FolderName ITRS-GCRS-J2000
# 凭据读取顺序：参数 > 环境变量 Blog__PublicBaseUrl / Blog__ApiKey > appsettings.Production.local.json
# 不要在日志中打印 ApiKey。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$FolderName,
    [string]$BaseUrl,
    [string]$ApiKey,
    [string]$Tags,
    [string]$Title,
    [string]$Slug,
    [string]$Summary,
    [bool]$Publish = $true
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $here = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($here)) {
        $here = Get-Location
    }
    return (Resolve-Path (Join-Path $here "..")).Path
}

function Read-LocalBlogConfig {
    param([string]$RepoRoot)
    $path = Join-Path $RepoRoot "Astrox.Blog\appsettings.Production.local.json"
    if (-not (Test-Path $path)) {
        return $null
    }
    return Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
}

function Resolve-Setting {
    param(
        [string]$Explicit,
        [string]$EnvironmentName,
        [string]$FromFile
    )
    if (-not [string]::IsNullOrWhiteSpace($Explicit)) { return $Explicit.Trim() }
    $fromEnv = [Environment]::GetEnvironmentVariable($EnvironmentName)
    if (-not [string]::IsNullOrWhiteSpace($fromEnv)) { return $fromEnv.Trim() }
    if (-not [string]::IsNullOrWhiteSpace($FromFile)) { return $FromFile.Trim() }
    return $null
}

$repoRoot = Get-RepoRoot
$docsDir = Join-Path $repoRoot "Docs"
$source = Join-Path $docsDir $FolderName
if (-not (Test-Path $source -PathType Container)) {
    throw "找不到 Docs 子文件夹：$source"
}

$markdown = Get-ChildItem -Path $source -Filter *.md -File
if ($markdown.Count -eq 0) {
    throw "文件夹内没有 .md 文件：$source"
}

$local = Read-LocalBlogConfig -RepoRoot $repoRoot
$fileUrl = $null
$fileKey = $null
if ($null -ne $local -and $null -ne $local.Blog) {
    $fileUrl = [string]$local.Blog.PublicBaseUrl
    $fileKey = [string]$local.Blog.ApiKey
}
$resolvedUrl = Resolve-Setting -Explicit $BaseUrl -EnvironmentName "Blog__PublicBaseUrl" -FromFile $fileUrl
$resolvedKey = Resolve-Setting -Explicit $ApiKey -EnvironmentName "Blog__ApiKey" -FromFile $fileKey

if ([string]::IsNullOrWhiteSpace($resolvedUrl) -or [string]::IsNullOrWhiteSpace($resolvedKey)) {
    throw @"
缺少生产站点地址或 API Key。任选一种方式配置后重试（勿提交真实密钥）：
  1. 复制 Astrox.Blog/appsettings.Production.local.json.example
     为 Astrox.Blog/appsettings.Production.local.json 并填入 Blog:PublicBaseUrl 与 Blog:ApiKey
  2. 设置环境变量 Blog__PublicBaseUrl、Blog__ApiKey
"@
}

$resolvedUrl = $resolvedUrl.TrimEnd("/")
$zipPath = Join-Path $env:TEMP ("astrox-blog-" + $FolderName + ".zip")
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path $source -DestinationPath $zipPath -Force
$zipSize = (Get-Item $zipPath).Length
if ($zipSize -gt 20MB) {
    throw "zip 超过 20 MB（$zipSize 字节），请缩小图片后重试。"
}

$form = @(
    "-F", "file=@$zipPath",
    "-F", ("publish=" + ($(if ($Publish) { "true" } else { "false" })))
)
if (-not [string]::IsNullOrWhiteSpace($Tags)) { $form += "-F"; $form += "tags=$Tags" }
if (-not [string]::IsNullOrWhiteSpace($Title)) { $form += "-F"; $form += "title=$Title" }
if (-not [string]::IsNullOrWhiteSpace($Slug)) { $form += "-F"; $form += "slug=$Slug" }
if (-not [string]::IsNullOrWhiteSpace($Summary)) { $form += "-F"; $form += "summary=$Summary" }

$endpoint = "$resolvedUrl/api/posts/from-zip"
Write-Host "上传 $FolderName ($zipSize 字节) → $endpoint"

$responseFile = Join-Path $env:TEMP "astrox-blog-upload-response.json"
$code = & curl.exe -sS -o $responseFile -w "%{http_code}" -X POST $endpoint `
    -H "Authorization: Bearer $resolvedKey" `
    @form

if ($code -eq "404") {
    throw "生产站没有 /api/posts/from-zip（HTTP 404）。请先把含该接口的提交推到 main 完成 IIS 部署，再重试上传。"
}
if ($code -eq "401" -or $code -eq "403") {
    throw "生产 API Key 未被接受（HTTP $code）。请检查 appsettings.Production.local.json 或 Blog__ApiKey，不要把密钥写进仓库。"
}
if ($code -notmatch "^2") {
    $body = if (Test-Path $responseFile) { Get-Content -Raw -Encoding UTF8 $responseFile } else { "" }
    throw "上传失败 HTTP $code。响应：$body"
}

$json = Get-Content -Raw -Encoding UTF8 $responseFile | ConvertFrom-Json
Write-Host ("结果：created={0} updated={1} mediaFolder={2}" -f $json.created, $json.updated, $json.mediaFolder)
if ($json.images) {
    foreach ($img in $json.images) {
        Write-Host "图片 $resolvedUrl$img"
    }
}
foreach ($post in $json.posts) {
    $postUrl = "$resolvedUrl/Posts/$($post.slug)"
    Write-Host "文章 $($post.title) → $postUrl"
}

Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
