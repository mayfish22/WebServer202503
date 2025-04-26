using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

[ModelMetadataType(typeof(EmployeeMetadata))]
public partial class Employee
{
    /// <summary>
    /// 大頭照檔案下載網址
    /// </summary>
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? PhotoURL { get; set; }
    /// <summary>
    /// 人臉特徵檔下載網址
    /// </summary>
    [NotMapped]  // 指定此屬性不應映射到資料庫
    public string? FaceFeatureURL { get; set; }
}

public partial class EmployeeMetadata
{
    /// <summary>
    /// 員工唯一識別碼（GUID），主鍵，系統自動產生。
    /// </summary>
    [Display(Name= "唯一識別碼")]
    public Guid ID { get; set; }

    /// <summary>
    /// 員工編號（例如：EMP001），公司內部代碼，不可重複。
    /// </summary>
    [Display(Name = "員工編號")]
    [Required(ErrorMessage = "請輸入員工編號。")]
    [MaxLength(20)]
    public string No { get; set; }

    /// <summary>
    /// 員工的中文或英文姓名。
    /// </summary>
    [Display(Name = "姓名")]
    [Required(ErrorMessage = "請輸入員工姓名。")]
    [MaxLength(20)]
    public string Name { get; set; }

    /// <summary>
    /// 員工的電子郵件地址，用於通知與聯絡。
    /// </summary>
    [Display(Name = "電子郵件")]
    [MaxLength(50)]
    public string Email { get; set; }

    /// <summary>
    /// 員工的行動電話號碼，用於聯絡用途。
    /// </summary>
    [Display(Name = "行動電話")]
    [MaxLength(50)]
    public string Mobile { get; set; }

    /// <summary>
    /// 員工正式入職公司的日期。
    /// </summary>
    [Display(Name = "到職日期")]
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// 大頭照對應的 FileStorage 資料表主鍵 ID。
    /// </summary>
    [Display(Name = "大頭照")]
    public Guid? Photo { get; set; }

    /// <summary>
    /// 人臉辨識特徵檔案對應的 FileStorage 資料表主鍵 ID。
    /// </summary>
    [Display(Name = "人臉特徵檔")]
    public Guid? FaceFeature { get; set; }

    /// <summary>
    /// 建立時間（系統自動記錄）。
    /// </summary>
    [Display(Name = "建立時間")]
    public DateTime CreatedDT { get; set; }

    /// <summary>
    /// 最後一次修改此筆資料的時間戳記，可為 NULL。
    /// </summary>
    [Display(Name = "修改時間")]
    public DateTime? ModifiedDT { get; set; }
}