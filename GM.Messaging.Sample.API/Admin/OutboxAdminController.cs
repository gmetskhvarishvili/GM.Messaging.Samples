using Asp.Versioning;
using GM.API.Controllers;
using GM.Messaging.Persistence.Administration;
using Microsoft.AspNetCore.Mvc;

namespace GM.Messaging.Sample.API.Admin;

/// <summary>
/// The outbox's backlog and dead letters, through GM.Messaging's <see cref="IOutboxAdministration"/>.
/// A dead-lettered message is one the relay gave up on after its last retry (for example an event type
/// with no route); fix the cause, then requeue it. In a real service, protect these endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/outbox")]
public class OutboxAdminController(IOutboxAdministration outbox) : BaseController
{
    /// <summary>Pending messages, the oldest one's age, and dead-lettered messages.</summary>
    [HttpGet(Name = nameof(GetOutboxBacklog))]
    [ProducesResponseType(typeof(MessageBacklog), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxBacklog(CancellationToken cancellationToken) =>
        Ok(await outbox.GetBacklogAsync(cancellationToken));

    [HttpGet("dead-letters", Name = nameof(GetDeadLetters))]
    [ProducesResponseType(typeof(IReadOnlyList<DeadLetteredOutboxMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeadLetters([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        Ok(await outbox.GetDeadLetteredAsync(skip, take, cancellationToken));

    [HttpPost("dead-letters/{id:guid}/requeue", Name = nameof(RequeueDeadLetter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequeueDeadLetter(Guid id, CancellationToken cancellationToken) =>
        await outbox.RequeueAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("dead-letters/requeue", Name = nameof(RequeueAllDeadLetters))]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequeueAllDeadLetters(CancellationToken cancellationToken) =>
        Ok(await outbox.RequeueAllDeadLetteredAsync(cancellationToken));

    [HttpDelete("dead-letters", Name = nameof(PurgeDeadLetters))]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> PurgeDeadLetters([FromQuery] DateTime? deadLetteredBeforeUtc, CancellationToken cancellationToken) =>
        Ok(await outbox.PurgeDeadLetteredAsync(deadLetteredBeforeUtc, cancellationToken));
}
