using FluentValidation;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Shared.Validators
{
    public class CariKartValidator : AbstractValidator<CariKart>
    {
        public CariKartValidator()
        {
            RuleFor(x => x.Unvan)
                .NotEmpty().WithMessage("Cari ünvanı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Cari ünvanı en az 3 karakter olmalıdır.");

            RuleFor(x => x.Telefon)
                .NotEmpty().WithMessage("Telefon numarası gereklidir.");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Geçersiz e-posta adresi.");

            RuleFor(x => x.VKN)
                .Length(10, 11).When(x => !string.IsNullOrEmpty(x.VKN))
                .WithMessage("VKN 10 veya 11 haneli olmalıdır.");
        }
    }
}
