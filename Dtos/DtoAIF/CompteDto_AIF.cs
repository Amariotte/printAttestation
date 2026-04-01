namespace InteroperabiliteProject.Dtos
{

    public class CompteDto_AIF
    {
        public string? codeAgence { get; set; }
        public string? nomAgence { get; set; }
        public string? numeroCompte { get; set; }
        public string? typeCompte { get; set; }
        public string? cleRib { get; set; }
        public string? iban { get; set; }
        public string? intituleCompte { get; set; }
        public string? racineCompte { get; set; }
        public string? titulaireCompte { get; set; }
        public string? soldeDisponibleCompte { get; set; }
        public string? soldeActuelCompte { get; set; }
        public string? soldeFuturCompte { get; set; }
        public string? codeDeviseCompte { get; set; }
        public string? deviseCompte { get; set; }
        public string? sensCompte { get; set; }
        public bool taxeCompte { get; set; }
        public string? statutCompte { get; set; }

        public bool instanceFermetureCompte { get; set; }
        public bool FermetureCompte { get; set; }
        public string? dateOuverture { get; set; }
        public string? dateFermeture { get; set; }
        public string? DateDernierMouvementCompte { get; set; }
        public string? dateDernierDebit { get; set; }
        public string? dateDernierCredit { get; set; }
        public string? dateInstanceFermetureCompte { get; set; }
        public string? montantFondReserve { get; set; }
        public string? rubriqueComptable { get; set; }

    }

    public class CompteliteDto_AIF
    {
        public string? intituleCompte { get; set; }
        public string? codeAgence { get; set; }
        public string? nomAgence { get; set; }
        public string? codeDeviseCompte { get; set; }
        public string? deviseCompte { get; set; }
        public string? numeroCompte { get; set; }
        public string? cleRib { get; set; }
        public string? iban { get; set; }
        public string? rubriqueComptable { get; set; }
        public string? typeCompte { get; set; }
        public string? dateOuverture { get; set; }
        public string? soldeDisponibleCompte { get; set; }
        public string? soldeFuturCompte { get; set; }


    }


}
