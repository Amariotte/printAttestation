using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class CategorieDto
    {
        public int? Id { get; set; }
        public string nom { get; set; }
        public string icon { get; set; }
        public niveau_categorie? niveau { get; set; }

    }
}


