namespace Foods.Application.Security.Users.Commands.Create
{
    using FluentValidation;

    public sealed class CreateCommandValidator : AbstractValidator<CreateCommand>
    {
        public CreateCommandValidator()
        {
            RuleFor(e => e.Name).NotEmpty().WithMessage("الاسم مطلوب");
            RuleFor(e => e.UserName).NotEmpty().WithMessage("اسم الدخول مطلوب");
            RuleFor(e => e.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة").MinimumLength(4).WithMessage("كلمة المرور قصيرة");
            RuleFor(e => e.Role).NotEmpty().WithMessage("الدور مطلوب");
            RuleFor(e => e.CountryId).NotEmpty().When(e => e.Role == "Customer").WithMessage("البلد مطلوبة");
        }
    }
}
