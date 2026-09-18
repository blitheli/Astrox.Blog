# Windows系统发布Asp.Net core应用到IIS上

如何在windows系统上发布一个Asp.net core应用的网站？踩了不少坑，现在简单总结如下。

亲测有效的系统：
- windows 7 x64
- windows 10 x64
- windows server 2016 x64

具体步骤如下：

## 1.Net Core SDK安装
.Net Core SDK是一组库和工具，开发人员可用其创建 .NET 应用程序和库。它包含以下用于构建和运行应用程序的组件：
- .NET CLI。
- .NET 库和运行时。
- dotnet 驱动程序。

只有安装了.Net运行时才能运行.Net Core程序。安装可从微软的官网下载，网址为：https://dotnet.microsoft.com/download。

下载时可以选择SDK，也可以只选择Runtime。

如果本机已经安装了Visual Studio 2019，则已经包含了.Net Core 3.1，不需单独下载。
## 2	IIS部署
IIS是Internet Information Services英文全称的缩写，是一个World Wide Web server服务。IIS是一种Web（网页）服务组件，其中包括Web服务器、FTP服务器、NNTP服务器和SMTP服务器，分别用于网页浏览、文件传输、新闻服务和邮件发送等方面，它使得在网络（包括互联网和局域网）上发布信息成了一件很容易的事。

简单来说，IIS是网页服务组件，用来搭载网站运行程序的平台。

IIS部署因操作系统不同而不同，下面分为服务器版本和桌面版给出。
### 2.1 Windows Server操作系统
启用 Web 服务器 (IIS) 服务器角色并建立角色服务。

1)	通过“管理”菜单或“服务器管理器”中的链接使用“添加角色和功能”向导。 在“服务器角色”步骤中，选中“Web 服务器(IIS)”框 。
 
2)	在“功能”步骤后，为 Web 服务器 (IIS) 加载“角色服务”步骤。 选择所需 IIS 角色服务，或接受提供的默认角色服务。

3)	继续执行“确认”步骤，安装 Web 服务器角色和服务。 安装 Web 服务器 (IIS) 角色后无需重启服务器/IIS。
 
### 2.2 Windows 桌面操作系统(win7/win10)
1)	导航到“控制面板”>“程序”>“程序和功能”>“打开或关闭 Windows 功能”（位于屏幕左侧） 。
2)	打开“Internet Information Services”节点。 打开“Web 管理工具”节点。
3)	选中“IIS 管理控制台”框。
4)	选中“万维网服务”框。
5)	接受“万维网服务”的默认功能，或自定义 IIS 功能。
6)	如果 IIS 安装需要重新启动，则重新启动系统。

## 3	安装 .NET Core 托管捆绑包（Windows系统）
IIS配置完成后，现在开始安装.Net Core托管捆绑包（dotnet-hosting-5.0.9-win.exe）。

注意，如果在 IIS 之前安装了托管捆绑包，则必须修复捆绑包安装。 在安装 IIS 后再次运行托管捆绑包安装程序。

如果在安装 64 位 (x64) 版本的 .NET Core 之后安装了捆绑包，则可能看上去缺少 SDK（未检测到 .NET Core SDK）。若要解决此问题，请将 C:\Program Files\dotnet\ 移到 PATH 上 C:\Program Files (x86)\dotnet\ 之前的位置。 

捆绑包内包含 .NET Core 运行时、.NET Core 库和 ASP.NET Core 模块。该模块允许 ASP.NET Core 应用在 IIS 内部运行。

1)	运行安装程序（dotnet-hosting-5.0.9-win.exe）。
2)	重启或在命令行界面中（cmd.exe）执行以下命令：
net stop was /y
net start w3svc

## 4 发布项目文件

使用Visual Studio创建Asp.Net Core 项目，一般为以下三种：
- Asp.Net Core Web(Razor page)
- Asp.Net Core Web（MVC）
- Asp.Net Core WebAPI
右键项目，选择发布，即可编译成功，并发布到文件夹中（可以自己指定，或者默认的路径）。

此文件夹即为我们要发布网站上的内容。
## 5  创建IIS站点（web网站）
首先创建程序发布文件夹。

在 IIS 服务器上，创建一个文件夹以包含应用已发布的文件夹和文件。 在接下来的步骤中，文件夹路径作为应用程序的物理路径提供给 IIS。

此处，假设我们创建了位于D盘的“wwwroot”文件夹。并将ASP.Net Core程序发布完成后的内容（上节提到的发布文件夹）拷贝到此文件夹中。

以win10系统为例，在左下角搜索IIS，即可打开IIS管理器。
 
点击“添加网站”，在弹出的窗口中，填入网站信息：
-  “网站名称”-自己任意取，此处为”AeroSpace_WebAPI；
- “物理路径”-关联到所创建网站应用的部署文件夹，即之前创建的"D:\wwwroot"；
- “端口号”-自己任意取，只要不于与现存的冲突就行了。

大功告成！！我们成功的利用IIS组件在本机创建了一个网站。网站所在的文件夹就是"D:\wwwroot"。

## 小结
以上即为windows系统上发布asp.net core应用到IIS网站的顺序。具体每个步骤不清楚可网上搜索教程。


 

