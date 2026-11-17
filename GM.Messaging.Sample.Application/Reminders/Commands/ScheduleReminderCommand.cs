using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Customers;
using GM.Messaging.Sample.Domain.SeedWork;

namespace GM.Messaging.Sample.Application.Reminders.Commands;

public sealed record ScheduleReminderCommand(Guid CustomerId, string Text, int SendInSeconds) : IRequest<ScheduleReminderResult>;

public sealed record ScheduleReminderResult(Guid EventId, DateTime DueAtUtc);

public class ScheduleReminderCommandValidator : AbstractValidator<ScheduleReminderCommand>
{
    public ScheduleReminderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SendInSeconds).InclusiveBetween(1, 7 * 24 * 3600);
    }
}

public class ScheduleReminderCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ScheduleReminderCommand, ScheduleReminderResult>
{
    public async Task<ScheduleReminderResult> Handle(ScheduleReminderCommand request, CancellationToken cancellationToken)
    {
        var dueAt = DateTimeOffset.UtcNow.AddSeconds(request.SendInSeconds);
        var evt = new ReminderDueIntegrationEvent(request.CustomerId, request.Text, dueAt.UtcDateTime);

        // Saved now, in this transaction; the relay publishes it once it's due.
        var message = OutboxMessage.Create(evt);
        message.PublishAt(dueAt);
        await unitOfWork.OutboxMessageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ScheduleReminderResult(evt.EventId, dueAt.UtcDateTime);
    }
}
