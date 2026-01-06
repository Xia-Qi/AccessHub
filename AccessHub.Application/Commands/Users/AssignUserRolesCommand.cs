using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Users
{
    public class AssignUserRolesCommand : IRequest
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; }
    }

    public class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignUserRolesCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            // 验证用户是否存在
            var user = await _userRepository.GetByIdAsync(new UserId(request.UserId));
            if (user == null)
            {
                throw new InvalidOperationException($"用户 {request.UserId} 不存在");
            }

            // 将角色ID转换为RoleId对象
            var roleIds = request.RoleIds.Select(roleId => new RoleId(roleId)).ToList();

            // 分配角色
            await _userRepository.AssignRolesToUserAsync(new UserId(request.UserId), roleIds);

            await _unitOfWork.CommitTransactionAsync();
        }
    }
}