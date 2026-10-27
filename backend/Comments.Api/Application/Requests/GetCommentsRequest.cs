using System.ComponentModel.DataAnnotations;
using Comments.Api.Application.Models;

namespace Comments.Api.Application.Requests;

public sealed record GetCommentsRequest(
    [Range(1, 1_000_000)] int Page = 1,
    [EnumDataType(typeof(CommentSortField))] CommentSortField SortBy = CommentSortField.Date,
    [EnumDataType(typeof(SortDirection))] SortDirection SortDir = SortDirection.Desc);
