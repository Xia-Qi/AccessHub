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

        public async Task<User> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
        {

            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null)
                throw new Exception($"User {request.Username} not found");

            return new UserDetailsDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = user.Roles.Select(r => r.Name).ToList()
            };
        }
    }
}