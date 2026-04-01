using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Model;
using QRCoder;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InteroperabiliteProject.DtoAppMobile;
using InteroperabiliteProject.Migrations;
using ask.Dtos.RequestToSendDto;

namespace InteroperabiliteProject.Tools
{
    public static class BuildAndMap
    {


        public static TransactionDto BuildSouscriptionDto(t_scheduled s, bool isClientPayeur, string? clientPaysNom, string? clientPspNom)
        {


            var clientCompte = isClientPayeur ? s.compteClientPaye : s.compteClientPayeur;
            var clientPays = isClientPayeur ? s.paysClientPaye : s.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? s.codeMembreParticipantPaye : s.codeMembreParticipantPayeur;

            var clientConnecteCompte = isClientPayeur ? s.compteClientPayeur : s.compteClientPaye;
            var clientConnecteAlias = isClientPayeur ? s.aliasClientPayeur : s.aliasClientPaye;



            return new TransactionDto
            {
                endToEndId = s.endToEndId,
                alias = clientConnecteAlias,
                statut = s.statut.ToString(),
                compte = clientConnecteCompte,
                montant = s.montant,
                txId = s.txId,
                sens = isClientPayeur ? "debit" : "credit",
                canal = s.canal,
                motif = s.motif,
                dateDebut = s.dateDebut,
                periodicite = s.periodicite,
                frequence = s.frequence,
                dateFin = s.dateFin,
                clientPSP = clientCodeParticipant,
                clientAlias = isClientPayeur ? s.aliasClientPaye : s.aliasClientPayeur,
                clientNom = isClientPayeur ? s.nomClientPaye : s.nomClientPayeur,
                clientPays = clientPays,
                dateOperation = s.r_createdon,
                clientPhoto = isClientPayeur ? s.photoClientPaye : s.photoClientPayeur,
                clientCompte = clientCompte,
                clientPaysNom = clientPaysNom,
                clientPSPNom = clientPspNom,
                retraitFrais = s.fraisRetrait,
                montantFrais = s.montantFrais,
                retraitAchat = s.montantAchat,
                facture = s.numeroDocumentReference,
                subscriptionId = s.Id.ToString(),

                //clientPSPNom = resultats.nomOfficiel,
            };


        }




        public static TransactionDto BuildTransactionDto(t_transfert t, bool isClientPayeur, string? clientPaysNom, string? clientPspNom)
        {
            // Côté “autre” client (celui en face du client connecté)
            var clientCompte = isClientPayeur ? t.compteClientPaye : t.compteClientPayeur;
            var clientPays = isClientPayeur ? t.paysClientPaye : t.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? t.codeMembreParticipantPaye : t.codeMembreParticipantPayeur;

            // Côté “connecté”
            var clientConnecteCompte = isClientPayeur ? t.compteClientPayeur : t.compteClientPaye;
            var clientConnecteAlias = isClientPayeur ? t.aliasClientPayeur : t.aliasClientPaye;

            return new TransactionDto
            {
                endToEndId = t.endToEndId,
                alias = clientConnecteAlias,
                statut = t.statut_general.ToString(),
                compte = clientConnecteCompte,
                montant = t.montant,
                txId = t.identifiantTransaction,
                sens = isClientPayeur ? "debit" : "credit",
                canal = t.canalCommunication,
                motif = t.motif,
                clientPSP = clientCodeParticipant,
                clientAlias = isClientPayeur ? t.aliasClientPaye : t.aliasClientPayeur,
                clientNom = isClientPayeur ? t.nomClientPaye : t.nomClientPayeur,
                clientPays = clientPays,
                dateOperation = t.dateHeureIrrevocabilite ?? t.dateHeureAcceptation ?? t.r_createdon,
                clientPhoto = isClientPayeur ? t.photoClientPaye : t.photoClientPayeur,
                clientCompte = clientCompte,
                clientPaysNom = clientPaysNom,
                clientPSPNom = clientPspNom,
                montantFrais = t.montantFrais,
                retraitFrais = t.fraisRetrait,
                retraitAchat = t.montantAchat,
                facture = t.numeroDocumentReference,
                annulationRaison = t.annulationRaison,
                annulationDate = t.annulationDate,
                annulationStatut = t.annulationStatut.ToString(),
                annulationStatutRaison = t.annulationStatutRaison,
                subscriptionId = t.r_scheduled_id_fk?.ToString(),
                retourDate = t.retourDate,
                retourStatut = t.retourStatut.ToString(),
                retourStatutRaison = t.retourStatutRaison,
                dateExpiration = t.dateLimiteAction
            };
        }



