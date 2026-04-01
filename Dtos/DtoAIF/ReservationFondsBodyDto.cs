namespace InteroperabiliteProject.Dtos
{

    public class ReservationFondsBodyDto
    {
        public string numeroCompte { get; set; }
        public string montantReserve { get; set; }
        public string? dateEcheance { get; set; }
        public string? designationReserve { get; set; }
        public string identifiantTransaction { get; set; }
     

    }


}
