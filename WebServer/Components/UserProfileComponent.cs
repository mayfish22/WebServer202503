using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using WebServer.Models.WebServerDB;

namespace WebServer.Components;

// 設定此類別為 ViewComponent，並指定名稱為 "UserProfile"
[ViewComponent(Name = "UserProfile")]
public class UserProfileComponent : ViewComponent
{
    private readonly IHttpContextAccessor _httpContext; // 用於訪問 HttpContext 的介面
    private readonly WebServerDBContext _webServerDB; // 用於訪問資料庫的上下文

    // 建構函數，注入 IHttpContextAccessor 和 WebServerDBContext
    public UserProfileComponent(IHttpContextAccessor httpContext, WebServerDBContext webServerDB)
    {
        _httpContext = httpContext; // 初始化 HttpContextAccessor
        _webServerDB = webServerDB; // 初始化資料庫上下文
    }

    // 異步方法，負責執行 ViewComponent 的邏輯
    public async Task<IViewComponentResult> InvokeAsync()
    {
        User? userProfile = null; // 用於儲存使用者資料的變數
        try
        {
            // 獲取當前 HttpContext
            var httpContext = _httpContext.HttpContext;

            // 確保 HttpContext 不為 null
            if (httpContext != null)
            {
                // 獲取當前使用者的 ClaimsPrincipal
                var user = httpContext.User;

                // 確保使用者已登入
                if (user.Identity.IsAuthenticated)
                {
                    // 從 Claims 中獲取使用者 ID
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    // 確保使用者 ID 存在且能成功解析為 Guid
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                        // 從資料庫中查找使用者資料
                        userProfile = await _webServerDB.User.FindAsync(userId);
                }
            }
        }
        catch (Exception ex)
        {
            // 記錄錯誤資訊
            Log.Error(nameof(UserProfileComponent), ex);
        }
        // 返回視圖，並傳遞使用者資料（如果有的話）
        return View("Default", userProfile);
    }
}
