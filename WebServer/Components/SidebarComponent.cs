using Microsoft.AspNetCore.Mvc;
using WebServer.Models.ViewModels;

namespace WebServer.Components;

// 設定此類別為 ViewComponent，並指定名稱為 "Sidebar"
[ViewComponent(Name = "Sidebar")]
public class SidebarComponent : ViewComponent // 繼承自 ViewComponent 類別
{
    // 建構函式，初始化 SidebarComponent 類別的實例
    public SidebarComponent()
    {
    }

    // 異步方法，負責執行 ViewComponent 的邏輯
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // 使用 Task.Yield() 讓控制權返回到調用者，允許其他任務執行
        await Task.Yield();

        // 建立 SidebarViewModel 的實例，並初始化 MenuItems 屬性
        var model = new SidebarViewModel
        {
            MenuItems = new List<MenuItem> // 初始化 MenuItems 為一個 MenuItem 的列表
            {
                new MenuItem // 首頁選項
                {
                    Title = "首頁", // 顯示的標題
                    URL = "/Home/Index", // 對應的 URL
                    Icon = "menu-icon tf-icons bx bx-home-circle", // 顯示的圖示 CSS 類別
                },
                new MenuItem // 使用者主檔選項
                {
                    Title = "使用者主檔",
                    URL = "/User/Index",
                    Icon = "menu-icon tf-icons bx bx-user",
                },
                new MenuItem // 檔案清單選項
                {
                    Title = "檔案清單",
                    URL = "/FileStorage/Index",
                    Icon = "menu-icon tf-icons bx bx-images",
                },
                new MenuItem // 人臉特徵選項
                {
                    Title = "人臉特徵",
                    URL = "/FaceFeature/Index",
                    Icon = "menu-icon tf-icons bx bx-images",
                },
                new MenuItem // 員工主檔
                {
                    Title = "員工主檔",
                    URL = "/Employee/Index",
                    Icon = "menu-icon tf-icons bx bx-male",
                },
                new MenuItem // 個人設定選項，包含子選項
                {
                    Title = "個人設定",
                    URL = string.Empty, // URL 為空，因為這是一個父選項
                    Icon = "menu-icon tf-icons bx bx-cog",
                    SubItems = new List<MenuItem> // 初始化子選項列表
                    {
                        new MenuItem // 登出子選項
                        {
                            Title = "登出",
                            URL = "/Account/Signout", // 對應的 URL
                            Icon = "menu-icon tf-icons bx bx-power-off", // 顯示的圖示 CSS 類別
                        },
                    },
                },
            },
        };

        // 返回視圖，並將 model 傳遞給視圖
        return View("Default", model);
    }
}