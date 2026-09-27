# Cesium demo测试

本文演示如何在博客的 Markdown 文章里用 `<iframe>` 嵌入一个可交互的 Cesium 三维地球 demo：demo 页面单独放在站点媒体目录下，文章中只需一行 iframe 即可引用，读者无需离开文章就能拖动、缩放地球。

## 一、思路

Markdown 本身不能运行 JavaScript，也不适合直接写一整段 Cesium 代码。比较干净的做法是：

1. 把 demo 写成一个独立的 HTML 页面（本文为 `demos/cesium-demo-test/index.html`），放到站点可静态访问的目录，例如 `/media/demos/cesium-demo-test/index.html`；
2. 在文章 md 中写一个 `<iframe>` 指向该页面；
3. 在 iframe 下方再给一个「全屏打开」的普通链接，方便在新窗口里完整体验。

这样 demo 与文章互相隔离：Cesium 的脚本、样式不会污染博客页面，博客的样式也不会影响 demo。

## 二、最小 Cesium demo

demo 使用国内访问较快的 npmmirror CDN 加载固定版本的 Cesium（`cesium@1.145.0`），**不使用 Cesium Ion token**：底图改为免 token 的 ArcGIS World Imagery 瓦片，地形用椭球 `EllipsoidTerrainProvider`，从而避免 Ion 默认影像/地形因缺少 token 而报错。页面加载后先显示整个地球，随后飞到示例点「上海」。

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Cesium demo 测试</title>
  <link rel="stylesheet" href="https://registry.npmmirror.com/cesium/1.145.0/files/Build/Cesium/Widgets/widgets.css" />
  <script src="https://registry.npmmirror.com/cesium/1.145.0/files/Build/Cesium/Cesium.js"></script>
  <style>
    /* 铺满 iframe */
    html, body, #cesiumContainer { width: 100%; height: 100%; margin: 0; padding: 0; overflow: hidden; background: #000; }
  </style>
</head>
<body>
  <div id="cesiumContainer"></div>
  <script>
    // 免 token 底图：ArcGIS World Imagery
    const imagery = new Cesium.UrlTemplateImageryProvider({
      url: "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
      maximumLevel: 18,
      credit: "Esri, Maxar, Earthstar Geographics"
    });

    const viewer = new Cesium.Viewer("cesiumContainer", {
      baseLayer: new Cesium.ImageryLayer(imagery),          // 不用 Ion 默认影像
      terrainProvider: new Cesium.EllipsoidTerrainProvider(), // 不用 Ion 地形
      baseLayerPicker: false,
      geocoder: false,
      animation: false,
      timeline: false
    });

    // 示例点：上海
    viewer.entities.add({
      name: "上海",
      position: Cesium.Cartesian3.fromDegrees(121.4737, 31.2304),
      point: { pixelSize: 12, color: Cesium.Color.CYAN, outlineColor: Cesium.Color.WHITE, outlineWidth: 2 },
      label: { text: "上海", font: "16px sans-serif", pixelOffset: new Cesium.Cartesian2(0, -20) }
    });

    // 飞到上海
    setTimeout(() => {
      viewer.camera.flyTo({
        destination: Cesium.Cartesian3.fromDegrees(121.4737, 31.2304, 1500000),
        duration: 3
      });
    }, 1500);
  </script>
</body>
</html>
```

完整文件见本文文件夹下的 `demos/cesium-demo-test/index.html`，线上地址为 `/media/demos/cesium-demo-test/index.html`。

## 三、在 md 中嵌入 iframe

文章里只需要写下面这一段（宽度 100%、高度 500 像素、无边框、允许全屏；iframe 须独占一行）：

```html
<iframe src="/media/demos/cesium-demo-test/index.html" width="100%" height="500" style="border:0" allowfullscreen loading="lazy" title="Cesium demo测试"></iframe>

<a href="/media/demos/cesium-demo-test/index.html" target="_blank">全屏打开</a>
```

## 四、实际效果

<iframe src="/media/demos/cesium-demo-test/index.html" width="100%" height="500" style="border:0" frameborder="0" allowfullscreen loading="lazy" title="Cesium demo测试"></iframe>

[全屏打开](/media/demos/cesium-demo-test/index.html)

## 五、注意事项

1. **Markdown 渲染器要保留原始 HTML**：如果 Markdown 管线开启了"禁用 HTML"（例如 Markdig 的 `DisableHtml()`）或做了 HTML 过滤，`<iframe>` 会被当成普通文本转义显示，而不是真正嵌入。本站仍保留 `DisableHtml()`，只对**单独一行**、`src` 以 `/media/` 开头的 iframe 做白名单放行（仅保留 `src`、`width`、`height`、`style`、`title`、`allowfullscreen`、`loading`、`frameborder` 属性），所以 iframe 标签要写在同一行、前后空一行。
2. **X-Frame-Options / CSP**：若站点、反向代理（Nginx）或 IIS 给 demo 页面加了 `X-Frame-Options: DENY`，或 `Content-Security-Policy` 的 `frame-ancestors 'none'`，浏览器会拒绝在 iframe 中显示。同源嵌入时使用 `SAMEORIGIN` 或 `frame-ancestors 'self'` 即可。
3. **MIME 类型**：静态目录需要以正确的 `Content-Type` 返回 `.html`（`text/html`）和 `.js`（`text/javascript`），否则浏览器不会执行或会直接下载文件。
4. **CDN 与底图可访问性**：国内访问建议选 npmmirror 等国内 CDN，并固定版本号；底图瓦片服务需要返回 CORS 头（`Access-Control-Allow-Origin`），否则 WebGL 无法把瓦片当作纹理使用。OpenStreetMap 官方瓦片在国内访问不稳定。
5. **性能**：一个页面嵌入多个 Cesium iframe 会同时创建多个 WebGL 上下文，占用较多显存，建议每篇文章只嵌一两个，并加 `loading="lazy"`。
