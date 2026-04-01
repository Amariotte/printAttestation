namespace InteroperabiliteProject.Dtos
{



    public class RetourAifReservationCancel
    {
        public int Code { get; set; }
        public ReservationCancelDto_AIF Message { get; set; }
        public string Description { get; set; }
    }



}
