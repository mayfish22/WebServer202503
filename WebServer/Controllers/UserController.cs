using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebServer.Models.ViewModels;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

// 使用[Authorize]屬性來限制對此控制器的訪問，只有經過授權的用戶才能訪問
[Authorize]
// 定義路由模板，指定控制器和操作的路由格式
// {controller} 將被替換為控制器名稱，{action=Index} 指定默認操作為 Index
[Route("{controller}/{action=Index}")]
public class UserController : Controller
{
    private readonly WebServerDBContext _webServerDB;

    // 控制器的建構函數
    public UserController(WebServerDBContext webServerDB)
    {
        // 將傳入的上下文實例賦值給私有字段
        _webServerDB = webServerDB;
    }

    #region Index
    // 使用 [HttpGet] 特性標記此方法為處理HTTP GET請求的行為方法
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 使用 await 關鍵字等待 Task.Yield() 的完成，這會將控制權返回到調用者，允許其他任務執行
        await Task.Yield();

        // 在ASP.NET Core MVC中，~ 符號是一個特殊的字符，用於表示應用程序的根目錄。
        // 這個符號在路徑中提供了一種方便的方式來引用應用程序的根位置，
        // 無論應用程序的實際部署位置如何。
        // 例如，"~/Views/User/Index.cshtml" 會被解析為從應用程序的根目錄開始的完整路徑。

