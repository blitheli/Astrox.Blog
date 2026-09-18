# STK Component是什么

> 来源：[CSDN · 云上飞47636962](https://blog.csdn.net/u011575168/article/details/52431406)  
> 发布时间：2016-09-04 11:34:10  
> 整理说明：正文由 CSDN 公开页导出为 Markdown；原文无配图与公式，故无本地图片、无需公式改写。

AGI 公司自2008年开始发布 AGI Component 类库（后更名为 STK Component），不同于 STK Engine，AGI Component 类库建立在微软 .Net 2.0 平台上，是底层计算功能类的集合。借助于 AGI Component，软件开发者可以非常灵活的开发桌面应用程序、网页程序等。无论是计算坐标转换、处理数据还是开发大型程序，使用 STK Component 都可大大节省时间，提高工作效率。

目前 STK Component 类库的发布包含两种形式：.Net 和 Java。按照功能分类，STK Component 类库主要包含以下几大类库：

1. Dynamic Geometry Library：最核心的类库，是 AGI 所有软件的基础。包括：时间、坐标系、矢量工具；传感器模型、姿态模型、数值方法；轨道积分器；
2. Navigation Accuracy Library：导航精度分析类库；
3. Terrain Analysis Library：有关地形方面的类库；
4. Spatial Analysis Library：有关覆盖分析方面的类库；
5. Communication Library：有关通信方面的类库；
6. Graphics Library (Insight3D)：3D 控件类库；

STK Component 类库中所有的类及其相关说明请参考其帮助文档。实际开发时可参考其提供的示例，在进行 .Net 开发时，开发者需要拥有 .Net 编程基础，对类等概念要熟悉。

以 C# 语言为例，通过 .NET Reflector 软件可对 STK Component 类库（程序集，DLL 文件）进行反编译，通过观察、理解其类的构成及相应功能的具体代码，可极大提高对 STK Component 的认识以及自己的编程能力。
