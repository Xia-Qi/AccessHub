using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using MediatR;

namespace AccessHub.Application.Commands.Auth
{
    public class AuthCommand:IRequest<User?>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
