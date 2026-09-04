using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Users
{
    public class GetUserDetailsQuery : IRequest<UserDetailsDto>
    {
        public string Username { get; set; } = string.Empty;

        private GetUserDetailsQuery()
        {
        }
        public GetUserDetailsQuery(string username)
        {
            Username = username;
        }
    }

    public class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public ICollection<string> Roles { get; set; }
        public ICollection<Guid> RoleIds { get; set; }
    }
}