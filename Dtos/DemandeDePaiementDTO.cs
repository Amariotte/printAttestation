using Microsoft.Extensions.FileSystemGlobbing.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.RequestToReceiveDto;
using Keycloak.Net.Models.Clients;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Win32;
using Serilog;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System;

namespace InteroperabiliteProject.Dtos
{
    public class DemandeDePaiementDTO
    {
        public string msgId { get; set; }
        public string clientDemandeur { get; set; }
        public string identifiantDemandePaiement { get; set; }
        public string? dateHeureExecution { get; set; }
        public string? dateLimiteAction { get; set; }
        public string nomClientPayeur { get; set; }
        public string typeClientPayeur { get; set; }
        public string villeClientPayeur { get; set; }
        public string adresseClientPayeur { get; set; }
        public string numeroIdentificationClientPayeur { get; set; }
        public string systemeIdentificationClientPayeur { get; set; }
        public string dateNaissanceClientPayeur { get; set; }
        public string villeNaissanceClientPayeur { get; set; }
        public string paysNaissanceClientPayeur { get; set; }
        public string paysClientPayeur { get; set; }
        public string numeroRCCMClientPayeur { get; set; }
        public string ibanClientPayeur { get; set; }
        public string otherClientPayeur { get; set; }
        public string typeCompteClientPayeur { get; set; }
        public string deviseCompteClientPayeur { get; set; }
        public string? dateHeureAcceptation { get; set; }
        public string aliasClientPayeur { get; set; }
        public string codeMembreParticipantPayeur { get; set; }
        public string endToEndId { get; set; }
        public string? referenceBulk { get; set; }
        public string canalCommunication { get; set; }
        public bool? autorisationModificationMontant { get; set; }
        public string montantRemisePaiementImmediat { get; set; }
        public string? tauxRemisePaiementImmediat { get; set; }
        public string? identifiantMandat { get; set; }
        public string? signatureNumeriqueMandat { get; set; }
        public string montant { get; set; }
        public string nomClientPaye { get; set; }
        public string typeClientPaye { get; set; }
        public string villeClientPaye { get; set; }
        public string latitudeClientPaye { get; set; }
        public string longitudeClientPaye { get; set; }
        public string adresseClientPaye { get; set; }
        public string numeroIdentificationClientPaye { get; set; }
        public string systemeIdentificationClientPaye { get; set; }
        public string numeroRCCMClientPaye { get; set; }
        public string dateNaissanceClientPaye { get; set; }
        public string villeNaissanceClientPaye { get; set; }
        public string paysNaissanceClientPaye { get; set; }
        public string paysClientPaye { get; set; }
        public string ibanClientPaye { get; set; }
        public string otherClientPaye { get; set; }
        public string typeCompteClientPaye { get; set; }
        public string deviseCompteClientPaye { get; set; }
        public string aliasClientPaye { get; set; }
        public string codeMembreParticipantPaye { get; set; }
        public string? motif { get; set; }
        public string? typeDocumentReference { get; set; }
        public string? numeroDocumentReference { get; set; }
        public string? montantAchat { get; set; }
        public string? montantRetrait { get; set; }
        public string? fraisRetrait { get; set; }



        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(msgId) ||
                string.IsNullOrWhiteSpace(endToEndId) ||
                string.IsNullOrWhiteSpace(montant) ||
                string.IsNullOrWhiteSpace(paysClientPayeur) ||
                string.IsNullOrWhiteSpace(typeCompteClientPayeur) ||
                string.IsNullOrWhiteSpace(deviseCompteClientPayeur) ||
                string.IsNullOrWhiteSpace(nomClientPayeur) ||
                string.IsNullOrWhiteSpace(typeClientPayeur) ||
                string.IsNullOrWhiteSpace(codeMembreParticipantPayeur) ||
                string.IsNullOrWhiteSpace(paysClientPaye) ||
                string.IsNullOrWhiteSpace(typeCompteClientPaye) ||
                string.IsNullOrWhiteSpace(deviseCompteClientPaye) ||
                string.IsNullOrWhiteSpace(nomClientPaye) ||
                string.IsNullOrWhiteSpace(typeClientPaye) ||
                string.IsNullOrWhiteSpace(canalCommunication) ||
                string.IsNullOrWhiteSpace(dateHeureAcceptation) ||
                string.IsNullOrWhiteSpace(identifiantDemandePaiement)
                )
                 
            {
                throw new ArgumentException("Champs obligatoires manquants dans DemandeDePaiementDTO.");
            }
        }
    
    }
}

