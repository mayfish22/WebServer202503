using WebServer.Models.WebServerDB;

namespace WebServer.Services;

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

    /// <summary>
    /// 驗證 UserViewModel 的資料
    /// </summary>
    /// <param name="id"></param>
    /// <param name="account"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    public IEnumerable<ValidatorMessage> ValidateUser(Guid id, string? account, string? email)
    {
        // 儲存驗證結果的列表
        var result = new List<ValidatorMessage>();

        // 檢查帳號是否重複
        var accountDups = _webServerDB.User
            .Where(s => !s.ID.Equals(id) && s.AccountNormalize == (account ?? "").Trim().ToUpper())
            .Select(s => s);
        if (accountDups.Any())
        {
            // 如果帳號已被使用，則添加驗證訊息
            result.Add(new ValidatorMessage
            {
                ElementID = "Account", // 對應的網頁元件ID
                Text = "帳號已被使用", // 驗證失敗的訊息
            });
        }

        // 檢查電子郵件是否重複
        var emailDups = _webServerDB.User
            .Where(s => !s.ID.Equals(id) && s.EmailNormalize == (email ?? "").Trim().ToUpper())
            .Select(s => s);
        if (emailDups.Any())
        {
            // 如果電子郵件已被使用，則添加驗證訊息
            result.Add(new ValidatorMessage
            {
                ElementID = "Email", // 對應的網頁元件ID
                Text = "電子信箱已被使用", // 驗證失敗的訊息
            });
        }

        // 返回所有的驗證訊息
        return result;
    }

    /// <summary>
    /// 驗證 Employee 資料的唯一性規則 (編號, 電子郵件, 行動電話)。
    /// </summary>
    /// <param name="employee">要驗證的 Employee 物件</param>
    /// <returns>包含驗證錯誤訊息的列表。如果列表為空，表示驗證成功。</returns>
    public IEnumerable<ValidatorMessage> ValidateEmployee(Employee employee)
    {
        // 儲存驗證結果的列表
        var result = new List<ValidatorMessage>();

        // 對可能包含空格或大小寫的欄位進行標準化處理 (Trim spaces, No/Email to Upper)
        employee.No = employee.No?.Trim().ToUpper();
        employee.Email = employee.Email?.Trim().ToUpper();
        employee.Mobile = employee.Mobile?.Trim(); // 電話號碼通常不需要轉大寫，但可以去首尾空白

        // --- 檢查員工編號是否重複 ---
        // 只在員工編號不為空時進行檢查
        if (!string.IsNullOrEmpty(employee.No))
        {
            // 查詢資料庫中是否存在ID不同但員工編號相同的記錄
            var noDups = _webServerDB.Employee
                .Where(s => !s.ID.Equals(employee.ID) && s.No == employee.No)
                .Select(s => s);

            if (noDups.Any())
            {
                // 如果員工編號已被使用，則添加驗證訊息
                result.Add(new ValidatorMessage
                {
                    ElementID = "Employee.No", // 對應的網頁元件
                    Text = "員工編號已被使用", // 驗證失敗的訊息
                });
            }
        }


        // --- 檢查電子郵件是否重複 ---
        // 只在電子郵件不為空時進行檢查
        if (!string.IsNullOrEmpty(employee.Email))
        {
            // 查詢資料庫中是否存在ID不同但電子郵件相同的記錄
            var emailDups = _webServerDB.Employee
                .Where(s => !s.ID.Equals(employee.ID) && s.Email == employee.Email) 
                .Select(s => s);

            if (emailDups.Any())
            {
                // 如果電子郵件已被使用，則添加驗證訊息
                result.Add(new ValidatorMessage
                {
                    ElementID = "Employee.Email", // 對應的網頁元件
                    Text = "電子郵件已被使用", // 驗證失敗的訊息
                });
            }
        }


        // --- 檢查行動電話是否重複 ---
        // 只在行動電話不為空時進行檢查
        if (!string.IsNullOrEmpty(employee.Mobile))
        {
            // 查詢資料庫中是否存在ID不同但行動電話相同的記錄
            var mobileDups = _webServerDB.Employee
                .Where(s => !s.ID.Equals(employee.ID) && s.Mobile == employee.Mobile) 
                .Select(s => s);

            if (mobileDups.Any())
            {
                // 如果行動電話已被使用，則添加驗證訊息
                result.Add(new ValidatorMessage
                {
                    ElementID = "Employee.Mobile", // 對應的網頁元件
                    Text = "行動電話已被使用", // 驗證失敗的訊息
                });
            }
        }


        // 返回所有的驗證訊息
        return result;
    }

}
