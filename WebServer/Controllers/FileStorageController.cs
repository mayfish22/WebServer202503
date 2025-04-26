using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using WebServer.Attributes;
using WebServer.Helpers;
using WebServer.Hubs;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

[Authorize] // 設定此控制器需要授權
[Route("{controller}/{action=Index}")] // 設定控制器的路由格式
public class FileStorageController : Controller
{
    private readonly ILogger<FileStorageController> _logger;
    private static readonly FormOptions _defaultFormOptions = new FormOptions();
    private readonly string[] _permittedExtensions = new string[] { ".jpg", ".png" }; // 允許的檔案類型
    private readonly long _fileSizeLimit = 50 * 1024 * 1024; // 50MB，設定檔案大小上限
    private readonly string _targetFilePath; // 儲存檔案的路徑
    private readonly WebServerDBContext _webServerDB; // 資料庫上下文
    private readonly IHttpContextAccessor _httpContext; // 用於存取 HTTP 上下文
    private readonly IHubContext<FaceHub> _faceHub;

    // 建構子，初始化日誌記錄器、資料庫上下文和 HTTP 上下文
    public FileStorageController(ILogger<FileStorageController> logger, 
        WebServerDBContext webServerDB, 
        IHttpContextAccessor httpContext,
        IHubContext<FaceHub> faceHub)
    {
        _logger = logger;
        _webServerDB = webServerDB;
        _httpContext = httpContext;
        _faceHub = faceHub;

        // 設定檔案儲存的目標路徑
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _targetFilePath = Path.Combine(programDataPath, "WebServer", "files");
        // 檢查資料夾是否存在，如果不存在則創建它
        if (!Directory.Exists(_targetFilePath))
        {
            Directory.CreateDirectory(_targetFilePath); // 創建資料夾
        }
    }

    #region Upload（上傳檔案）

    [HttpPost] // 定義 HTTP POST 方法
    [AllowAnonymous] // 允許匿名使用者存取
    [DisableFormValueModelBinding] // 禁用模型綁定，避免影響檔案上傳
    public async Task<IActionResult> Upload()
    {
        try
        {
            var ids = new List<Guid>(); // 儲存上傳檔案的 ID

            // 檢查請求是否為 Multipart Content-Type
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "請求無法處理（錯誤 1）。");
                return BadRequest(ModelState);
            }

