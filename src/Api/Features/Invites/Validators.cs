using Api.Domain.Businesses;
using FluentValidation;

namespace Api.Features.Invites;

public sealed class CreateInviteRequestValidator : AbstractValidator<CreateInviteRequest>
{
    public CreateInviteRequestValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.InvitedEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.RoleToGrant).Must(r => r == BusinessMemberRole.Admin || r == BusinessMemberRole.Member);
    }
}

public sealed class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
