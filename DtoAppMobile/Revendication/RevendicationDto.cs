namespace InteroperabiliteProject.DtoAppMobile.Revendication
{
    public class RevendicationDto
    {
        public string? id { get; set; }
        public string? alias { get; set; }
        public string? statut { get; set; }

        public DateTime? dateVerrouillage { get; set; }
        public DateTime? dateDemande { get; set; }
        public DateTime? dateCloture { get; set; }
        public DateTime? dateAction { get; set; }
        public shidDto? shid { get; set; }

    }

    public class shidDto
    {
        public string? cle { get; set; }
        public string? type { get; set; }
        public string? shid { get; set; }
    }

}