            #region 取得使用者資訊
            Guid? userId = null;
            var httpContext = _httpContext.HttpContext;
            if (httpContext != null)
            {
                var user = httpContext.User;
                if (user.Identity.IsAuthenticated)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid tmp))
                        userId = tmp;
                }
            }
            #endregion

            var formAccumulator = new KeyValueAccumulator(); // 用於儲存表單資料
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var streamedFileContent = Array.Empty<byte>();

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    // 處理檔案部分
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);
                        streamedFileContent = await FileHelper.ProcessStreamedFile(section, contentDisposition, ModelState, _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        var fileId = Guid.NewGuid();
                        var filePath = Path.Combine(_targetFilePath, fileId.ToString());
                        using (var targetStream = System.IO.File.Create(filePath))
                        {
                            await targetStream.WriteAsync(streamedFileContent);
                        }

                        // 將檔案資訊儲存到資料庫
                        await _webServerDB.FileStorage.AddAsync(new WebServer.Models.WebServerDB.FileStorage
                        {
                            ID = fileId,
                            Type = nameof(Upload),
                            FileName = trustedFileNameForDisplay,
                            FileSize = streamedFileContent.Length,
                            Path = filePath,
                            CreatedUserID = userId,
                            CreatedDT = DateTime.Now,
                        });
                        await _webServerDB.SaveChangesAsync();
                        ids.Add(fileId);
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return Json(new { ids = ids });
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "上傳檔案時發生錯誤");
            return BadRequest(e.Message);
        }
    }
    #endregion

    #region Download（下載檔案）

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var file = await _webServerDB.FileStorage.FindAsync(id);
            if (file == null)
                throw new Exception("找不到檔案編號");

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(file.FileName, out string contentType))
            {
                contentType = "application/octet-stream";
            }

            using (FileStream fsSource = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fsSource.Length];
                await fsSource.ReadAsync(bytes, 0, bytes.Length);
                return new FileStreamResult(new MemoryStream(bytes), contentType)
                {
                    FileDownloadName = file.FileName,
                };
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "下載檔案時發生錯誤");
            return BadRequest(e.Message);
        }
    }
    #endregion

    #region Index
    // GET: /Product/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield();  // 讓出控制權，允許其他任務執行
        return View("~/Views/FileStorage/Index.cshtml");  // 返回指定的視圖
    }

    // POST: /FileStorage/GetData
    [HttpPost]
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            // 從資料庫中查詢 FileStorage 表
            var query = from n1 in _webServerDB.FileStorage
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
                query = query.Where(t => t.FileName.ToUpper().Contains(sQuery));
            }
            #endregion

            #region 排序
            // 獲取排序的列索引和方向
            int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
            string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
            string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

            // 根據排序方向和列進行排序
            bool bDescending = sortDirection.Equals("DESC");
            switch (sortColumn)
            {
                case nameof(FileStorage.FileName):
                    query = bDescending ? query.OrderByDescending(o => o.FileName) : query.OrderBy(o => o.FileName);
                    break;
                case nameof(FileStorage.FileSize):
                    query = bDescending ? query.OrderByDescending(o => o.FileSize) : query.OrderBy(o => o.FileSize);
                    break;
                case nameof(FileStorage.Path):
                    query = bDescending ? query.OrderByDescending(o => o.Path) : query.OrderBy(o => o.Path);
                    break;
                case nameof(FileStorage.CreatedDT):
                    query = bDescending ? query.OrderByDescending(o => o.CreatedDT) : query.OrderBy(o => o.CreatedDT);
                    break;
                default:
                    query = query.OrderByDescending(o => o.CreatedDT);  // 默認排序
                    break;
            }
            #endregion 排序

            // 獲取過濾後的記錄數
            var recordsFiltered = await query.CountAsync();

            // 根據分頁參數獲取當前頁的數據
            var list = recordsFiltered == 0
                ? new List<FileStorage>() // 如果沒有過濾後的記錄，返回空列表
                : query.Skip(start).Take(Math.Min(length, recordsFiltered - start)).ToList(); // 分頁查詢

            list.ForEach(s =>
            {
                s.FileURL = $"/FileStorage/Download/{s.ID}";
            });

            // 構建返回給 DataTable 的數據對象
            dynamic dataTableData = new
            {
                draw = draw, // DataTable 的 draw 參數
                data = list, // 當前頁的數據
                recordsTotal = recordsTotal, // 總記錄數
                recordsFiltered = recordsFiltered, // 過濾後的記錄數
            };

            // 返回 JSON 格式的數據
            return Json(dataTableData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持屬性名稱不變
            });
        }
        catch (Exception e)
        {
            // 記錄錯誤信息
            _logger.LogError(e, $"{nameof(FileStorage)}.{nameof(GetData)}");

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
            return Json(dataTableData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持屬性名稱不變
            });
        }
    }
    #endregion

    #region UploadFaceFeature（上傳人臉特徵）
    [HttpPost] // 定義 HTTP POST 方法
    [AllowAnonymous] // 允許匿名使用者存取
    [DisableFormValueModelBinding] // 禁用模型綁定，避免影響檔案上傳
    public async Task<IActionResult> UploadFaceFeature()
    {
        try
        {
            var ids = new List<Guid>(); // 儲存上傳檔案的 ID

            // 檢查請求是否為 Multipart Content-Type
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "請求無法處理（錯誤 1）。");
                return BadRequest(ModelState);
            }

            #region 取得使用者資訊
            Guid? userId = null;
            var httpContext = _httpContext.HttpContext;
            if (httpContext != null)
            {
                var user = httpContext.User;
                if (user.Identity.IsAuthenticated)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid tmp))
                        userId = tmp;
                }
            }
            #endregion

            var formAccumulator = new KeyValueAccumulator(); // 用於儲存表單資料
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var streamedFileContent = Array.Empty<byte>();

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    // 處理檔案部分
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);
                        //不檢查檔案類型
                        streamedFileContent = await FileHelper.ProcessStreamedFile(section, contentDisposition, ModelState, null, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        var fileId = Guid.NewGuid();
                        var filePath = Path.Combine(_targetFilePath, fileId.ToString());
                        using (var targetStream = System.IO.File.Create(filePath))
                        {
                            await targetStream.WriteAsync(streamedFileContent);
                        }

                        // 將檔案資訊儲存到資料庫
                        var fileStorage = new WebServer.Models.WebServerDB.FileStorage
                        {
                            ID = fileId,
                            Type = nameof(Upload),
                            FileName = trustedFileNameForDisplay,
                            FileSize = streamedFileContent.Length,
                            Path = filePath,
                            CreatedUserID = userId,
                            CreatedDT = DateTime.Now,
                        };
                        var faceFeature = new WebServer.Models.WebServerDB.FaceFeature
                        {
                            ID = fileId,
                            FileStorageID = fileStorage.ID,
                            Name = $"人臉特徵{DateTime.Now:yyyyMMddHHmmss}",
                            CreatedDT = DateTime.Now,
                        };
                        await _webServerDB.FileStorage.AddAsync(fileStorage);
                        await _webServerDB.FaceFeature.AddAsync(faceFeature);
                        await _webServerDB.SaveChangesAsync();
                        ids.Add(fileId);
                        // 推送檔案ID到前端
                        await _faceHub.Clients.All.SendAsync("ReceiveFaceFeature", fileId.ToString());
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return Json(new { ids = ids });
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "上傳人臉特徵時發生錯誤");
            return BadRequest(e.Message);
        }
    }
    #endregion

    #region UploadFaceImage（上傳人臉照片）

    [HttpPost] // 定義 HTTP POST 方法
    [AllowAnonymous] // 允許匿名使用者存取
    [DisableFormValueModelBinding] // 禁用模型綁定，避免影響檔案上傳
    public async Task<IActionResult> UploadFaceImage()
    {
        try
        {
            var ids = new List<Guid>(); // 儲存上傳檔案的 ID

            // 檢查請求是否為 Multipart Content-Type
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "請求無法處理（錯誤 1）。");
                return BadRequest(ModelState);
            }

            #region 取得使用者資訊
            Guid? userId = null;
            var httpContext = _httpContext.HttpContext;
            if (httpContext != null)
            {
                var user = httpContext.User;
                if (user.Identity.IsAuthenticated)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid tmp))
                        userId = tmp;
                }
            }
            #endregion

            var formAccumulator = new KeyValueAccumulator(); // 用於儲存表單資料
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var streamedFileContent = Array.Empty<byte>();

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    // 處理檔案部分
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);
                        streamedFileContent = await FileHelper.ProcessStreamedFile(section, contentDisposition, ModelState, _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        var fileId = Guid.NewGuid();
                        var filePath = Path.Combine(_targetFilePath, fileId.ToString());
                        using (var targetStream = System.IO.File.Create(filePath))
                        {
                            await targetStream.WriteAsync(streamedFileContent);
                        }

                        // 將檔案資訊儲存到資料庫
                        await _webServerDB.FileStorage.AddAsync(new WebServer.Models.WebServerDB.FileStorage
                        {
                            ID = fileId,
                            Type = nameof(Upload),
                            FileName = trustedFileNameForDisplay,
                            FileSize = streamedFileContent.Length,
                            Path = filePath,
                            CreatedUserID = userId,
                            CreatedDT = DateTime.Now,
                        });
                        await _webServerDB.SaveChangesAsync();
                        ids.Add(fileId);
                        // 推送檔案ID到前端
                        await _faceHub.Clients.All.SendAsync("ReceiveFaceImage", fileId.ToString());
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }

            return Json(new { ids = ids });
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "上傳檔案時發生錯誤");
            return BadRequest(e.Message);
        }
    }
    #endregion
}