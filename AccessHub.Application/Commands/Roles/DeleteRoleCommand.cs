using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Roles
{
    public class DeleteRoleCommand : IRequest
    {
        public Guid RoleId { get; set; }
    }

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteRoleCommandHandler(
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            var role = await _roleRepository.GetByIdAsync(new RoleId(request.RoleId));
            if (role == null)
                throw new DomainException($"角色 {request.RoleId} 不存在");

            await _roleRepository.DeleteAsync(role);
            await _unitOfWork.CommitTransactionAsync();
        }
    }
}