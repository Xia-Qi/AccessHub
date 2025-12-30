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
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            UserDomainService userDomainService,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _userDomainService = userDomainService;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserId> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = new User(
                request.Username,
                request.Email,
                _passwordHasher.HashPassword(request.Password),
                request.PhoneNumber);
                await _userRepository.AddAsync(user);
                await _unitOfWork.CommitTransactionAsync();
                return user.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}