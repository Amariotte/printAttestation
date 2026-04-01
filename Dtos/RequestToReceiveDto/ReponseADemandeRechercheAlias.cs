using InteroperabiliteProject.Model;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ask.Dtos.RequestToReceiveDto
{
    public class ReponseADemandeRechercheAlias
    {
        public string valeurAlias { get; set; }
        public string endToEndId { get; set; }
        public string nom { get; set; }
        public string categorie { get; set; }
        public string paysResidence { get; set; }
        public string nationalite { get; set; }
        public string identificationNationale { get; set; }
        public string dateNaissance { get; set; }
        public string paysNaissance { get; set; }
        public string villeNaissance { get; set; }
        public string participant { get; set; }
        public string iban { get; set; }
        public string typeCompte { get; set; }
        public string typeAlias { get; set; }
        public string statut { get; set; }
        public string raisonSociale { get; set; }
        public string denominationSociale { get; set; }
        public string identificationFiscale { get; set; }
        public string ville { get; set; }
        public string identificationRccm { get; set; }
        public string numeroPasseport { get; set; }
        public string telephone { get; set; }
        public string email { get; set; }
        public string adresse { get; set; }
        public string codePostale { get; set; }
        public string photo { get; set; }
        public string other { get; set; }
        public string nomMere { get; set; }
        public string genre { get; set; }
        public string categorieEntreprise { get; set; }
        public string codeActivite { get; set; }
        public string raisonRejet { get; set; }
    }
}