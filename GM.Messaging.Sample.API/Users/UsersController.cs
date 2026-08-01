using Asp.Versioning;
using FluentValidation;
using GM.API.Controllers;
using GM.Messaging.Sample.Application.Users.Commands;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Users;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : BaseController
{
    [HttpPost(Name = nameof(RegisterUser))]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterUser(
        [FromBody] RegisterUserRequestModel request,
        [FromServices] IValidator<RegisterUserRequestModel> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var command = request.Adapt<RegisterUserCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
