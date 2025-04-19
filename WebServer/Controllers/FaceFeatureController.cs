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
public class FaceFeatureController : Controller
{
    private readonly WebServerDBContext _webServerDB;

    // 控制器的建構函數
    public FaceFeatureController(WebServerDBContext webServerDB)
    {
        // 將傳入的上下文實例賦值給私有字段
        _webServerDB = webServerDB;
    }

    #region Index
    // 使用 [HttpGet] 特性標記此方法為處理HTTP GET請求的行為方法
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield();
        return View("~/Views/FaceFeature/Index.cshtml");
    }

    [HttpPost] // 指定此方法為 HTTP POST 請求
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            // 從資料庫中查詢 User 表
            var query = from n1 in _webServerDB.FaceFeature
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
                query = query.Where(t => t.Name.Contains(sQuery)); // 檢查名稱
            }
            #endregion

            #region 排序
            int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
            string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
            string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

            bool bDescending = sortDirection.Equals("DESC");
            switch (sortColumn)
            {
                case "name":
                    query = bDescending ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name);
                    break;
                case "createdDT":
                    query = bDescending ? query.OrderByDescending(o => o.CreatedDT) : query.OrderBy(o => o.CreatedDT);
                    break;
                case "modifiedDT":
                    query = bDescending ? query.OrderByDescending(o => o.ModifiedDT) : query.OrderBy(o => o.ModifiedDT);
                    break;
                default:
                    query = query.OrderByDescending(o => o.CreatedDT);
                    break;
            }
            #endregion 排序

            // 獲取過濾後的記錄數
            var recordsFiltered = await query.CountAsync();

            // 根據分頁參數獲取當前頁的數據
            var list = recordsFiltered == 0
                ? new List<FaceFeature>() // 如果沒有過濾後的記錄，返回空列表
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

    #region Detail
    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 true
        var model = await GetFaceFeatureViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/FaceFeature/Default.cshtml", model);
    }
    #endregion

    #region Edit

    // 定義一個 HTTP GET 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        // 異步獲取用戶視圖模型，傳入用戶 ID 和只讀模式為 false
        var model = await GetFaceFeatureViewModelAsync(id, false);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/FaceFeature/Default.cshtml", model);
    }

    // 定義一個 HTTP POST 請求的路由，要求 id 參數必須是 Guid 類型
    [HttpPost("{id:Guid}")]
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊
    public async Task<IActionResult> Edit(Guid id, FaceFeatureViewModel model)
    {
        try
        {
            // 設置模型為可編輯狀態
            model.IsReadonly = false;

            // 檢查模型狀態是否有效
            if (!ModelState.IsValid)
                // 如果無效，返回視圖並傳遞當前模型
                return View("~/Views/FaceFeature/Default.cshtml", model);
          
            var data = await _webServerDB.FaceFeature.FindAsync(model.FaceFeature.ID);
            data.Name = model.FaceFeature.Name.Trim(); // 去除名稱前後空格
            data.ModifiedDT = DateTime.Now; // 設置修改時間為當前時間

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(FaceFeatureViewModel.ErrorMessage), e.Message);
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(FaceFeatureViewModel)}.{nameof(Edit)}");
            // 返回視圖並傳遞當前模型
            return View("~/Views/FaceFeature/Default.cshtml", model);
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
        var model = await GetFaceFeatureViewModelAsync(id, true);

        // 返回視圖，並將模型傳遞給視圖
        return View("~/Views/FaceFeature/Default.cshtml", model);
    }

    // 定義一個 HTTP POST 請求的路由，要求 id 參數必須是 Guid 類型，並將動作名稱設置為 Delete
    [HttpPost("{id:Guid}"), ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken] // 防止跨站請求偽造攻擊
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            var data = await _webServerDB.FaceFeature.FindAsync(id);
            _webServerDB.FaceFeature.Remove(data);

            // 保存更改到數據庫
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // 如果發生異常，重新獲取用戶視圖模型
            var model = await GetFaceFeatureViewModelAsync(id, true);
            // 將錯誤信息添加到模型狀態中
            ModelState.AddModelError(nameof(FaceFeatureViewModel.ErrorMessage), e.Message);
            // 記錄錯誤信息
            Log.Error(e, $"{nameof(FaceFeatureController)}.{nameof(DeleteConfirmed)}");
            // 返回視圖並傳遞當前模型
            return View("~/Views/FaceFeature/Default.cshtml", model);
        }

        // 重定向到用戶列表頁面
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region 下載檔案 
    [AllowAnonymous] // 允許未經授權的用戶訪問此 Action
    [HttpGet("{index}")] // 設定 HTTP GET 請求的路徑和參數。{id} 和 {index} 是路由參數。
                              // 雖然路由中定義了 id，但在程式碼中只使用了 index 參數來查詢檔案。
    public async Task<IActionResult> Download(int index) // 定義一個異步的 Action 方法，接收一個 int 類型的 index 參數
    {
        try // 使用 try-catch 塊來捕獲可能發生的錯誤
        {
            if(index < 0)
                throw new Exception("索引必須大於等於 0"); // 如果 index 小於 0，則拋出異常
            // 查詢資料庫，從 FaceFeature 和 FileStorage 兩個表中聯接 (Join)
            // 條件是 FaceFeature 的 FileStorageID 等於 FileStorage 的 ID
            // 按照 ModifiedDT 欄位（修改日期時間）降序排列
            // 最後選擇 FileStorage 的所有欄位
            var query = from n1 in _webServerDB.FaceFeature
                        join n2 in _webServerDB.FileStorage on n1.FileStorageID equals n2.ID
                        orderby n1.ModifiedDT descending
                        select new
                        {
                            Path = n2.Path,
                            FileName = n1.Name,
                        };

            // 根據傳入的 index，跳過前 index 筆記錄，然後取出後 1 筆記錄
            // 並非下載特定 id 的檔案，而是根據 index 在排序後的結果中選擇檔案
            var file = await query.Skip(index).Take(1).FirstOrDefaultAsync();

            // 如果找不到對應的檔案記錄，拋出異常
            if (file == null)
                throw new Exception("找不到檔案");

            // 創建一個 FileExtensionContentTypeProvider 來根據檔案副檔名判斷內容類型 (MIME Type)
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            string contentType;
            // 嘗試獲取檔案的內容類型，如果失敗，則設定為預設的二進位流類型
            if (!provider.TryGetContentType(file.FileName, out contentType))
            {
                contentType = "application/octet-stream"; // 預設的二進位流類型
            }

            // 使用 using 語句確保 FileStream 在使用後被正確關閉
            // 根據資料庫中儲存的檔案路徑 (file.Path) 打開檔案
            using (FileStream fsSource = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
            {
                // 創建一個位元組陣列來儲存檔案內容，大小為檔案的長度
                byte[] bytes = new byte[fsSource.Length];
                // 異步讀取檔案內容到位元組陣列中
                await fsSource.ReadAsync(bytes, 0, bytes.Length);

                // 回傳一個 FileStreamResult，將檔案內容作為一個記憶體流回傳
                // 設定內容類型和建議的下載檔案名稱
                return new FileStreamResult(new MemoryStream(bytes), contentType)
                {
                    FileDownloadName = file.FileName, // 設定下載到客戶端時的檔案名稱
                };
            }
        }
        catch (Exception e) // 捕獲任何在 try 塊中發生的異常
        {
            // 使用 Log 記錄錯誤信息，包含異常 e 和錯誤訊息
            Log.Error(e, "下載檔案時發生錯誤");
            // 回傳一個 BadRequest (HTTP 400) 結果，並帶上錯誤訊息
            return BadRequest(e.Message);
        }
    }
    #endregion

    #region GetFaceFeatureViewModelAsync
    // 取得使用者資料，並存成 GetFaceFeatureViewModelAsync
    private async Task<FaceFeatureViewModel> GetFaceFeatureViewModelAsync(Guid? id, bool isReadonly)
    {
        // 檢查是否提供 ID
        if (id.HasValue)
        {
            // 異步查找指定 ID 的用戶
            var faceFeature = await _webServerDB.FaceFeature.FindAsync(id);

            // 如果找不到，則拋出異常
            if (faceFeature == null)
                throw new Exception("查無資料");

            // 創建 FaceFeatureViewModel 實例並填充FaceFeature數據
            var model = new FaceFeatureViewModel
            {
                FaceFeature = faceFeature,
                IsReadonly = isReadonly, // 設置是否為只讀模式
            };

            return model;
        }
        else
        {
            // 如果沒有提供 ID，則創建一個新的 FaceFeatureViewModel 實例
            var model = new FaceFeatureViewModel
            {
                FaceFeature = new FaceFeature
                {
                    ID = Guid.NewGuid(), // 設置新的唯一 ID
                }, 
                IsReadonly = isReadonly, // 設置是否為只讀模式
            };

            // 返回新的 FaceFeatureViewModel
            return model;
        }
    }
    #endregion
}