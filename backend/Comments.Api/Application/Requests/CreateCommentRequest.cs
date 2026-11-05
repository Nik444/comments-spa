using System.ComponentModel.DataAnnotations;
using Comments.Api.Application.Validation;
using Comments.Api.Domain;
using Microsoft.AspNetCore.Http;

namespace Comments.Api.Application.Requests;

public sealed record CreateCommentRequest(
    [Required, StringLength(CommentLimits.UserNameMax), RegularExpression(CommentLimits.UserNamePattern, ErrorMessage = "Имя может содержать только латинские буквы и цифры.")] string UserName,
    [Required, EmailAddress, StringLength(CommentLimits.EmailMax)] string Email,
    [HttpUrl, StringLength(CommentLimits.HomePageMax)] string? HomePage,
    [Required, StringLength(CommentLimits.TextMax), CommentText] string Text,
    [Range(1, long.MaxValue)] long? ParentId,
    [StringLength(64)] string? CaptchaId,
    [StringLength(16)] string? CaptchaAnswer,
    // Пока только принимается формой, обработка вложения будет отдельным шагом (сервис получит свой тип вместо IFormFile).
    [AttachmentFile] IFormFile? File = null);