        public static TransfertDto BuildTransfertDto(t_transfert t, string? defaultDevise)
        {
            // Montant (avec remises)
            string montant = t.montant.ToString();
            if (t.montantRemisePaiementImmediat > 0)
                montant = (t.montant - t.montantRemisePaiementImmediat).ToString();
            if (t.tauxRemisePaiementImmediat > 0)
                montant = (t.montant - t.montant * (t.tauxRemisePaiementImmediat / 100)).ToString();

            // Devise (fallback propre)
            string devisePayeur = t.deviseCompteClientPayeur ?? defaultDevise ?? "XOF";
            string devisePaye = string.IsNullOrWhiteSpace(t.deviseCompteClientPaye) ? (defaultDevise ?? "XOF") : t.deviseCompteClientPaye;


            var rq = new TransfertDto
            {
                msgId = t.msgId,
                endToEndId = t.endToEndId,
                montant = montant,
                motif = t.motif,
                identifiantTransaction = t.identifiantTransaction,
                referenceBulk = t.referenceBulk,
                typeTransaction = t.typeTransaction,
                canalCommunication = t.canalCommunication,
                dateHeureAcceptation = Tools.ConvertirDateTimeEnFormatJson(DateTime.Now),
                typeDocumentReference = t.typeDocumentReference,
                numeroDocumentReference = t.numeroDocumentReference,

                // Payeur
                latitudeClientPayeur = t.latitudeClientPayeur,
                longitudeClientPayeur = t.longitudeClientPayeur,
                codeMembreParticipantPayeur = t.codeMembreParticipantPayeur,
                aliasClientPayeur = t.aliasClientPayeur,
                nomClientPayeur = t.nomClientPayeur,
                dateNaissanceClientPayeur = t.dateNaissanceClientPayeur,
                typeCompteClientPayeur = t.typeCompteClientPayeur,
                villeClientPayeur = t.villeClientPayeur,
                villeNaissanceClientPayeur = t.villeNaissanceClientPayeur,
                adresseClientPayeur = t.adresseClientPayeur,
                ibanClientPayeur = t.ibanClientPayeur,
                otherClientPayeur = t.otherClientPayeur,
                paysClientPayeur = t.paysClientPayeur,
                paysNaissanceClientPayeur = t.paysNaissanceClientPayeur,
                deviseCompteClientPayeur = devisePayeur,
                typeClientPayeur = t.typeClientPayeur,
                numeroRCCMClientPayeur = t.typeClientPayeur == "C" ? t.numeroRCCMClientPayeur : null,
                systemeIdentificationClientPayeur = t.typeCompteClientPayeur != "TRAL" ? t.systemeIdentificationClientPayeur : null,
                numeroIdentificationClientPayeur = t.typeCompteClientPayeur != "TRAL" ? t.numeroIdentificationClientPayeur : null,


                // Payé
                nomClientPaye = t.nomClientPaye,
                dateNaissanceClientPaye = t.dateNaissanceClientPaye,
                typeCompteClientPaye = t.typeCompteClientPaye,
                villeClientPaye = t.villeClientPaye,
                villeNaissanceClientPaye = t.villeNaissanceClientPaye,
                adresseClientPaye = t.adresseClientPaye,
                ibanClientPaye = t.ibanClientPaye,
                otherClientPaye = t.otherClientPaye,
                paysClientPaye = t.paysClientPaye,
                paysNaissanceClientPaye = t.paysNaissanceClientPaye,
                codeMembreParticipantPaye = t.codeMembreParticipantPaye,
                typeClientPaye = t.typeClientPaye,
                deviseCompteClientPaye = devisePaye,
                numeroRCCMClientPaye = t.typeClientPaye == "C" ? t.numeroRCCMClientPaye : null,
                systemeIdentificationClientPaye = t.typeCompteClientPaye != "TRAL" ? t.systemeIdentificationClientPaye : null,
                numeroIdentificationClientPaye = t.typeCompteClientPaye != "TRAL" ? t.numeroIdentificationClientPaye : null,
                aliasClientPaye = string.IsNullOrEmpty(t.aliasClientPaye) ? null : t.aliasClientPaye,


                // Frais / achats / retraits
                montantAchat = t.montantAchat > 0 ? t.montantAchat.ToString() : null,
                montantRetrait = t.montantRetrait > 0 ? t.montantRetrait.ToString() : null,
                fraisRetrait = t.fraisRetrait > 0 ? t.fraisRetrait.ToString() : null,
            };

            return rq;
        }

    }

}
