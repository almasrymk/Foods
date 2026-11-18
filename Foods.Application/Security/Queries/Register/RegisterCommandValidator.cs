namespace Foods.Application.Security.Queries.Create
{
    using FluentValidation;

    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(e => e.Name).NotEmpty();
            RuleFor(e => e.UserName).NotEmpty();
            RuleFor(e => e.Password).NotEmpty().MinimumLength(4);
            RuleFor(e => e.CountryId).NotEmpty();
            RuleForEach(e => e.Family).ChildRules(member =>
            {
                member.RuleFor(e => e.Age).InclusiveBetween(0, 120);
            });
        }
    }
}
