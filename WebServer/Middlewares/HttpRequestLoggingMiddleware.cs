using Serilog;
using Serilog.Context;

namespace WebServer.Middlewares;

/// <summary>
/// 中介軟體類，用於記錄進入的 HTTP 請求的詳細資訊。
/// 此中介軟體會捕獲請求的各種屬性並將其記錄到日誌中。
/// </summary>
public class HttpRequestLoggingMiddleware
{
    private readonly RequestDelegate _next; // 儲存下一個中介軟體的委託

    /// <summary>
    /// 初始化 HttpRequestLoggingMiddleware 類的新實例。
    /// </summary>
    /// <param name="next">下一個中介軟體的委託。</param>
    public HttpRequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next; // 初始化下一個中介軟體
    }

    /// <summary>
    /// 處理 HTTP 請求的主要方法，記錄請求的詳細資訊。
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 從 Claims 中取得使用者的識別碼（通常是唯一 ID），並推入日誌上下文中，方便後續記錄時一併輸出。
        using (LogContext.PushProperty("UserID", context.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value))
        // 從 Claims 中取得使用者名稱（Name），並推入日誌上下文中，方便識別是哪位使用者發出的請求。
        using (LogContext.PushProperty("UserName", context.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.Name)?.Value))
        // 取得發送請求的使用者 IP 位址，並推入日誌上下文，便於追蹤來源。
        using (LogContext.PushProperty("IP", context.Connection.RemoteIpAddress.ToString()))
        // 取得 HTTP 請求的方法（例如 GET、POST），並推入日誌上下文中。
        using (LogContext.PushProperty("Method", context.Request.Method))
        // 取得請求所使用的通訊協定（HTTP 或 HTTPS），並推入日誌上下文。
        using (LogContext.PushProperty("Scheme", context.Request.Scheme))
        // 取得請求主機名稱（例如 localhost:5001 或 yourdomain.com），若有值則推入日誌上下文。
        using (LogContext.PushProperty("Host", context.Request.Host.HasValue ? context.Request.Host.Value : null))
        // 取得請求的 URL 路徑（例如 /api/user），若有值則推入日誌上下文。
        using (LogContext.PushProperty("Path", context.Request.Path.HasValue ? context.Request.Path.Value : null))
        // 取得查詢字串（例如 ?id=123），若有值則推入日誌上下文，否則為空字串。
        using (LogContext.PushProperty("QueryString", context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty))
        {
            // 實際記錄一則資訊等級的日誌，內容為：誰（IP 與使用者名稱）發出了什麼樣的請求（方法與完整 URL 結構）。
            Log.Information("收到 {IP} {UserName} 的 HTTP 請求：{Method} {Scheme}://{Host}{Path}{QueryString}");
        }

        await _next(context); // 呼叫下一個中介軟體以繼續請求處理
    }
}