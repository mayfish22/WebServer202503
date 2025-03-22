using WebServer.Models.WebServerDB;

namespace WebServer.Services
{
    // 用於存儲驗證訊息的類別
    public class ValidatorMessage
    {
        // 網頁元件ID，通常用於標識前端表單中的特定元素
        public string? ElementID { get; set; }

        // 訊息內容，包含驗證失敗的具體信息
        public string? Text { get; set; }
    }

    // 用於驗證用戶註冊資料的服務類別
    public class ValidatorService
    {
        // WebServerDBContext 實例，用於與資料庫進行交互
        private readonly WebServerDBContext _webServerDB;

        // 建構函數，注入 WebServerDBContext 實例
        public ValidatorService(WebServerDBContext webServerDB)
        {
            _webServerDB = webServerDB;
        }

        /// <summary>
        /// 驗證 SignupViewModel 的資料
        /// </summary>
        /// <param name="user">要驗證的用戶資料</param>
        /// <returns>返回驗證結果的集合</returns>
        public IEnumerable<ValidatorMessage> ValidateSignup(User user)
        {
            // 儲存驗證結果的列表
            var result = new List<ValidatorMessage>();

            // 檢查帳號是否重複
            var accountDups = _webServerDB.User
                .Where(s => s.AccountNormalize == user.Account.Trim().ToUpper())
                .Select(s => s);
            if (accountDups.Any())
            {
                // 如果帳號已被使用，則添加驗證訊息
                result.Add(new ValidatorMessage
                {
                    ElementID = "User.Account", // 對應的網頁元件ID
                    Text = "帳號已被使用", // 驗證失敗的訊息
                });
            }

            // 檢查電子郵件是否重複
            var emailDups = _webServerDB.User
                .Where(s => s.EmailNormalize == user.Email.Trim().ToUpper())
                .Select(s => s);
            if (emailDups.Any())
            {
                // 如果電子郵件已被使用，則添加驗證訊息
                result.Add(new ValidatorMessage
                {
                    ElementID = "User.Email", // 對應的網頁元件ID
                    Text = "電子信箱已被使用", // 驗證失敗的訊息
                });
            }

            // 返回所有的驗證訊息
            return result;
        }
    }
}
