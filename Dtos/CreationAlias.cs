namespace InteroperabiliteProject.Dtos
{
    public class CreationAlias
    {
        public string? idCreationAlias { get; set; }
        public string? nomClient { get; set; }
        public string? categorieClient { get; set; } // "P" "B" "G" "C"
        public string? paysResidenceClient { get; set; }
        public string? telephoneClient { get; set; }
        public string? adresseClient { get; set; }
        public string? participant { get; set; }
        public string? other { get; set; }
        public string? typeCompte { get; set; } // "CACC" "SVGS" "LLSV" "TRAN" "TRAL"
        public string? dateOuvertureCompte { get; set; }
        public string? typeAlias { get; set; } // "SHID" "MBNO" "MCOD"
        public string? valeurAlias { get; set; }
        public string? nationaliteClient { get; set; }
        public string? genreClient { get; set; }
        public string? identificationRccm { get; set; }
        public string? identificationNationaleClient { get; set; }
        public string? dateNaissanceClient { get; set; }
        public string? paysNaissanceClient { get; set; }
        public string? codePostaleClient { get; set; }
        public string? villeNaissanceClient { get; set; }
        public string? iban { get; set; }
        public string? numeroPasseport { get; set; }
        public string? villeClient { get; set; }
        public string? raisonSociale { get; set; }
        public string? emailClient { get; set; }
        public string? denominationSociale { get; set; }
        public string? identificationFiscale { get; set; }
        public string? nomMere { get; set; }
        public string? categorieEntreprise { get; set; }
        public string? codeActivite { get; set; }
        public string? photoClient { get; set; }
        public bool? preConfirmation { get; set; }
    }
}






