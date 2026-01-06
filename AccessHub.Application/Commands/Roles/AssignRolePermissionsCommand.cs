using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Roles
{
    public class AssignRolePermissionsCommand : IRequest
    {
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; }
    }

    public class AssignRolePermissionsCommandHandler : IRequestHandler<AssignRolePermissionsCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignRolePermissionsCommandHandler(
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AssignRolePermissionsCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            // 验证角色是否存在
            var role = await _roleRepository.GetByIdAsync(new RoleId(request.RoleId));
            if (role == null)
            {
                throw new InvalidOperationException($"角色 {request.RoleId} 不存在");
            }

            // 将权限ID转换为PermissionId对象
            var permissionIds = request.PermissionIds.Select(permissionId => new PermissionId(permissionId)).ToList();

            // 分配权限
            await _roleRepository.AssignPermissionsToRoleAsync(new RoleId(request.RoleId), permissionIds);

            await _unitOfWork.CommitTransactionAsync();
        }
    }
}