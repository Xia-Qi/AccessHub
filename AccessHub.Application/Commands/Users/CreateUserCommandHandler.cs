using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using AccessHub.Domain.Users.Services;
using Domain.Base;
using MediatR;

namespace AccessHub.Application.Commands.Users
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserId>
    {
        private readonly IUserRepository _userRepository;
        private readonly UserDomainService _userDomainService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            UserDomainService userDomainService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _userDomainService = userDomainService;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserId> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            var user = await _userDomainService.CreateUserAsync(
                request.Username,
                request.Email,
                request.Password,
                request.PhoneNumber);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return user.Id;
        }
    }
}