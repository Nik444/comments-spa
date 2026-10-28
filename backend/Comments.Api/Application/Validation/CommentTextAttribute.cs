using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CommentTextAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text)
            return ValidationResult.Success;

        var errors = CommentTextValidator.Validate(text);

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(string.Join(" ", errors));
    }
}
