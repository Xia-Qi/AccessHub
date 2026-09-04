using AccessHub.Domain;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Users
{
    public class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, UserDetailsDto>
    {
        private readonly IUserRepository _userRepository;

        public GetUserDetailsQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDetailsDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
        {

            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null)
                throw new DomainException($"User {request.Username} not found");

            return new UserDetailsDto
            {
                Id = user.Id.Value,
                Username = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = user.UserRoles.Select(r => r.Role.Name).ToList(),
                RoleIds = user.UserRoles.Select(r => r.RoleId.Value).ToList()
            };
        }
    }
}