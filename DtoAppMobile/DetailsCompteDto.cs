using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class DetailsCompteDto
    {
        public aliasDto? alias { get; set; }
        public compteDto? compte { get; set; }
        public clientDto? client { get; set; }
        public string? dateCreation { get; set; }
        public string? dateModification { get; set; }

    }
 public class aliasDto
    {
        public string? cle { get; set; }
        public string? type { get; set; }
        public string? shid { get; set; }
        public string? codeQr { get; set; }

    }

    public class compteDto
    {
        public string? participant { get; set; }
        public string? type { get; set; }
        public string? numero { get; set; }
        public string? agence { get; set; }
        public string? dateOuverture { get; set; }
    }

    public class clientDto
    {
        public string? categorie { get; set; }
        public string? nom { get; set; }
        public string? nationalite { get; set; }
        public string? paysResidence { get; set; }
        public string? telephone { get; set; }
        public string? photo { get; set; }
        public string? email { get; set; }
        public string? adresse { get; set; }
        public string? codePostale { get; set; }
        public string? raisonSociale { get; set; }
        public string? denominationSociale { get; set; }
        public string? categorieEntreprise { get; set; }
        public string? identificationRCCM { get; set; }
        public string? identificationFiscale { get; set; }
    }

}
