using Api.Domain.Businesses;
using FluentValidation;

namespace Api.Features.Businesses;

public sealed class CreateBusinessRequestValidator : AbstractValidator<CreateBusinessRequest>
{
    public CreateBusinessRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).Must(r => r == BusinessMemberRole.Admin || r == BusinessMemberRole.Member);
    }
}

public sealed class ChangeMemberRoleRequestValidator : AbstractValidator<ChangeMemberRoleRequest>
{
    public ChangeMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role).Must(r => r == BusinessMemberRole.Admin || r == BusinessMemberRole.Member);
    }
}
