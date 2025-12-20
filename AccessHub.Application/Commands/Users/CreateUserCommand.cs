using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Commands.Users
{
    public class CreateUserCommand : IRequest<UserId>
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
    }
}