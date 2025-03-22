using System.ComponentModel.DataAnnotations; // 引入數據註解命名空間
using WebServer.Models.WebServerDB; // 引入 WebServerDB 模型命名空間
using WebServer.Services; // 引入服務命名空間

namespace WebServer.Models.ViewModels
{
    // 註冊視圖模型，實現 IValidatableObject 接口以進行自定義驗證
    public class SignupViewModel : IValidatableObject
    {
        // 用戶資料
        public User User { get; set; }

        // 錯誤訊息，可能為 null
        public string? ErrorMessage { get; set; }

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
            var rs = service!.ValidateSignup(User);

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
}
