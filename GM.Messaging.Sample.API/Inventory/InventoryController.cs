using Asp.Versioning;
using FluentValidation;
using GM.API.Controllers;
using GM.Messaging.Sample.Application.Inventory.Commands;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class InventoryController : BaseController
{
    [HttpPost(Name = nameof(UpdateInventory))]
    [ProducesResponseType(typeof(UpdateInventoryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateInventory(
        [FromBody] UpdateInventoryRequestModel request,
        [FromServices] IValidator<UpdateInventoryRequestModel> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var command = request.Adapt<UpdateInventoryCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