        // 返回一個視圖，指定視圖的路徑為 "~/Views/User/Index.cshtml"
        return View("~/Views/User/Index.cshtml");
    }

    [HttpPost] // 指定此方法為 HTTP POST 請求
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            // 從資料庫中查詢 User 表
            var query = from n1 in _webServerDB.User
                        select n1;

            // 獲取總記錄數
            var recordsTotal = await query.CountAsync();

            #region 關鍵字搜尋
            // 檢查是否有搜尋關鍵字
            if (!string.IsNullOrEmpty((string)Request.Form["search[value]"]))
            {
                // 取得搜尋關鍵字並轉為大寫
                string sQuery = Request.Form["search[value]"].ToString().ToUpper();

                // 根據搜尋關鍵字過濾查詢
                query = query.Where(t => t.AccountNormalize.Contains(sQuery) // 檢查帳號
                            || t.EmailNormalize.Contains(sQuery)    // 檢查電子郵件
                            || t.Name.ToUpper().Contains(sQuery)   // 檢查姓名
                            || t.Mobile.ToUpper().Contains(sQuery)); // 檢查手機號碼
            }
            #endregion

            #region 排序
            int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
            string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
            string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

            bool bDescending = sortDirection.Equals("DESC");
            switch (sortColumn)
            {
                case "account":
                    query = bDescending ? query.OrderByDescending(o => o.Account) : query.OrderBy(o => o.Account);
                    break;
                case "email":
                    query = bDescending ? query.OrderByDescending(o => o.Email) : query.OrderBy(o => o.Email);
                    break;
                case "name":
                    query = bDescending ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name);
                    break;
                case "mobile":
                    query = bDescending ? query.OrderByDescending(o => o.Mobile) : query.OrderBy(o => o.Mobile);
                    break;
                case "birthday":
                    query = bDescending ? query.OrderByDescending(o => o.Birthday) : query.OrderBy(o => o.Birthday);
                    break;
                default:
                    query = query.OrderBy(o => o.Account);
                    break;
            }
            #endregion 排序

            // 獲取過濾後的記錄數
            var recordsFiltered = await query.CountAsync();

            // 根據分頁參數獲取當前頁的數據
            var list = recordsFiltered == 0
                ? new List<User>() // 如果沒有過濾後的記錄，返回空列表
                : query.Skip(start).Take(Math.Min(length, recordsFiltered - start)).ToList(); // 分頁查詢

            // 構建返回給 DataTable 的數據對象
            dynamic dataTableData = new
            {
                draw = draw, // DataTable 的 draw 參數
                data = list, // 當前頁的數據
                recordsTotal = recordsTotal, // 總記錄數
                recordsFiltered = recordsFiltered, // 過濾後的記錄數
            };

            // 返回 JSON 格式的數據
            return Json(dataTableData);
        }
        catch (Exception e)
        {
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(UserController)}.{nameof(GetData)}");

            // 構建返回的錯誤數據對象
            dynamic dataTableData = new
            {
                draw = draw, // DataTable 的 draw 參數
                data = Array.Empty<string>(), // 返回空數據
                recordsTotal = 0, // 總記錄數為 0
                recordsFiltered = 0, // 過濾後的記錄數為 0
                errorMessage = e.Message, // 錯誤信息
            };

            // 返回 JSON 格式的錯誤數據
            return Json(dataTableData);
        }
    }

    #endregion

    #region Create

    // GET: /User/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // 異步獲取用戶視圖模型，傳入 null 表示創建新用戶，第二個參數為 false 表示不使用只讀模式
        var model = await GetUserViewModelAsync(null, false);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/User/Default.cshtml", model);
    }

    // POST: /User/Create
    [HttpPost]
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊
    public async Task<IActionResult> Create(UserViewModel model)
    {
        try
        {
            // 設置模型為可編輯狀態
            model.IsReadonly = false;

            // 檢查模型狀態是否有效
            if (!ModelState.IsValid)
                // 如果無效，返回視圖並傳遞當前模型
                return View("~/Views/User/Default.cshtml", model);

            // 創建新的 User 實例並設置其屬性
            var user = new User
            {
                ID = model.ID, // 用戶 ID
                Account = model.Account.Trim(), // 用戶帳號，去除前後空格
                AccountNormalize = model.Account.Trim().ToUpper(), // 正規化帳號（轉為大寫）
                Email = model.Email.Trim(), // 用戶電子郵件，去除前後空格
                EmailNormalize = model.Email.Trim().ToUpper(), // 正規化電子郵件（轉為大寫）
                PasswordHash = AccountController.EncoderSHA512(model.Birthday.Value.ToString("yyyyMMdd")), // 使用生日生成密碼
                Name = model.Name.Trim(), // 用戶姓名，去除前後空格
                Mobile = model.Mobile.Trim(), // 用戶手機號碼，去除前後空格
                Birthday = model.Birthday, // 用戶生日
                CreatedDT = DateTime.Now, // 設置創建日期為當前時間
            };

            // 將新用戶添加到數據庫
            await _webServerDB.User.AddAsync(user);
            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(UserViewModel.ErrorMessage), e.Message);
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(UserController)}.{nameof(Create)}");
            // 返回視圖並傳遞當前模型
            return View("~/Views/User/Default.cshtml", model);
        }

        // 重定向到用戶列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Detail
    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 true
        var model = await GetUserViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/User/Default.cshtml", model);
    }
    #endregion

    #region Edit

    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 false
        var model = await GetUserViewModelAsync(id, false);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/User/Default.cshtml", model);
    }

    // 定義一個 HTTP POST 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpPost("{id:Guid}")]
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊
    public async Task<IActionResult> Edit(Guid id, UserViewModel model)
    {
        try
        {
            // 設置模型為可編輯狀態
            model.IsReadonly = false;

            // 檢查模型狀態是否有效
            if (!ModelState.IsValid)
                // 如果無效，返回視圖並傳遞當前模型
                return View("~/Views/User/Default.cshtml", model);

            // 創建新的 User 實例並設置其屬性
            var user = await _webServerDB.User.FindAsync(model.ID);
            user.Account = model.Account.Trim(); // 去除帳號前後空格
            user.AccountNormalize = model.Account.Trim().ToUpper(); // 正規化帳號為大寫
            user.Email = model.Email.Trim(); // 去除電子郵件前後空格
            user.EmailNormalize = model.Email.Trim().ToUpper(); // 正規化電子郵件為大寫
            user.Name = model.Name.Trim(); // 去除姓名前後空格
            user.Mobile = model.Mobile.Trim(); // 去除手機號碼前後空格
            user.Birthday = model.Birthday; // 設置生日
            user.ModifiedDT = DateTime.Now; // 設置修改時間為當前時間

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(UserViewModel.ErrorMessage), e.Message);
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(UserController)}.{nameof(Edit)}");
            // 返回視圖並傳遞當前模型
            return View("~/Views/User/Default.cshtml", model);
        }

        // 重定向到用戶列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Delete

    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 true
        var model = await GetUserViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/User/Default.cshtml", model);
    }

    // 定義一個 HTTP POST 請求的路由，要求 id 參數必須是 Guid 類型，並將動作名稱設置為 Delete
    [HttpPost("{id:Guid}"), ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            // 根據用戶 ID 查找用戶
            var user = await _webServerDB.User.FindAsync(id);
            // 從數據庫中刪除用戶
            _webServerDB.User.Remove(user);

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，重新獲取用戶視圖模型
            var model = await GetUserViewModelAsync(id, true);
            // 將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(UserViewModel.ErrorMessage), e.Message);
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(UserController)}.{nameof(DeleteConfirmed)}");
            // 返回視圖並傳遞當前模型
            return View("~/Views/User/Default.cshtml", model);
        }

        // 重定向到用戶列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region GetUserViewModelAsync
    // 取得使用者資料，並存成 UserViewModel
    private async Task<UserViewModel> GetUserViewModelAsync(Guid? id, bool isReadonly)
    {
        // 檢查是否提供了用戶 ID
        if (id.HasValue)
        {
            // 異步查找指定 ID 的用戶
            var user = await _webServerDB.User.FindAsync(id);

            // 如果找不到用戶，則拋出異常
            if (user == null)
                throw new Exception("查無此人");

            // 創建 UserViewModel 實例並填充用戶數據
            var model = new UserViewModel
            {
                ID = user.ID, // 設置用戶 ID
                Account = user.Account, // 設置用戶帳號
                Email = user.Email, // 設置用戶電子郵件
                Name = user.Name, // 設置用戶姓名
                Mobile = user.Mobile, // 設置用戶手機號碼
                Birthday = user.Birthday, // 設置用戶生日
                IsReadonly = isReadonly, // 設置是否為只讀模式
            };

            // 返回填充好的 UserViewModel
            return model;
        }
        else
        {
            // 如果沒有提供用戶 ID，則創建一個新的 UserViewModel 實例
            var model = new UserViewModel
            {
                ID = Guid.NewGuid(), // 生成新的 GUID 作為用戶 ID
                IsReadonly = isReadonly, // 設置是否為只讀模式
            };

            // 返回新的 UserViewModel
            return model;
        }
    }
    #endregion

}