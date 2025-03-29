using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebServer.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        // 移除 FormValueProviderFactory，這樣就不會從表單數據中綁定模型
        factories.RemoveType<FormValueProviderFactory>();

        // 移除 FormFileValueProviderFactory，這樣就不會從上傳的文件中綁定模型
        factories.RemoveType<FormFileValueProviderFactory>();

        // 移除 JQueryFormValueProviderFactory，這樣就不會從 JQuery 表單數據中綁定模型
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}