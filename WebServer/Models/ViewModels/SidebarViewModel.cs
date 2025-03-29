namespace WebServer.Models.ViewModels;

/// <summary>
/// ViewModel 用於表示側邊欄的資料。
/// </summary>
public class SidebarViewModel
{
    /// <summary>
    /// 側邊欄的選單項目列表。
    /// </summary>
    public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

/// <summary>
/// 表示側邊欄中的單一選單項目。
/// </summary>
public class MenuItem
{
    /// <summary>
    /// 選單項目的標題。
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 選單項目的連結 URL。
    /// </summary>
    public string? URL { get; set; }

    /// <summary>
    /// 選單項目的圖示 (Icon) 名稱或路徑。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 子選單項目列表，若無子選單則為 null。
    /// </summary>
    public List<MenuItem>? SubItems { get; set; } = new List<MenuItem>();
}