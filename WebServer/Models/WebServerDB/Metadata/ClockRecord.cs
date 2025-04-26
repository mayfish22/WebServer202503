using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.WebServerDB;

[ModelMetadataType(typeof(ClockRecordMetadata))]
public partial class ClockRecord
{
    /// <summary>
    /// 員工編號
    /// </summary>
    [NotMapped]  // 指定此屬性不應映射到資料庫
    [Display(Name = "員工編號")]
    public string? EmployeeNo { get; set; }
    /// <summary>
    /// 員工姓名
    /// </summary>
    [NotMapped]  // 指定此屬性不應映射到資料庫
    [Display(Name = "員工姓名")]
    public string? EmployeeName { get; set; }
}

public partial class ClockRecordMetadata
{
    /// <summary>
    /// 打卡記錄唯一識別碼 (GUID)
    /// </summary>
    [Display(Name = "唯一識別碼")]
    public Guid ID { get; set; }

    /// <summary>
    /// 關聯 Employee.ID 的外鍵 (員工識別碼)
    /// </summary>
    public Guid EmployeeID { get; set; }

    /// <summary>
    /// 打卡發生的日期與時間，精確到毫秒
    /// </summary>
    [Display(Name = "打卡時間")]
    public DateTime ClockDateTime { get; set; }

    /// <summary>
    /// 打卡類型，如 In / Out / Break / Overtime 等
    /// </summary>
    [Display(Name = "打卡類型")]
    public string Type { get; set; }

    /// <summary>
    /// 打卡地點或裝置標識 (可選，若沒有可為 NULL)
    /// </summary>
    [Display(Name = "打卡地點")]
    public string Location { get; set; }

    /// <summary>
    /// 此筆紀錄的建立時間 (UTC 時間)
    /// </summary>
    [Display(Name = "建立時間")]
    public DateTime CreatedDT { get; set; }

    public virtual Employee Employee { get; set; }
}