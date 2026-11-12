using AeShipManagement.Application.Common;
using FluentValidation;

namespace AeShipManagement.Application.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must be at most 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(254).WithMessage("Email must be at most 254 characters.")
            .EmailAddress().WithMessage("Email is not a valid e-mail address.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(30).WithMessage("Role name must be at most 30 characters.");
    }
}

public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber).ValidPageNumber();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
