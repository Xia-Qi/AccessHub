using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Permissions
{
    public class GetPermissionsQuery : IRequest<GetPermissionsQueryResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
    }

    public class GetPermissionsQueryResult
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<PermissionDto> Permissions { get; set; } = [];
    }

    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }

    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, GetPermissionsQueryResult>
    {
        private readonly IPermissionRepository _permissionRepository;

        public GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<GetPermissionsQueryResult> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        {
            var (permissions, totalCount) = await _permissionRepository.GetPermissionsAsync(
                request.Page,
                request.PageSize,
                request.Search
            );

            var permissionDtos = permissions.Select(permission => new PermissionDto
            {
                Id = permission.Id.Value,
                Code = permission.Code,
                Name = permission.Name,
                Description = permission.Description,
                CreatedAt = permission.CreatedAt,
                LastModifiedAt = permission.LastModifiedAt
            }).ToList();

            return new GetPermissionsQueryResult
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Permissions = permissionDtos
            };
        }
    }
}