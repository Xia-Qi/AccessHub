using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Queries.Users
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, GetUsersQueryResult>
    {
        private readonly IUserRepository _userRepository;

        public GetUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<GetUsersQueryResult> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var (users, totalCount) = await _userRepository.GetUsersAsync(
                request.Page,
                request.PageSize,
                request.Search,
                request.IsActive
            );

            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id.Value,
                Username = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber.Value,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.LastModifiedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            }).ToList();

            return new GetUsersQueryResult
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Users = userDtos
            };
        }
    }
}