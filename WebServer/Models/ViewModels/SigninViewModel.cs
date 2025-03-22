using System.ComponentModel;

namespace WebServer.Models.ViewModels
{
    public class SigninViewModel
    {
        //帳號
        [DisplayName("帳號")]
        public string? Account { get; set; }
        //密碼
        [DisplayName("密碼")]
        public string? Password { get; set; }
        //登入後轉跳的頁面
        public string? ReturnUrl { get; set; }
        //錯誤訊息
        public string? ErrorMessage { get; set; }
    }
}