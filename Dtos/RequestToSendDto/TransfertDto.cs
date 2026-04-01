using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Reflection.Metadata;

namespace ask.Dtos.RequestToSendDto
{
    public class TransfertDto
    {

        public string? msgId { get; set; }
        public string? endToEndId { get; set; }
        public string? montant { get; set; }
        public string? paysClientPayeur { get; set; }
        public string? typeCompteClientPayeur { get; set; }
        public string? deviseCompteClientPayeur { get; set; }
        public string? ibanClientPayeur { get; set; }
        public string? aliasClientPayeur { get; set; }
        public string? nomClientPayeur { get; set; }
        public string? typeClientPayeur { get; set; }
        public string? codeMembreParticipantPayeur { get; set; }
        public string? paysClientPaye { get; set; }
        public string? typeCompteClientPaye { get; set; }
        public string? deviseCompteClientPaye { get; set; }
        public string? otherClientPaye { get; set; }
        public string? nomClientPaye { get; set; }
        public string? typeClientPaye { get; set; }
        public string? canalCommunication { get; set; }
        public string dateHeureAcceptation { get; set; }
        public string? numeroIdentificationClientPayeur { get; set; }
        public string? systemeIdentificationClientPayeur { get; set; }
        public string? numeroIdentificationClientPaye { get; set; }
        public string? systemeIdentificationClientPaye { get; set; }
        public string? adresseClientPaye { get; set; }
        public string? adresseClientPayeur { get; set; }
        public string? villeClientPaye { get; set; }
        public string? villeClientPayeur { get; set; }
        public string? aliasClientPaye { get; set; }
        public string? latitudeClientPayeur { get; set; }
        public string? longitudeClientPayeur { get; set; }
        public string? dateNaissanceClientPayeur { get; set; }
        public string? villeNaissanceClientPayeur { get; set; }
        public string? paysNaissanceClientPayeur { get; set; }
        public string? dateNaissanceClientPaye { get; set; }
        public string? villeNaissanceClientPaye { get; set; }
        public string? paysNaissanceClientPaye { get; set; }

        //**********************************************************************************************

        public string? identifiantTransaction { get; set; }
        public string? referenceBulk { get; set; }
        public string? typeTransaction { get; set; }


     
        public string? otherClientPayeur { get; set; }
       
        public string? numeroRCCMClientPayeur { get; set; }


        public string? codeMembreParticipantPaye { get; set; }
       
        public string? ibanClientPaye { get; set; }
        
        public string? motif { get; set; }
            
        public string? numeroRCCMClientPaye { get; set; }
        public string? typeDocumentReference { get; set; }
        public string? numeroDocumentReference { get; set; }
        public string? montantAchat { get; set; }
        public string? montantFrais { get; set; }
        public string? montantRetrait { get; set; }
        public string? fraisRetrait { get; set; }
    }
}