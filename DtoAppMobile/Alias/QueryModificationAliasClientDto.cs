using System.ComponentModel.DataAnnotations;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryModificationAliasClientDto
    {
        [Required]
        public string? alias { get; set; }
        public string? paysResidenceClient { get; set; }
        public string? telephoneClient { get; set; }
        public string? numeroPasseport { get; set; }
        public string? codePostalClient { get; set; }
        public string? adresseClient { get; set; }
        public string? emailClient { get; set; }
        public string? villeClient { get; set; }
        public string? denominationSociale { get; set; }
        public string? photoClient { get; set; }
        public bool? preConfirmation { get; set; }

    }
}
