namespace ask.Dtos.Reponses
{
    public class EmployeDto
    {
        public int? id { get; set; }
        public string nom { get; set; }
        public string prenom { get; set; }
        public string adresse { get; set; }
        public string sexe { get; set; }
        public string nationalite { get; set; }
        public string telephone { get; set; }
        public string dateNaissance { get; set; }
        public string villeNaissance { get; set; }
        public int? directionId { get; set; }
        public string directionNom { get; set; }
    }
}
