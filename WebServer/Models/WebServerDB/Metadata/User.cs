using Microsoft.AspNetCore.Mvc; // 引入 ASP.NET Core MVC 命名空間
using System.ComponentModel.DataAnnotations.Schema; // 引入數據註解的命名空間
using System.ComponentModel.DataAnnotations; // 引入數據註解的命名空間

namespace WebServer.Models.WebServerDB
{
    // 使用 ModelMetadataType 特性來指定 UserMetadata 類作為 User 類的元數據
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {
        // 密碼屬性，使用 NotMapped 特性表示不映射到資料庫
        [NotMapped]
        [Display(Name = "密碼")] // 顯示名稱
        [Required(ErrorMessage = "密碼必填")] // 必填驗證
        [RegularExpression(@"^.{4,20}$", ErrorMessage = "密碼限4~20個字")] // 密碼長度限制
        public string? Password { get; set; }

        // 確認密碼屬性，使用 NotMapped 特性表示不映射到資料庫
        [NotMapped]
        [Display(Name = "確認密碼")] // 顯示名稱
        [Required(ErrorMessage = "請再次輸入密碼")] // 必填驗證
        [Compare("Password", ErrorMessage = "密碼不相符")] // 比較密碼和確認密碼
        public string? ConfirmPassword { get; set; }
    }

    // 用戶元數據類，定義用戶屬性的驗證規則
    public partial class UserMetadata
    {
        // ID 屬性，顯示名稱
        [Display(Name = "ID")]
        public string? ID { get; set; }

        // 帳號屬性，顯示名稱
        [Display(Name = "帳號")]
        [Required(ErrorMessage = "帳號必填")] // 必填驗證
        [RegularExpression(@"^(?=[^\._]+[\._]?[^\._]+$)[\w\.]{3,20}$", ErrorMessage = "帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。")] // 帳號格式驗證
        public string? Account { get; set; }

        // 電子信箱屬性，顯示名稱
        [Display(Name = "電子信箱")]
        [Required(ErrorMessage = "電子信箱必填")] // 必填驗證
        [EmailAddress(ErrorMessage = "無效的電子信箱格式")] // 電子信箱格式驗證
        [MaxLength(50)] // 最大長度限制
        public string? Email { get; set; }

        // 姓名屬性，顯示名稱
        [Display(Name = "姓名")]
        [Required(ErrorMessage = "姓名必填")] // 必填驗證
        [MaxLength(100)] // 最大長度限制
        public string? Name { get; set; }

        // 行動電話屬性，顯示名稱
        [Display(Name = "行動電話")]
        [MaxLength(100)] // 最大長度限制
        public string? Mobile { get; set; }

        // 生日屬性，顯示名稱
        [Display(Name = "生日")]
        public DateOnly? Birthday { get; set; }

        // 地址屬性，顯示名稱
        [Display(Name = "地址")]
        [MaxLength(100)] // 最大長度限制
        public string? Address { get; set; }
    }
}