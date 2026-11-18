namespace Foods.Application.Security.Users.Commands.Update
{
    using FluentValidation;

    public sealed class UpdateCommandValidator : AbstractValidator<UpdateCommand>
    {
        public UpdateCommandValidator()
        {
            RuleFor(e => e.Id).NotEmpty().WithMessage("المستخدم مطلوب");
            RuleFor(e => e.Name).NotEmpty().WithMessage("الاسم مطلوب");
            RuleFor(e => e.UserName).NotEmpty().WithMessage("اسم الدخول مطلوب");
            RuleFor(e => e.Role).NotEmpty().WithMessage("الدور مطلوب");
            RuleFor(e => e.CountryId).NotEmpty().When(e => e.Role == "Customer").WithMessage("البلد مطلوبة");
            RuleFor(e => e.Password).MinimumLength(4).When(e => !string.IsNullOrWhiteSpace(e.Password)).WithMessage("كلمة المرور قصيرة");
        }
    }
}
