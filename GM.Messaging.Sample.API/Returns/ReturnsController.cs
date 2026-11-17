using Asp.Versioning;
using FluentValidation;
using GM.API.Controllers;
using GM.Messaging.Sample.Application.Returns.Commands;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Returns;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ReturnsController : BaseController
{
    [HttpPost(Name = nameof(RequestReturn))]
    [ProducesResponseType(typeof(RequestReturnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestReturn(
        [FromBody] RequestReturnRequestModel request,
        [FromServices] IValidator<RequestReturnRequestModel> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var command = request.Adapt<RequestReturnCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
