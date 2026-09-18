# ZZ: ASP.NET WebApi(post) Json请求太大,无法反序列化问题解决方法

转载自：[https://blog.csdn.net/andy_5826_liu/article/details/92995776](%E4%BF%AE%E6%94%B9web.config%E9%85%8D%E7%BD%AE%E4%B8%A4%E4%B8%AA%E8%8A%82%E7%82%B9%EF%BC%9A%20%20%3Csystem.web%3E%E8%8A%82%E7%82%B9%E4%B8%8B%3ChttpRuntime%20targetFramework=%224.5%22%20/%3E%E4%BF%AE%E6%94%B9%E4%B8%BA%20%3ChttpRuntime%20targetFramework=%224.5%22%20maxRequestLength=%22102400%22%20%20%20%20%20%20%20%20%20%20executionTimeout=%22200%22%20enable=%22true%22%20/%3E%20%3Csystem.webServer%3E%E8%8A%82%E7%82%B9%E4%B8%8B%E6%96%B0%E5%A2%9E%EF%BC%88%E5%A6%82%E6%9C%89%E5%88%99%E4%BF%AE%E6%94%B9%EF%BC%89%20%20%20%20%20%3Csecurity%3E%20%20%20%20%20%20%20%3CrequestFiltering%3E%20%20%20%20%20%20%20%20%20%3CrequestLimits%20maxAllowedContentLength=%2220971520%22%20/%3E%20%20%20%20%20%20%20%3C/requestFiltering%3E%20%20%20%20%20%3C/security%3E%20%20%20%20%20%E9%9C%80%E8%A6%81%E6%B3%A8%E6%84%8F%E7%9A%84%E6%98%AF%EF%BC%9A%E7%AC%AC%E4%B8%80%E4%B8%AA%E8%8A%82%E7%82%B9maxRequestLength%E5%8D%95%E4%BD%8D%E6%98%AFkb,%20executionTimeout%E5%8D%95%E4%BD%8D%E6%98%AFs%EF%BC%9B%E7%AC%AC%E4%BA%8C%E4%B8%AA%E8%8A%82%E7%82%B9maxAllowedContentLength%E5%8D%95%E4%BD%8D%E6%98%AFbyte%EF%BC%8C%20%E6%89%80%E4%BB%A5%E4%B8%8A%E9%9D%A2%E6%98%AF100M%EF%BC%8C200s%EF%BC%9B%E4%B8%8B%E9%9D%A2%E6%98%AF20M%20%20ps:%20%E5%A6%82%E6%9E%9C%E9%85%8D%E7%BD%AE%E4%BA%86%E8%BF%99%E4%BA%9B%EF%BC%8C%E8%BF%98%E6%98%AF%E6%8F%90%E7%A4%BA%E8%B6%85%E5%87%BA%E9%99%90%E5%88%B6%EF%BC%8C%E5%88%99%E5%BA%94%E8%AF%A5%E6%80%80%E7%96%91%E6%98%AF%E5%90%A6%E6%9C%89Nginx%E6%88%96%E5%85%B6%E5%AE%83%E4%BB%A3%E7%90%86%E7%A8%8B%E5%BA%8F%EF%BC%8C%E7%9C%8B%E7%9C%8B%E5%85%B6%E4%B8%8A%E4%BC%A0%E6%96%87%E4%BB%B6%E5%A4%A7%E5%B0%8F%E9%99%90%E5%88%B6%EF%BC%88http%E3%80%81https%E9%99%90%E5%88%B6%E5%8F%AF%E8%83%BD%E4%B8%8D%E4%B8%80%E8%87%B4%EF%BC%89%20%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%E2%80%94%20%E7%89%88%E6%9D%83%E5%A3%B0%E6%98%8E%EF%BC%9A%E6%9C%AC%E6%96%87%E4%B8%BACSDN%E5%8D%9A%E4%B8%BB%E3%80%8CThinkinLiu%E3%80%8D%E7%9A%84%E5%8E%9F%E5%88%9B%E6%96%87%E7%AB%A0%EF%BC%8C%E9%81%B5%E5%BE%AACC%204.0%20BY-SA%E7%89%88%E6%9D%83%E5%8D%8F%E8%AE%AE%EF%BC%8C%E8%BD%AC%E8%BD%BD%E8%AF%B7%E9%99%84%E4%B8%8A%E5%8E%9F%E6%96%87%E5%87%BA%E5%A4%84%E9%93%BE%E6%8E%A5%E5%8F%8A%E6%9C%AC%E5%A3%B0%E6%98%8E%E3%80%82%20%E5%8E%9F%E6%96%87%E9%93%BE%E6%8E%A5%EF%BC%9Ahttps://blog.csdn.net/andy_5826_liu/article/details/92995776)

最近使用 ASP.NET 开发一个WebApi程序时遇到的问题，请求参数为json格式，返回参数也为json格式。

控制器的方法接收参数为一个具体类的形参，运行时(post请求），asp.net帮我把json字符串反序列化为我自定义的类对象。但是我发现当post的请求json数据量太大时，反序列化失败，导致接收到的类对象为null。

解决方法如下：

修改web.config配置两个节点：
<system.web>节点下<httpRuntime targetFramework="4.5" />修改为

```csharp
<httpRuntime targetFramework="4.5" maxRequestLength="102400" 
        executionTimeout="200" enable="true" />
```
<system.webServer>节点下新增（如有则修改）
```csharp
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="20971520" />
  </requestFiltering>
</security> 
```
   
需要注意的是：第一个节点maxRequestLength单位是kb, executionTimeout单位是s；第二个节点maxAllowedContentLength单位是byte， 所以上面是100M，200s；下面是20M

ps: 如果配置了这些，还是提示超出限制，则应该怀疑是否有Nginx或其它代理程序，看看其上传文件大小限制（http、https限制可能不一致）
————————————————
版权声明：本文为CSDN博主「ThinkinLiu」的原创文章，遵循CC 4.0 BY-SA版权协议，转载请附上原文出处链接及本声明。
原文链接：https://blog.csdn.net/andy_5826_liu/article/details/92995776

