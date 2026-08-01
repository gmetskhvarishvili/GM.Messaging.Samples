using Asp.Versioning;
using FluentValidation;
using GM.API.Controllers;
using GM.Messaging.Sample.Application.Orders.Commands;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : BaseController
{
    [HttpPost(Name = nameof(CreateOrder))]
    [ProducesResponseType(typeof(CreateOrderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequestModel request,
        [FromServices] IValidator<CreateOrderRequestModel> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var command = request.Adapt<CreateOrderCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
