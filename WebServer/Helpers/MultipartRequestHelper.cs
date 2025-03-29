using Microsoft.Net.Http.Headers;

namespace WebServer.Helpers;

/// <summary>
/// 提供處理 multipart/form-data 請求的輔助方法。
/// </summary>
public static class MultipartRequestHelper
{
    /// <summary>
    /// 從 Content-Type 標頭中提取邊界字符串。
    /// </summary>
    /// <param name="contentType">包含邊界的 MediaTypeHeaderValue。</param>
    /// <param name="lengthLimit">邊界字符串的最大長度限制。</param>
    /// <returns>提取的邊界字符串。</returns>
    /// <exception cref="InvalidDataException">當缺少邊界或邊界長度超過限制時拋出。</exception>
    public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        // 移除邊界的引號並獲取其值
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        // 檢查邊界是否為空
        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }

        // 檢查邊界長度是否超過限制
        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException(
                $"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }

    /// <summary>
    /// 檢查指定的 Content-Type 是否為 multipart 類型。
    /// </summary>
    /// <param name="contentType">要檢查的 Content-Type 字符串。</param>
    /// <returns>如果是 multipart 類型，則返回 true；否則返回 false。</returns>
    public static bool IsMultipartContentType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType)
               && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 檢查 Content-Disposition 標頭是否表示表單數據。
    /// </summary>
    /// <param name="contentDisposition">要檢查的 ContentDispositionHeaderValue。</param>
    /// <returns>如果是表單數據，則返回 true；否則返回 false。</returns>
    public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
    {
        // 檢查 Content-Disposition 是否為 form-data，且沒有文件名
        return contentDisposition != null
            && contentDisposition.DispositionType.Equals("form-data")
            && string.IsNullOrEmpty(contentDisposition.FileName.Value)
            && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
    }

    /// <summary>
    /// 檢查 Content-Disposition 標頭是否表示文件數據。
    /// </summary>
    /// <param name="contentDisposition">要檢查的 ContentDispositionHeaderValue。</param>
    /// <returns>如果是文件數據，則返回 true；否則返回 false。</returns>
    public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
    {
        // 檢查 Content-Disposition 是否為 form-data，且有文件名
        return contentDisposition != null
            && contentDisposition.DispositionType.Equals("form-data")
            && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
    }
}