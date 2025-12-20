using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace AccessHub.Application.Commands.Auth
{
    public class AuthCommandValidator : AbstractValidator<AuthCommand>
    {
        public AuthCommandValidator()
        {
            RuleFor(x => x.Username).NotEmpty().NotNull().MinimumLength(2).MaximumLength(20);
            RuleFor(x => x.Password).NotEmpty().NotNull().MinimumLength(6).MaximumLength(20);
        }
    }
}
