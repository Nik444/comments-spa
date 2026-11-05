using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Comments.Api.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AttachmentFileAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
            return ValidationResult.Success;

        using var stream = file.OpenReadStream();
        var error = AttachmentValidator.Validate(file.FileName, stream);

        return error is null ? ValidationResult.Success : new ValidationResult(error);
    }
}
