using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory; // 引入記憶體快取的命名空間

namespace WebServer.Controllers; // 定義 Controller 所屬的命名空間

[Route("{controller}/{action=Index}")] // 設定路由模板，例如 /EspCam/Index 或 /EspCam/RegisterIp
public class Esp32CamController : Controller
{
    private readonly IMemoryCache _cache; // 用於儲存快取資料的 IMemoryCache 實例
    private const string Esp32CamIpCacheKey = "Esp32CamIpAddress"; // 定義一個常數作為快取的鍵值，用於唯一識別 ESP32-CAM 的 IP 位址

    /// <summary>
    /// EspCamController 的建構子
    /// 透過依賴注入取得 IMemoryCache 的實例
    /// </summary>
    /// <param name="cache">IMemoryCache 的實例</param>
    public Esp32CamController(IMemoryCache cache)
    {
        _cache = cache; // 將注入的 IMemoryCache 實例賦值給私有變數
    }

    /// <summary>
    /// 接收 ESP32-CAM 發送過來的 IP 位址並儲存到快取中
    /// </summary>
    /// <param name="ip">ESP32-CAM 的 IP 位址字串</param>
    /// <returns>回傳註冊結果的 IActionResult</returns>
    [HttpGet] // 設定此 Action 接收 HTTP GET 請求 (或根據 ESP32-CAM 發送方法設定為 [HttpPost])
    public IActionResult RegisterIp(string ip)
    {
        // 檢查接收到的 IP 位址是否為空或 Null
        if (!string.IsNullOrEmpty(ip))
        {
            // 設定快取
            var cacheEntryOptions = new MemoryCacheEntryOptions();

            // 將接收到的 IP 位址儲存到快取中
            // 使用 Esp32CamIpCacheKey 作為鍵值，ip 作為要儲存的值，cacheEntryOptions 作為快取選項
            _cache.Set(Esp32CamIpCacheKey, ip, cacheEntryOptions);

            // 在控制台輸出接收到的 IP 位址，用於偵錯
            Console.WriteLine($"Received and cached ESP32-CAM IP: {ip}");
            // 回傳成功訊息
            return Ok("IP registered successfully.");
        }
        else
        {
            // 如果接收到的 IP 位址無效，回傳錯誤訊息
            Console.WriteLine("Received invalid IP address."); // 記錄收到無效 IP
            return BadRequest("Invalid IP address.");
        }
    }

    /// <summary>
    /// 從快取中獲取儲存的 ESP32-CAM IP 位址
    /// </summary>
    /// <returns>回傳包含 IP 位址的 IActionResult，如果快取中沒有則回傳 NotFound</returns>
    [HttpGet] // 設定此 Action 接收 HTTP GET 請求
    public IActionResult GetEspCamIp()
    {
        string esp32CamIpAddress;
        // 嘗試從快取中獲取儲存的 IP 位址
        // TryGetValue 方法會嘗試獲取指定鍵值的值，如果找到則返回 true，否則返回 false
        if (_cache.TryGetValue(Esp32CamIpCacheKey, out esp32CamIpAddress))
        {
            // 如果從快取中成功獲取到 IP 位址
            Console.WriteLine($"Retrieved ESP32-CAM IP from cache: {esp32CamIpAddress}"); // 記錄從快取中讀取的 IP
            // 回傳包含 IP 位址的 Ok 結果
            return Ok(esp32CamIpAddress);
        }
        else
        {
            // 如果快取中沒有找到對應的 IP 位址 (可能尚未註冊或已過期)
            Console.WriteLine("ESP32-CAM IP not found in cache."); // 記錄快取中沒有 IP
            // 回傳 NotFound 結果，表示找不到資源
            return NotFound("ESP32-CAM IP not registered yet or has expired.");
        }
    }
}