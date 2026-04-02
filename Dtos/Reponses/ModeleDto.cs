namespace ask.Dtos.Reponses
{
    public class ModeleDto
    {
        public int? id { get; set; }
        public string description { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public PLATEFORME_MESSAGERIE? plateforme { get; set; }
        public TYPE_MODELE? type { get; set; }
    }
}
