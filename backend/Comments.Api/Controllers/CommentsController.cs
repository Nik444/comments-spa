using Comments.Api.Application.Requests;
using Comments.Api.Application.Responses;
using Comments.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(CommentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<RootCommentResponse>>> GetPage([FromQuery] GetCommentsRequest request, CancellationToken cancellationToken) =>
        await service.GetPageAsync(request, cancellationToken);

    // /api/comments/by-ids?ids=1&ids=2
    [HttpGet("by-ids")]
    public async Task<ActionResult<IReadOnlyList<CommentResponse>>> GetByIds([FromQuery] long[] ids, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdsAsync(ids, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommentResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var comment = await service.GetByIdAsync(id, cancellationToken);
        return comment is null ? NotFound() : comment;
    }

    // Форма (multipart/form-data), чтобы можно было приложить файл.
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommentResponse>> Create([FromForm] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
