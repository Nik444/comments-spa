using System.ComponentModel.DataAnnotations;
using Comments.Api.Domain;
using Microsoft.AspNetCore.Http;

namespace Comments.Api.Application.Requests;

public sealed record CreateCommentRequest(
    [Required, StringLength(CommentLimits.UserNameMax)] string UserName,
    [Required, EmailAddress, StringLength(CommentLimits.EmailMax)] string Email,
    [Url, StringLength(CommentLimits.HomePageMax)] string? HomePage,
    [Required, StringLength(CommentLimits.TextMax)] string Text,
    [Range(1, long.MaxValue)] long? ParentId,
    // Пока только принимается формой, обработка вложения будет отдельным шагом (сервис получит свой тип вместо IFormFile).
    IFormFile? File = null);
