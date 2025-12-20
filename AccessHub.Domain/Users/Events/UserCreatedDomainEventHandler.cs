using AccessHub.Domain.Users.Events;
using MediatR;

namespace AccessHub.Domain.Users.Events
{
    public class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
    {
        //private readonly IEmailService _emailService;

        public UserCreatedDomainEventHandler(
            //,            IEmailService emailService
            )
        {
            //_emailService = emailService;
        }

        public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
        {

            //await _emailService.SendWelcomeEmailAsync(notification.UserId);
        }
    }
}