using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Roles
{
    public class GetRolesQuery : IRequest<GetRolesQueryResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
    }

    public class GetRolesQueryResult
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public ICollection<RoleDto> Roles { get; set; }
    }

    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public ICollection<string> Permissions { get; set; }
    }
}