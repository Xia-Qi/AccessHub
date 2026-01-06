using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Permissions
{
    public class UpdatePermissionCommand : IRequest
    {
        public Guid PermissionId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            var permission = await _permissionRepository.GetByIdAsync(new PermissionId(request.PermissionId));
            if (permission == null)
                throw new DomainException($"权限 {request.PermissionId} 不存在");

            // 检查权限名称和编码是否已被其他权限使用
            var existingPermission = await _permissionRepository.GetByNameAsync(request.Name);
            if (existingPermission != null && existingPermission.Id.Value != request.PermissionId)
                throw new DomainException($"权限名称 {request.Name} 已存在");

            existingPermission = await _permissionRepository.GetByCodeAsync(request.Code);
            if (existingPermission != null && existingPermission.Id.Value != request.PermissionId)
                throw new DomainException($"权限编码 {request.Code} 已存在");

            // 更新权限信息
            permission.UpdateCode(request.Code);
            permission.UpdateName(request.Name);
            permission.UpdateDescription(request.Description);

            await _permissionRepository.UpdateAsync(permission);
            await _unitOfWork.CommitTransactionAsync();
        }
    }
}