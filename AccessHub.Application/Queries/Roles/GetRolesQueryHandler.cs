using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Roles
{
    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, GetRolesQueryResult>
    {
        private readonly IRoleRepository _roleRepository;

        public GetRolesQueryHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<GetRolesQueryResult> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var (roles, totalCount) = await _roleRepository.GetRolesAsync(
                request.Page,
                request.PageSize,
                request.Search
            );

            var roleDtos = roles.Select(role => new RoleDto
            {
                Id = role.Id.Value,
                Name = role.Name,
                Code = role.Code,
                CreatedAt = role.CreatedAt,
                LastModifiedAt = role.LastModifiedAt,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.Code).ToList()
            }).ToList();

            return new GetRolesQueryResult
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Roles = roleDtos
            };
        }
    }
}