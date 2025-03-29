using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

[ModelMetadataType(typeof(FileStorageMetadata))]
public partial class FileStorage 
{
    /// <summary>
    /// 檔案下載網址
    /// </summary>
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? FileURL { get; set; }
}

public partial class FileStorageMetadata
{
    public Guid ID { get; set; }
    [Display(Name = "類別")]
    public string Type { get; set; }
    [Display(Name = "檔案名稱")]
    public string FileName { get; set; }
    [Display(Name = "檔案大小")]
    public int FileSize { get; set; }
    [Display(Name = "檔案實體路徑")]
    public string Path { get; set; }
    [Display(Name = "創建人員")]  // 設定顯示名稱為「創建人員」
    public Guid? CreatedUserID { get; set; }
    [Display(Name = "創建時間")]  // 設定顯示名稱為「創建時間」
    public DateTime CreatedDT { get; set; }
}