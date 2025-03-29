using Serilog;
using Serilog.Events;

namespace WebServer.Extensions;

public static class SerilogExtension
{
    /// <summary>
    /// 配置 Serilog 日誌記錄器。
    /// </summary>
    /// <param name="builder">Web 應用程序生成器。</param>
    /// <returns>配置過的 Web 應用程序生成器。</returns>
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        // 獲取 ProgramData 路徑
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        // 設定日誌檔案的完整路徑，將日誌儲存在 C:\ProgramData\WebServer\logs 資料夾中
        var logFilePath = Path.Combine(programDataPath, "WebServer", "logs", "log-.txt");

        // 確保 logs 資料夾存在
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

        Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                .WriteTo.Console()
                .WriteTo.File(
                    path: logFilePath, // 使用 ProgramData 路徑的日誌檔案
                    rollingInterval: RollingInterval.Day, // 每天滾動
                    retainedFileCountLimit: 7, // 保留 7 天的日誌
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}") // 日誌格式
                .CreateLogger();

        // 啟用 Serilog 的自我日誌，將錯誤訊息輸出到控制台
        Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

        return builder;
    }
}