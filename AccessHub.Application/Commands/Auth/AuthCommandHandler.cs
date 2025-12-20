using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using AccessHub.Domain.Users.Services;
using MediatR;

namespace AccessHub.Application.Commands.Auth
{
    public class AuthCommandHandler:IRequestHandler<AuthCommand, User?>
    {
        private readonly UserDomainService _userDomainService;

        public AuthCommandHandler(UserDomainService userDomainService)
        {
            _userDomainService = userDomainService;
        }

        public async Task<User?> Handle(AuthCommand request, CancellationToken cancellationToken)
        {
            return await _userDomainService.ValidateUserAsync(request.Username, request.Password);
        }
    }
}
