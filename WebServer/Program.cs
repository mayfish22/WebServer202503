using Microsoft.EntityFrameworkCore;
using Serilog;
using WebServer.Extensions;
using WebServer.Hubs;
using WebServer.Middlewares;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // 在這裡放置可能會引發例外的程式碼
            var builder = WebApplication.CreateBuilder(args); // 創建 Web 應用程序生成器

            // 註冊 IHttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Add services to the container.
            builder.Services.AddControllersWithViews(); // 添加 MVC 控制器和視圖支持

            // 配置 Entity Framework Core 使用 SQL Server
            builder.Services.AddDbContext<WebServerDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("WebServerDB")); // 使用配置中的連接字串
            });

            // 資料驗證服務的依賴注入
            builder.Services.AddScoped<ValidatorService>(); // 將 ValidatorService 註冊為範圍服務

            // 使用 Session
            // 使用分散式記憶體快取 (Distributed Memory Cache)，它在應用程式範圍內存儲 Session 資料。
            builder.Services.AddDistributedMemoryCache(); // 添加分散式記憶體快取服務

            // 設置 Session 相關的配置
            builder.Services.AddSession(options =>
            {
                // 設定會話的閒置超時時間為 60 分鐘
                options.IdleTimeout = TimeSpan.FromMinutes(60);

                // 設定會話 Cookie 只能由伺服器端讀取，避免 JavaScript 存取
                options.Cookie.HttpOnly = true;

                // 設定會話 Cookie 為必要的，這表示該 Cookie 必須被用戶端儲存並且在每次請求時發送
                options.Cookie.IsEssential = true;
            });

            // 設定應用程式的認證方式，這裡使用 Cookie 驗證方案
            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme) // 設定認證方案使用 Cookie 認證
                .AddCookie(options =>
                {
                    // 設定當存取被拒絕的頁面時，將轉跳至指定的路徑 (例如，無權限存取頁面會導向至這個頁面)
                    options.AccessDeniedPath = new PathString("/Account/Signin");

                    // 設定當使用者未登入時，將轉跳至登入頁面
                    options.LoginPath = new PathString("/Account/Signin");

                    // 設定登出後的轉跳頁面
                    options.LogoutPath = new PathString("/Account/Signout");
                });

            // 設定 Serilog 日誌記錄器
            builder.ConfigureSerilog();

            // 加入記憶體快取服務
            builder.Services.AddMemoryCache();

            // 設定 SignalR
            builder.Services.AddSignalR();

            Log.Information("伺服器啟動"); // 記錄伺服器啟動的訊息

            var app = builder.Build(); // 建立 Web 應用程序

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) // 如果不是開發環境
            {
                app.UseExceptionHandler("/Home/Error"); // 使用自定義錯誤處理頁面
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts(); // 啟用 HSTS（HTTP 嚴格傳輸安全性）
            }

            //app.UseHttpsRedirection(); // 強制使用 HTTPS
            app.UseRouting(); // 啟用路由中介軟體

            app.UseSession(); // 啟用 Session 支持

            app.UseAuthentication(); // 啟用身份驗證中介軟體

            app.UseAuthorization(); // 啟用授權中介軟體

            app.UseMiddleware<HttpRequestLoggingMiddleware>();// 使用自定義的 HTTP 請求日誌中介軟體

            // 設定靜態資源的路由
            app.MapStaticAssets();
            // 設定控制器路由
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}") // 設定預設路由
                .WithStaticAssets(); // 支持靜態資源

            app.MapHub<FaceHub>("/faceHub");

            app.Run(); // 啟動應用程序
        }
        catch (Exception ex)
        {
            // 捕捉到例外後，記錄錯誤訊息
            Log.Fatal(ex, "伺服器意外終止"); // 記錄伺服器意外終止的錯誤
        }
        finally
        {
            // 確保在程式結束時關閉日誌
            Log.CloseAndFlush(); // 關閉 Serilog 日誌
        }
    }
}