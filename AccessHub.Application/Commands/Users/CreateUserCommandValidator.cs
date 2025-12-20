using FluentValidation;

namespace AccessHub.Application.Commands.Users
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(v => v.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50);

            RuleFor(v => v.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(v => v.Password)
                .NotEmpty()
                .MinimumLength(6)
                .MaximumLength(100);
        }
    }
}