using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class HttpUrlAttribute() : ValidationAttribute("Адрес должен начинаться с http:// или https://.")
{
    public override bool IsValid(object? value)
    {
        return value is not string text || text.Length == 0 || HttpUrl.IsValid(text);
    }
}
