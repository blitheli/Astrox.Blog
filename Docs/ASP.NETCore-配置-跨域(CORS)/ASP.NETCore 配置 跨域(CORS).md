# ASP.NETCore 配置 跨域(CORS)

本文介绍如何在 ASP.NET Core 的应用程序中启用 CORS。

## 问题描述
浏览器安全性可防止网页向不处理网页的域发送请求。 此限制称为同域策略(CORS)。 同域策略可防止恶意站点从另一站点读取敏感数据。 有时，你可能想要允许其他站点对你的应用进行跨域请求，这时就要配置CORS策略了。

最近在使用ASP.NET CORE 3.1编写Web API时，写了几个有关轨道计算方面的功能函数，供网上调用，采用windows server 2019上的IIS服务托管。

Web API提供的功能主要是POST方法，输入json格式对象，输出也为json格式。

使用postman工具，以及java编写的后台代码调用皆没有问题，但是在页面中使用ajax调用则出现cors错误("Access to XMLHttpRequest at ‘http://*****' from origin 'http://****' has been blocked by CORS policy: Response to preflight request doesn't pass access control check: It does not have HTTP ok status."

在IIS中，设置HTTP响应头，添加"Access-Control-Allow-Origin"为"*"也是无效的！！

## 解决方案
网上找了下，解决方案如下：
在微软的官方文档中，给出了三种解决方案，此处采用第1种：**“使用命名策略”**
在Startup.cs中配置

```csharp
public class Startup
{
	// 1 此处为新添加
    readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

    public void ConfigureServices(IServiceCollection services)
    {
	    // 2 此处添加CORS
        services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                              builder =>
                              {
                               		builder.AllowAnyMethod()
                               				// 此处设置为无任何限制
                                             .SetIsOriginAllowed(_ => true)
                                              .AllowAnyHeader()
                                              .AllowCredentials();
  									// 也可以这样设置：允许指定的域名访问：
                                    // builder.WithOrigins("http://example.com",
                                    //                  "http://www.contoso.com")
			                        //            .AllowAnyMethod()
			                        //            .AllowAnyHeader()
                                    //            .AllowCredentials();
                              });
        });


        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

		//  3 此处添加调用
		//	注意顺序，前面为app.UseRouting(),后面为app.UseAuthorization();
        app.UseCors(MyAllowSpecificOrigins);

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```
在上述代码共添加3处地方，说明如下：
 
 1. 将策略名称设置为 _myAllowSpecificOrigins 。 策略名称是任意的。
 2. 调用 UseCors 扩展方法并指定 _myAllowSpecificOrigins CORS 策略。
 3. UseCors 添加 CORS 中间件。  UseCors 调用必须位于UseRouting之后，但在 UseAuthorization之前。
 4. 从ASP.NET CORE 2.1之后, 其Cors组件已经升级，出于安全考虑必须明确要允许的内容。 因此Access-Control-Allow-Origin不能使用*通配符（AllowAnyOrigin），必须指定确切的协议+域+端口。 如果真的就不想做任何限制，其实也是有办法的。只需要将AllowAnyOrigin替换为SetIsOriginAllowed(_ => true)就可以解决。
## 参考
 1. [ASP.NET Core 配置跨域(CORS)](https://www.skyfinder.cc/2020/01/13/aspnetcorecors/)
 2. [(CORS) 启用跨域请求 ASP.NET Core](https://docs.microsoft.com/zh-cn/aspnet/core/security/cors?view=aspnetcore-5.0#ecors)
