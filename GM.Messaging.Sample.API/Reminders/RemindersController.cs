using Asp.Versioning;
using FluentValidation;
using GM.API.Controllers;
using GM.Messaging.Sample.Application.Reminders.Commands;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Reminders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RemindersController : BaseController
{
    [HttpPost(Name = nameof(ScheduleReminder))]
    [ProducesResponseType(typeof(ScheduleReminderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleReminder(
        [FromBody] ScheduleReminderRequestModel request,
        [FromServices] IValidator<ScheduleReminderRequestModel> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var command = request.Adapt<ScheduleReminderCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
