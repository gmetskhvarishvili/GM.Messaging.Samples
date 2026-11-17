using FluentValidation;

namespace GM.Messaging.Sample.API.Reminders;

public class ScheduleReminderRequestModel
{
    public required Guid CustomerId { get; set; }
    public string? Text { get; set; }
    public int SendInSeconds { get; set; }
}

public class ScheduleReminderRequestModelValidator : AbstractValidator<ScheduleReminderRequestModel>
{
    public ScheduleReminderRequestModelValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Text).NotNull().NotEmpty().MaximumLength(100);
        RuleFor(x => x.SendInSeconds).InclusiveBetween(1, 7 * 24 * 3600);
    }
}
