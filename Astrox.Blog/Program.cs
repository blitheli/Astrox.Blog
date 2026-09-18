using Astrox.Blog;
using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var logDirectory = ConfigureLog4Net(builder);

builder.Services.Configure<BlogOptions>(builder.Configuration.GetSection(BlogOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=astrox-blog.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});

builder.Services.AddAuthentication()
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthDefaults.Scheme, _ => { });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddSingleton<SiteUrlService>();
builder.Services.AddSingleton<CommentAntiSpamService>();
builder.Services.AddSingleton<PostMediaService>();

var mediaRoot = MediaRootResolver.Resolve(
    builder.Configuration["Blog:MediaRoot"],
    builder.Environment.ContentRootPath);
Directory.CreateDirectory(mediaRoot);
builder.Services.AddSingleton(new PostZipImporter(mediaRoot));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = PostZipImporter.MaxZipBytes;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = PostZipImporter.MaxZipBytes;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = PostZipImporter.MaxZipBytes;
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRoot),
    RequestPath = MediaRootResolver.RequestPath
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapPostsApi();
app.MapSeoEndpoints();

await DbSeeder.InitializeAsync(app.Services);

app.Logger.LogInformation(
    "Astrox.Blog 已启动，环境 {Environment}，日志目录 {LogDirectory}，媒体目录 {MediaRoot}（/media；文章图 /media/posts/{{slug}}）",
    app.Environment.EnvironmentName,
    logDirectory,
    mediaRoot);

app.Run();

static string ConfigureLog4Net(WebApplicationBuilder builder)
{
    var configured = builder.Configuration["Logging:Log4Net:Directory"];
    var logDirectory = string.IsNullOrWhiteSpace(configured)
        ? Path.Combine(builder.Environment.ContentRootPath, "Logs")
        : Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(builder.Environment.ContentRootPath, configured);

    logDirectory = Path.GetFullPath(logDirectory);
    Directory.CreateDirectory(logDirectory);
    log4net.GlobalContext.Properties["LogDirectory"] = logDirectory.Replace('\\', '/');

    builder.Logging.ClearProviders();
    builder.Logging.AddLog4Net(new Log4NetProviderOptions
    {
        Log4NetConfigFileName = "log4net.config",
        Watch = true
    });

    return logDirectory;
}

public partial class Program { }
