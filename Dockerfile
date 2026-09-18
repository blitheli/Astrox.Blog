# 语法：dotnet SDK 镜像构建 / 运行时镜像运行
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Astrox.Blog.sln ./
COPY Astrox.Blog/Astrox.Blog.csproj Astrox.Blog/
RUN dotnet restore Astrox.Blog/Astrox.Blog.csproj
COPY Astrox.Blog/ Astrox.Blog/
RUN dotnet publish Astrox.Blog/Astrox.Blog.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# 生产请通过 -e / compose 注入 Blog__AdminEmail、Blog__AdminPassword、Blog__ApiKey
# 并将 SQLite / 文章图片挂持久卷，例如：
#   -v blogdata:/data
#   ConnectionStrings__DefaultConnection=Data Source=/data/astrox-blog.db
#   Blog__MediaRoot=/data/astrox-blog-media
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Astrox.Blog.dll"]
