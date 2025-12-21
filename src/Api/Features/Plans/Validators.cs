using FluentValidation;

namespace Api.Features.Plans;

public sealed class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxBusinesses).GreaterThan(0);
        RuleFor(x => x.MaxMembersPerBusiness).GreaterThan(0);
    }
}

public sealed class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxBusinesses).GreaterThan(0);
        RuleFor(x => x.MaxMembersPerBusiness).GreaterThan(0);
    }
}
