using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Roles
{
    public class CreateRoleCommand : IRequest<RoleId>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public ICollection<string>? PermissionCodes { get; set; }
    }

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleId>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateRoleCommandHandler(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<RoleId> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            if (await _roleRepository.ExistsAsync(request.Name, request.Code))
                throw new DomainException("角色名称或编码已存在");

            var role = new Role(request.Name, request.Code);

            // 如果提供了权限代码列表，关联权限
            if (request.PermissionCodes != null && request.PermissionCodes.Any())
            {
                // 这里暂时不实现权限关联，后续完善
            }

            await _roleRepository.AddAsync(role);
            await _unitOfWork.CommitTransactionAsync();

            return role.Id;
        }
    }
}