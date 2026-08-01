using Asp.Versioning;
using FluentValidation;
using GM.API.Controllers;
using GM.Messaging.Sample.Application.Payments.Commands;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController : BaseController
{
    [HttpPost(Name = nameof(ProcessPayment))]
    [ProducesResponseType(typeof(ProcessPaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequestModel request,
        [FromServices] IValidator<ProcessPaymentRequestModel> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var command = request.Adapt<ProcessPaymentCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
