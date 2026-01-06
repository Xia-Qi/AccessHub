using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Permissions
{
    public class CreatePermissionCommand : IRequest<PermissionId>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, PermissionId>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PermissionId> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            if (await _permissionRepository.ExistsAsync(request.Name, request.Code))
                throw new DomainException("权限名称或编码已存在");

            var permission = new Permission(request.Code, request.Name, request.Description);

            await _permissionRepository.AddAsync(permission);
            await _unitOfWork.CommitTransactionAsync();

            return permission.Id;
        }
    }
}