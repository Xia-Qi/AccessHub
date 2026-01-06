using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Permissions
{
    public class DeletePermissionCommand : IRequest
    {
        public Guid PermissionId { get; set; }
    }

    public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            var permission = await _permissionRepository.GetByIdAsync(new PermissionId(request.PermissionId));
            if (permission == null)
                throw new DomainException($"权限 {request.PermissionId} 不存在");

            await _permissionRepository.DeleteAsync(permission);
            await _unitOfWork.CommitTransactionAsync();
        }
    }
}