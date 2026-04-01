namespace InteroperabiliteProject.Dtos
{
   

    public class RetourAifListeCompte
    {


        public string Code { get; set; }
        public string Description { get; set; }

        public MessageCompteListe Message { get; set; }
    }


    public class MessageCompteListe
    {
        public string racineClient { get; set; }
        public string titulaireCompte { get; set; }
        public string intituleCompte { get; set; }
        public List<CompteliteDto_AIF> compte { get; set; }
    }


}
