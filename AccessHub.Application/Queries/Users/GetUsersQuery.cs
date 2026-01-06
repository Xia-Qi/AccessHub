using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Users
{
    public class GetUsersQuery : IRequest<GetUsersQueryResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetUsersQueryResult
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public ICollection<UserDto> Users { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<string> Roles { get; set; }
    }
}