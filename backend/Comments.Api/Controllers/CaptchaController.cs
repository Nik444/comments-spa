using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/captcha")]
public class CaptchaController(ICaptchaService captcha) : ControllerBase
{
    // Если капча выключена в настройках, отвечаем 404: фронтенд в этом случае не показывает поле для кода.
    [HttpGet]
    [ProducesResponseType<CaptchaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaptchaResponse>> Get(CancellationToken cancellationToken)
    {
        if (!captcha.IsEnabled)
            return NotFound();

        return await captcha.CreateAsync(cancellationToken);
    }
}
