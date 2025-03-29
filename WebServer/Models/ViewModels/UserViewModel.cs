using System.ComponentModel.DataAnnotations;
using WebServer.Services;

namespace WebServer.Models.ViewModels;

public class UserViewModel : IValidatableObject
{
    public Guid ID { get; set; }
    public bool IsReadonly { get; set; }
    public string? ErrorMessage { get; set; }
    [Display(Name = "帳號")]
    [Required(ErrorMessage = "帳號必填")] // 必填驗證
    [RegularExpression(@"^(?=[^\._]+[\._]?[^\._]+$)[\w\.]{3,20}$", ErrorMessage = "帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。")] // 帳號格式驗證
    public string? Account { get; set; }
    [Display(Name = "電子信箱")]
    [Required(ErrorMessage = "電子信箱必填")] // 必填驗證
    [EmailAddress(ErrorMessage = "無效的電子信箱格式")] // 電子信箱格式驗證
    [MaxLength(50)] // 最大長度限制
    public string? Email { get; set; }
    [Display(Name = "姓名")]
    [Required(ErrorMessage = "姓名必填")] // 必填驗證
    [MaxLength(50)] // 最大長度限制
    public string? Name { get; set; }
    [Display(Name = "行動電話")]
    [MaxLength(50)] // 最大長度限制
    public string? Mobile { get; set; }
    [Display(Name = "生日")]
    public DateOnly? Birthday { get; set; }

    /// <summary>
    /// 自定義驗證邏輯
    /// </summary>
    /// <param name="validationContext">驗證上下文，包含服務和其他信息</param>
    /// <returns>返回驗證結果的集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // 從驗證上下文中獲取 ValidatorService 的實例
        var service = validationContext.GetService<ValidatorService>();

        // 使用 ValidatorService 進行用戶資料的驗證
        var rs = service!.ValidateUser(ID, Account, Email);

        // 如果驗證結果不為 null 且有任何錯誤
        if (rs != null && rs.Any())
        {
            // 將驗證結果轉換為 ValidationResult 集合
            var x = rs.Select(r => new ValidationResult(
                r.Text, // 錯誤訊息
                new[] { string.IsNullOrEmpty(r.ElementID) ? nameof(ErrorMessage) : r.ElementID } // 錯誤對應的屬性名稱
            ));
            return x; // 返回所有的驗證結果
        }

        // 如果沒有錯誤，返回空的驗證結果集合
        return Enumerable.Empty<ValidationResult>();
    }
}
