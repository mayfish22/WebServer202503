using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;

namespace WebServer.Helpers;

public static class FileHelper
{
    // 如果需要在 IsValidFileExtensionAndSignature 方法中檢查特定字符，
    // 請在 _allowedChars 字段中提供這些字符。
    private static readonly byte[] _allowedChars = { };

    // 有關更多文件簽名，請參見文件簽名數據庫 (https://www.filesignatures.net/)
    // 以及您希望添加的文件類型的官方規範。
    private static readonly Dictionary<string, List<byte[]>> _fileSignature = new Dictionary<string, List<byte[]>>
    {
        // GIF 文件的簽名
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },

        // PNG 文件的簽名
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },

        // JPEG 文件的簽名
        { ".jpeg", new List<byte[]>
            {
                // JPEG 文件的不同簽名變體
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
            }
        },

        // JPG 文件的簽名
        { ".jpg", new List<byte[]>
            {
                // JPG 文件的不同簽名變體
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
            }
        },

        // ZIP 文件的簽名
        { ".zip", new List<byte[]>
            {
                // ZIP 文件的不同簽名變體
                new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                new byte[] { 0x50, 0x4B, 0x4C, 0x49, 0x54, 0x45 },
                new byte[] { 0x50, 0x4B, 0x53, 0x70, 0x58 },
                new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                new byte[] { 0x50, 0x4B, 0x07, 0x08 },
                new byte[] { 0x57, 0x69, 0x6E, 0x5A, 0x69, 0x70 },
            }
        },
    };

    // **WARNING!**
    // 在以下文件處理方法中，文件的內容不會被掃描。
    // 在大多數生產場景中，會在將文件提供給用戶或其他系統之前，
    // 使用防病毒/防惡意軟件掃描器 API 對文件進行掃描。
    // 有關更多信息，請參見隨附此示例應用程序的主題。

    /// <summary>
    /// 處理上傳的表單文件，檢查其有效性並返回其內容的字節數組。
    /// </summary>
    /// <typeparam name="T">與上傳的文件相關聯的模型類型。</typeparam>
    /// <param name="formFile">上傳的表單文件。</param>
    /// <param name="modelState">模型狀態字典，用於存儲驗證錯誤。</param>
    /// <param name="permittedExtensions">允許的文件擴展名列表。</param>
    /// <param name="sizeLimit">文件大小限制（以字節為單位）。</param>
    /// <returns>文件內容的字節數組；如果處理失敗，則返回空數組。</returns>
    public static async Task<byte[]> ProcessFormFile<T>(IFormFile formFile,
        ModelStateDictionary modelState, string[] permittedExtensions,
        long sizeLimit)
    {
        var fieldDisplayName = string.Empty;

        // 使用反射獲取與此 IFormFile 相關聯的模型屬性的顯示名稱。
        // 如果找不到顯示名稱，錯誤消息將不會顯示顯示名稱。
        MemberInfo property =
            typeof(T).GetProperty(
                formFile.Name.Substring(formFile.Name.IndexOf(".",
                StringComparison.Ordinal) + 1));

        if (property != null)
        {
            // 獲取屬性的 DisplayAttribute 以獲取顯示名稱
            if (property.GetCustomAttribute(typeof(DisplayAttribute)) is
                DisplayAttribute displayAttribute)
            {
                fieldDisplayName = $"{displayAttribute.Name} ";
            }
        }

        // 不信任客戶端發送的文件名。為了顯示文件名，對值進行 HTML 編碼。
        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
            formFile.FileName);

        // 檢查文件長度。此檢查不會捕獲僅包含 BOM 的文件。
        if (formFile.Length == 0)
        {
            modelState.AddModelError(formFile.Name,
                $"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");

            return new byte[0]; // 返回空數組
        }

        // 檢查文件大小是否超過限制
        if (formFile.Length > sizeLimit)
        {
            var megabyteSizeLimit = sizeLimit / 1048576; // 將字節轉換為 MB
            modelState.AddModelError(formFile.Name,
                $"{fieldDisplayName}({trustedFileNameForDisplay}) exceeds " +
                $"{megabyteSizeLimit:N1} MB.");

            return new byte[0]; // 返回空數組
        }

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                // 將文件內容複製到內存流中
                await formFile.CopyToAsync(memoryStream);

                // 檢查內容長度，以防文件的唯一內容是 BOM，並且在去除 BOM 後內容實際上為空。
                if (memoryStream.Length == 0)
                {
                    modelState.AddModelError(formFile.Name,
                        $"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");
                }

                // 驗證文件擴展名和簽名
                if (!IsValidFileExtensionAndSignature(
                    formFile.FileName, memoryStream, permittedExtensions))
                {
                    modelState.AddModelError(formFile.Name,
                        $"{fieldDisplayName}({trustedFileNameForDisplay}) file " +
                        "type isn't permitted or the file's signature " +
                        "doesn't match the file's extension.");
                }
                else
                {
                    return memoryStream.ToArray(); // 返回文件內容的字節數組
                }
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並添加錯誤到模型狀態
            modelState.AddModelError(formFile.Name,
                $"{fieldDisplayName}({trustedFileNameForDisplay}) upload failed. " +
                $"Please contact the Help Desk for support. Error: {ex.HResult}");
            // 記錄異常
        }

        return new byte[0]; // 返回空數組
    }

    /// <summary>
    /// 處理上傳的流式文件，檢查其有效性並返回其內容的字節數組。
    /// </summary>
    /// <param name="section">包含文件內容的多部分部分。</param>
    /// <param name="contentDisposition">文件的內容處置標頭，包含文件名等信息。</param>
    /// <param name="modelState">模型狀態字典，用於存儲驗證錯誤。</param>
    /// <param name="permittedExtensions">允許的文件擴展名列表。</param>
    /// <param name="sizeLimit">文件大小限制（以字節為單位）。</param>
    /// <returns>文件內容的字節數組；如果處理失敗，則返回空數組。</returns>
    public static async Task<byte[]> ProcessStreamedFile(
        MultipartSection section, ContentDispositionHeaderValue contentDisposition,
        ModelStateDictionary modelState, string[] permittedExtensions, long sizeLimit)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                // 將流式文件的內容複製到內存流中
                await section.Body.CopyToAsync(memoryStream);

                // 檢查文件是否為空或是否超過大小限制
                if (memoryStream.Length == 0)
                {
                    modelState.AddModelError("File", "The file is empty.");
                }
                else if (memoryStream.Length > sizeLimit)
                {
                    var megabyteSizeLimit = sizeLimit / 1048576; // 將字節轉換為 MB
                    modelState.AddModelError("File",
                    $"The file exceeds {megabyteSizeLimit:N1} MB.");
                }
                // 檢查文件擴展名和簽名的有效性
                else if (permittedExtensions != null && permittedExtensions.Length > 0 &&
                    !IsValidFileExtensionAndSignature(
                    contentDisposition.FileName.Value, memoryStream,
                    permittedExtensions))
                {
                    modelState.AddModelError("File",
                        "The file type isn't permitted or the file's " +
                        "signature doesn't match the file's extension.");
                }
                else
                {
                    return memoryStream.ToArray(); // 返回文件內容的字節數組
                }
            }
        }
        catch (Exception ex)
        {
            // 捕獲異常並添加錯誤到模型狀態
            modelState.AddModelError("File",
                "The upload failed. Please contact the Help Desk " +
                $" for support. Error: {ex.HResult}");
            // 記錄異常
        }

        return new byte[0]; // 返回空數組
    }

    /// <summary>
    /// 驗證文件的擴展名和內容簽名，以確保文件的安全性和有效性。
    /// </summary>
    /// <param name="fileName">上傳文件的名稱。</param>
    /// <param name="data">文件內容的流。</param>
    /// <param name="permittedExtensions">允許的文件擴展名列表。</param>
    /// <returns>如果文件的擴展名和內容簽名有效，則返回 true；否則返回 false。</returns>
    private static bool IsValidFileExtensionAndSignature(string fileName, Stream data, string[] permittedExtensions)
    {
        // 檢查文件名、數據流是否為空或數據流長度是否為零
        if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
        {
            return false; // 無效的文件
        }

        // 獲取文件擴展名並轉換為小寫
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        // 檢查擴展名是否為空或不在允許的擴展名列表中
        if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
        {
            return false; // 無效的擴展名
        }

        // 將數據流的位置重置為開頭
        data.Position = 0;

        using (var reader = new BinaryReader(data))
        {
            // 檢查特定擴展名的文件內容
            if (ext.Equals(".txt") || ext.Equals(".csv") || ext.Equals(".prn"))
            {
                if (_allowedChars.Length == 0)
                {
                    // 限制字符為 ASCII 編碼
                    for (var i = 0; i < data.Length; i++)
                    {
                        if (reader.ReadByte() > sbyte.MaxValue)
                        {
                            return false; // 包含非 ASCII 字符
                        }
                    }
                }
                else
                {
                    // 限制字符為 ASCII 編碼和 _allowedChars 陣列中的值
                    for (var i = 0; i < data.Length; i++)
                    {
                        var b = reader.ReadByte();
                        if (b > sbyte.MaxValue || !_allowedChars.Contains(b))
                        {
                            return false; // 包含不允許的字符
                        }
                    }
                }

                return true; // 文件內容有效
            }

            // 如果需要允許未在 _fileSignature 字典中提供簽名的文件，請取消註釋以下代碼塊。
            // 我們建議為所有計劃在系統上允許的文件類型添加文件簽名並執行簽名檢查。
            /*
            if (!_fileSignature.ContainsKey(ext))
            {
                return true; // 如果未提供簽名，則視為有效
            }
            */

            // 文件簽名檢查
            // --------------------
            // 使用 _fileSignature 字典中提供的文件簽名，以下代碼測試輸入內容的文件簽名。
            var signatures = _fileSignature[ext];
            var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length)); // 讀取最大簽名長度的字節

            // 檢查讀取的字節是否與任何簽名匹配
            return signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }
    }

}