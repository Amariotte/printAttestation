using FluentValidation;

namespace InteroperabiliteProject.Dtos
{
    public class QrCodeDto
    {
        public string? alias { get; set; }
        public string? canal { get; set; }
        public string? pays { get; set; } 
        public string? montant { get; set; }
        public string? txId { get; set; }
       
    }

    public class QrCodeDtoValidator : AbstractValidator<QrCodeDto>
    {
        private static readonly List<string> PossibleCanal = new() { "731", "000", "400" };

        public QrCodeDtoValidator()
        {
            RuleFor(x => x.alias)
                .NotEmpty().WithMessage("L'alias est requis pour la génération");

            RuleFor(x => x.canal)
                .NotEmpty().WithMessage("Le canal est requis pour la génération")
                .Must(c => PossibleCanal.Contains(c!)).WithMessage("Les valeurs possibles du canal sont 731, 000 et 400");

            When(x => x.canal == "000", () =>
            {
                RuleFor(x => x.txId)
                    .NotEmpty().WithMessage("Le txid est requis pour la génération d'un QR code statique");
            });

            When(x => x.canal == "400", () =>
            {
                RuleFor(x => x.txId)
                    .NotEmpty().WithMessage("Le txid est requis pour la génération d'un QR code dynamique");

                RuleFor(x => x.montant)
                    .NotEmpty().WithMessage("Le montant est requis pour la génération d'un QR code dynamique")
                    .Must(m => double.TryParse(m, out var value) && value > 0)
                    .WithMessage("Le montant doit être supérieur à 0 pour la génération d'un QR code dynamique");
            });
        }
    }

}