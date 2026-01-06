using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Roles
{
    public class UpdateRoleCommand : IRequest
    {
        public Guid RoleId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateRoleCommandHandler(
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            var role = await _roleRepository.GetByIdAsync(new RoleId(request.RoleId));
            if (role == null)
                throw new DomainException($"角色 {request.RoleId} 不存在");

            // 更新角色信息
            role.UpdateName(request.Name);
            role.UpdateCode(request.Code);

            await _roleRepository.UpdateAsync(role);
            await _unitOfWork.CommitTransactionAsync();
        }
    }
}