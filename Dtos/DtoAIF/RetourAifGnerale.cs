namespace InteroperabiliteProject.Dtos
{
    public class RetourAifGnerale
    {
        public int Code { get; set; }
        public Message Message { get; set; }
        public string Description { get; set; }
    }

   
public class Message
    {
        public ClientDto_AIF client { get; set; }
        public CompteDto_AIF compte { get; set; }
    }


   

}
