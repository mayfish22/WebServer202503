using System.ComponentModel.DataAnnotations;
using WebServer.Models.WebServerDB;

namespace WebServer.Models.ViewModels;

public class FaceFeatureViewModel : IValidatableObject
{
    public bool IsReadonly { get; set; }
    public string? ErrorMessage { get; set; }
    public FaceFeature? FaceFeature { get; set; }

    /// <summary>
    /// 自定義驗證邏輯
    /// </summary>
    /// <param name="validationContext">驗證上下文，包含服務和其他信息</param>
    /// <returns>返回驗證結果的集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return Enumerable.Empty<ValidationResult>();
    }
}