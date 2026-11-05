using Comments.Api.Application.Abstractions;
using Comments.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/attachments")]
public class AttachmentsController(IFileStorage storage) : ControllerBase
{
    // Ключ уникален и не меняется, поэтому файл кэшируется браузером навсегда.
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        if (!AttachmentKeys.IsValid(key))
            return NotFound();

        var stream = await storage.OpenReadAsync(key, cancellationToken);

        if (stream is null)
            return NotFound();

        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";

        return File(stream, AttachmentKeys.ContentType(key));
    }
}
