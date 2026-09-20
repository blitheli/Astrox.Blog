# Cesium 用 BillboardCollection 替换默认 SkyBox 画真实星空

Cesium 默认的天空盒子是六张立方体贴图，看起来像“远方星空”，但其实是贴图像素，很难按星等缩放、按光谱着色，也难和真实赤经赤纬星表对齐。开源演示仓库 [AnotherCesiumSkybox](https://github.com/blitheli/AnotherCesiumSkybox) 关掉默认 SkyBox，改用 `BillboardCollection` 把 NASA Eyes 星表一颗颗画出来，再配合 ICRF→Fixed 惯性锁定，实现「地球在转、星空钉在惯性天球上」的观感。本文面向初学者，按直觉 → 原理 → 本地跑通的顺序说明。

![BillboardCollection 星空环绕地球](./skybox-billboardcollection.png)

## 先建立直觉：两种“星空”差在哪？

想象你站在地球表面抬头看夜空：

- **贴图星空（默认 SkyBox）**：像在相机外面套了一个画满星星的大盒子。盒子跟着相机走，用视线方向去采样六张图。优点是便宜、内置；缺点是星星不是“真实天体位置”，改亮度、颜色、对齐星表都不方便。
- **星表星空（本演示）**：每颗星是一个带柔和光晕的小精灵（Billboard）。位置来自星表坐标，亮度来自绝对星等与距离，颜色来自星表 RGB。地球在地固系里自转，星空集合每帧用矩阵“钉”在惯性天球上——于是你会看到地球转、星不动。

本仓库演示效果见上图：暗色地球外包一层针尖状恒星，接近 NASA Eyes on the Solar System 的观感。

## 默认 SkyBox 在做什么、有什么局限？

Cesium 的 `Scene.skyBox` 默认是一张**立方体贴图**（六张图：±X、±Y、±Z）。着色器根据视线方向采样，画出远景星空。

要点：

- 坐标约定偏 **TEME**（真赤道平春分点）相关变换，和“逐颗星表 + J2000/ICRF”不是同一套叙事；
- 星点是贴图像素，**不是**星表里的逐颗天体；
- 难以按星等缩放、按光谱着色，也难与真实赤经赤纬目录对齐。

因此演示在 `index.html` 里直接关掉默认天空，并把背景设为纯黑：

```js
// index.html
const viewer = new Cesium.Viewer("cesiumContainer", {
  // …
  skyBox: false,
  // …
});
viewer.scene.backgroundColor = Cesium.Color.BLACK;
```

关掉之后，画面上不再有立方体贴图星空，接下来要用自己的 `BillboardCollection` 把星填回去。

## 为什么选 BillboardCollection，而不是 PointPrimitive？

三种常见方案对比：

| 方案 | 优点 | 缺点 |
|------|------|------|
| 默认 `SkyBox` | 内置、开销低 | 贴图星空，难对齐星表 |
| `PointPrimitiveCollection` | 单点绘制、吞吐高 | 无纹理光晕，观感偏“硬点” |
| **`BillboardCollection`（本仓库）** | 共享纹理、柔和光斑、易调缩放/颜色 | 比 Point 稍重，但仍适合数万级亮星 |

`Billboard` 可以理解为“始终朝向相机的小纸片”。本仓库用一张共享的针尖光斑纹理（Eyes 片元核），给每颗星上色、调尺寸和透明度，观感比裸点柔和，更接近 Eyes。实现集中在 `js/starfield.js`。

核心流水线可以记成四步：

1. `fetch` 加载 `assets/eyes-stars/stars.0.dat` … `stars.5.dat` 与 `galaxies.0.dat`；
2. `parseEyesDat` 按 Eyes 布局解析，做黄道→J2000 旋转与颜色归一；
3. `billboards.add({ position, image, color, width, height, … })`；
4. 每帧更新 `collection.modelMatrix`（ICRF→Fixed）。

## 星表从哪来？`.dat` 怎么排？为什么不能 `file://`？

### 来源

星表来自 [NASA Eyes on the Solar System](https://eyes.nasa.gov/apps/solar-system/) 的静态点源目录。原始文件是 `stars.0.bin` … `stars.5.bin` 与 `galaxies.0.bin`；入库时改成 `.dat`，避免 Windows 浏览器把 `.bin` 当附件下载。演示运行时只读仓库内 `assets/eyes-stars/`，不热链 Eyes CDN。

离线下载入库时需带 Referer `https://eyes.nasa.gov/apps/solar-system/`；日常跑演示不必再访问 NASA。

### 二进制布局（小端）

每个文件：

1. 开头一个 `int32 count`（恒星条数）；
2. 然后重复 `count` 次，每条 **23 字节**：
   - `float32 mag`（视星等；尺寸主要用 absMag）
   - `float32 absMag`（绝对星等）
   - `uint8 r, g, b`
   - `float32 y_raw` → 位置里用 `y = -y_raw`
   - `float32 z`
   - `float32 x`

加载后再做两件事：

- 用四元数把黄道系坐标旋到 J2000：$(w,x,y,z)=(0.9791532214288992,\ 0.2031230389823101,\ 0,\ 0)$；
- 颜色按 `max(r,g,b)` 归一，避免整体偏暗。

分片大致按视星等划分：`stars.0` 为较亮星（mag ≤ 6.0），之后依次到 mag ≈ 8.03。缺可靠视差的星，距离会钉在约 $3.09\times 10^{20}$ m（10 kpc）。细节见仓库 `assets/eyes-stars/README.md`。

### 为什么必须用 HTTP，不能直接双击 `index.html`？

`starfield.js` 用浏览器的 `fetch` 拉 `.dat`。`file://` 协议下跨本地文件读取通常会被安全策略拦住，于是星表加载失败、星空空白。正确做法是在仓库根目录起一个静态 HTTP 服务（见文末「本地怎么跑」）。

## 从字节到天上的光点：解析 → 旋转 → Billboard

### 1. 解析

`parseEyesDat` 读 `ArrayBuffer`，校验长度是否等于 $4 + \mathrm{count}\times 23$，再逐条解出 mag、absMag、RGB 与位置。伪代码级理解：

```js
// js/starfield.js — parseEyesDat（概念摘录）
const count = view.getInt32(0, true);
for (let i = 0; i < count; i++) {
  const o = 4 + i * 23;
  const mag = view.getFloat32(o + 0, true);
  const absMag = view.getFloat32(o + 4, true);
  // r,g,b → 归一；y_raw → y = -y_raw；再读 z、x
  // rotateByQuaternion → J2000 位置
}
```

### 2. 黄道 → J2000

Eyes 目录里的位置在黄道相关坐标系；Cesium 惯性锁定用的是 ICRF/J2000 思路。所以解析时就乘上固定的 ecliptic→J2000 四元数，后面每帧只关心「惯性 ↔ 地固」，不再纠结黄道。

### 3. 放到“够远”的球面上再画 Billboard

真实恒星距离极大。为了日心/拉远视角仍落在相机 far 平面内，代码会把方向向量归一后缩放到渲染球半径（约 $5\times 10^{14}$ m），再 `collection.add`：

```js
// js/starfield.js — addCatalogItems（概念摘录）
collection.add({
  position: new Cesium.Cartesian3(rx, ry, rz),
  image: spriteImage,
  color: new Cesium.Color(r, g, b, alpha),
  width: size,
  height: size,
  sizeInMeters: false,
  horizontalOrigin: Cesium.HorizontalOrigin.CENTER,
  verticalOrigin: Cesium.VerticalOrigin.CENTER,
});
```

恒星与星系各用一个 `BillboardCollection`，默认加载全部 `stars.0–5` 分片。

## 惯性锁定：怎样让「地球转、星空钉住」？

### 问题

星表位置按**惯性系**（J2000 / 近似 ICRF）存放。Cesium 场景默认在**地固系**（ITRF / Fixed）里画地球。

若不对集合做变换，星空会和地球一起“粘”在 ECEF 里——你看不到「地球自转、星空相对惯性固定」，只会觉得星空跟着地面转。

### 做法

每帧把 `BillboardCollection.modelMatrix` 设为 ICRF→Fixed 旋转（平移为零）：

```js
// js/starfield.js — bindInertialLock
const m = Cesium.Transforms.computeIcrfToFixedMatrix(clock.currentTime);
collection.modelMatrix = Cesium.Matrix4.fromRotationTranslation(
  m,
  Cesium.Cartesian3.ZERO
);
```

挂在 `scene.preRender` 上。效果是：

- 地球仍在 Fixed 下自转；
- 星空 Billboard 的惯性坐标被变到当前 Fixed，**相对惯性空间保持不动**。

若 IAU 数据尚未就绪，`computeIcrfToFixedMatrix` 可能暂时拿不到，代码会短暂回退到 `computeTemeToPseudoFixedMatrix`。

### 演示页的“地球自转视角”

`index.html` 里还把相机锁在 ICRF：时钟加速跑（`multiplier = 3600`）时，你会更明显地看到地球在转、星空相对不动。这是观看技巧，和星空集合自身的 `modelMatrix` 锁定是两件配套的事。

地球影像用 Cesium 自带的 `NaturalEarthII` 静态瓦片，**不需要** Cesium Ion token，方便本地零配置演示。

## 亮度与尺寸：Eyes 风格的“针尖光斑”

NASA Eyes 的 `StarfieldComponent` 不是简单“星等越小点越大”，而是用通量估亮度，再压成很小的精灵：

1. 由绝对星等与距离算光度与通量，再取亮度  
   $$\mathrm{brightness} = 2\ln\bigl(1 + \mathrm{flux}(\mathrm{absMag}, d)\cdot 10^{4}\bigr)$$
2. 粒子尺度与窗口最长边、设备像素比有关：  
   $$\mathrm{particleSize} = \sqrt{\max(w,h)\cdot\mathrm{dpr}} / 60$$
3. 精灵像素尺寸大约是 $\mathrm{brightness}\times 4\times\mathrm{particleSize}$，再夹在约 5–50 px；绝大多数暗星落在下限附近——所以看起来像**针尖**。
4. 共享纹理的 alpha 核大致是 $\bigl(\mathrm{clamp}(1-2r,0,1)\bigr)^{5}$：中心亮、边缘很快衰减，形成柔和小光斑而不是硬方块。

颜色仍用星表 RGB（归一后），透明度再乘上由 brightness 推出的 alpha。调 `appearance.sizeScale` / `alphaScale` 可以整体放大或压暗，而不改星表。

一句话记忆：**亮星稍大稍亮，绝大多数是 5px 级针尖；光晕靠共享精灵纹理，不靠巨大 Billboard。**

## 本地怎么跑（简短可复现）

仓库：<https://github.com/blitheli/AnotherCesiumSkybox>

1. 克隆到本地，进入仓库根目录。
2. **不要**用浏览器直接打开 `index.html`（`file://` 拉不下 `.dat`）。
3. 起静态服务：
   - **Windows：** 双击 `serve.bat`，按提示打开地址（默认 <http://localhost:8080>）。
   - **macOS / Linux：**

```bash
./serve.sh
# 或: python3 -m http.server 8080
```

4. 浏览器打开 <http://localhost:8080/>。Cesium 从 CDN 加载钉扎版本（如 1.145），无需 npm、无需 Ion。
5. 左上角 HUD 应显示已加载的恒星/星系数量；画面为 NaturalEarthII 地球 + 环绕星空。时钟在走时，可观察地球自转、星空相对惯性固定。

可选验证解析：`node test/starfieldParseTest.js`。

## 小结

| 话题 | 结论 |
|------|------|
| 默认 SkyBox | 立方体贴图，便宜但不对齐星表 |
| 本演示方案 | 关 SkyBox，用 `BillboardCollection` + Eyes 星表 |
| 数据 | `stars.0–5.dat` + `galaxies.0.dat`，`int32` + 每星 23 字节 |
| 加载 | 必须 HTTP；`serve.bat` / `serve.sh` |
| 坐标 | 黄道→J2000 解析时完成；每帧 ICRF→Fixed 写 `modelMatrix` |
| 观感 | flux 亮度 + 针尖精灵核，接近 NASA Eyes |

完整代码与原理原文见开源仓库：[https://github.com/blitheli/AnotherCesiumSkybox](https://github.com/blitheli/AnotherCesiumSkybox)（注意仓库名大小写为 **Skybox**）。
