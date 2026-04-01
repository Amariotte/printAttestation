using ask.Dtos.RequestToReceiveDto;
using ask.Dtos.RequestToSendDto;
using AutoMapper;
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Dtos.EnvoieController;
using InteroperabiliteProject.Dtos.RevendicationAlias;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.ServicceAIP;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace InteroperabiliteProject.Controllers
{


    public class ReceptionAIPController
    {

        private readonly IDbContextFactory<InteropContext> _contextFactory;

        private readonly IConfiguration _configuration;
        private readonly AIPDATA _aipdata;
        private readonly ServiceAIF _ServiceAIF;
        private readonly IMapper _mapper;
        private readonly ILogger<ReceptionAIPController> _logger;
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly IreferenceRepo _ireferenceRepo;
        private readonly ICodeErreurRepo _codeErreurRepo;
        private readonly IParticipantsRepo _participantrepo;
        private readonly EnvoieController _envoieController;


        public ReceptionAIPController(IConfiguration configuration, EnvoieController envoieController, IDbContextFactory<InteropContext> contextFactory, IreferenceRepo ireferenceRepo, IOptions<AIPDATA> aipdata, IMapper mapper, ILogger<ReceptionAIPController> logger, ServiceAIF serviceAIF, ICodeErreurRepo codeErreurRepo, IParticipantsRepo participantrepo)
        {
            _configuration = configuration;
            _aipdata = aipdata.Value;
            _mapper = mapper;
            _logger = logger;
            _codeErreurRepo = codeErreurRepo;
            _participantrepo = participantrepo;

            jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _ServiceAIF = serviceAIF;
            _contextFactory = contextFactory;
            _ireferenceRepo = ireferenceRepo;
            _envoieController = envoieController;
        }



        public async Task RecevoirDemandeVerificationIdentite(VerificationIdentiteRequestFromAIP cre)
        {
            const string CODE_INVALID_ACCOUNT = "AC01";

            // Garde précoce
            if (cre is null)
            {
                _logger.LogWarning("Demande de vérification reçue NULL.");
                return;
            }

            var pretty = new JsonSerializerOptions { WriteIndented = true };
            _logger.LogInformation("Demande de vérification reçue : {Json}", JsonSerializer.Serialize(cre, pretty));

            // Prépare la réponse (champs communs)
            var resp = new RequeteReponseaDemandeVerificationIdentite
            {
                endToEndId = cre.endToEndId,
                msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                codeMembreParticipant = cre.codeMembreParticipant,
                msgIdDemande = cre.msgId
            };

            try
            {
                // Validation basique de l'IBAN/compte
                var numCompte = cre.ibanClient;
                if (string.IsNullOrWhiteSpace(numCompte))
                {
                    resp.resultatVerification = "false";
                    resp.codeRaison = CODE_INVALID_ACCOUNT;

                    _logger.LogWarning("Vérification identité: compte manquant pour endToEndId={E2E}, msgIdDemande={MsgIdDemande}.",
                        cre.endToEndId, cre.msgId);

                    _logger.LogInformation("Réponse envoyée (KO) : {Json}", JsonSerializer.Serialize(resp, pretty));
                    await _envoieController.ReponseaDemandeVerificationIdentite(resp);
                    return;
                }

                // 1) Récupération compte côté AIF
                var rCompte = await _ServiceAIF.GetClientCompte(numCompte, "");
                if (!rCompte.operationStatus || string.IsNullOrWhiteSpace(rCompte.data))
                {
                    resp.resultatVerification = "false";
                    resp.codeRaison = CODE_INVALID_ACCOUNT;

                    _logger.LogWarning("GetClientCompte KO : reason={Reason} | numCompte={Compte}", rCompte.erreur, numCompte);
                    _logger.LogInformation("Réponse envoyée (KO) : {Json}", JsonSerializer.Serialize(resp, pretty));
                    await _envoieController.ReponseaDemandeVerificationIdentite(resp);
                    return;
                }

                // 2) Désérialise le message retourné par AIF
                Message? msgAif;
                try
                {
                    msgAif = JsonConvert.DeserializeObject<Message>(rCompte.data);
                    if (msgAif is null)
                        throw new Exception("Désérialisation Message AIF retournée null.");
                }
                catch (Exception ex)
                {
                    resp.resultatVerification = "false";
                    resp.codeRaison = CODE_INVALID_ACCOUNT;

                    _logger.LogError(ex, "Parse Message AIF échoué. Payload={Payload}", rCompte.data);
                    _logger.LogInformation("Réponse envoyée (KO) : {Json}", JsonSerializer.Serialize(resp, pretty));
                    await _envoieController.ReponseaDemandeVerificationIdentite(resp);
                    return;
                }

                // 3) Équivalence client/compte
                var rEquiv = await _ServiceAIF.GetEquivalenceClientCompte(msgAif);
                if (!rEquiv.operationStatus || string.IsNullOrWhiteSpace(rEquiv.data))
                {
                    resp.resultatVerification = "false";
                    resp.codeRaison = CODE_INVALID_ACCOUNT;

                    _logger.LogWarning("GetEquivalenceClientCompte KO : reason={Reason}", rEquiv.erreur);
                    _logger.LogInformation("Réponse envoyée (KO) : {Json}", JsonSerializer.Serialize(resp, pretty));
                    await _envoieController.ReponseaDemandeVerificationIdentite(resp);
                    return;
                }

                // 4) Mappe la donnée d’équivalence
                ReponseAUneDemandeDeVerificationAIF? dto;
                try
                {
                    dto = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationAIF>(rEquiv.data);
                    if (dto is null)
                        throw new Exception("Désérialisation ReponseAUneDemandeDeVerificationAIF retournée null.");
                }
                catch (Exception ex)
                {
                    resp.resultatVerification = "false";
                    resp.codeRaison = CODE_INVALID_ACCOUNT;

                    _logger.LogError(ex, "Parse équivalence AIF échoué. Payload={Payload}", rEquiv.data);
                    _logger.LogInformation("Réponse envoyée (KO) : {Json}", JsonSerializer.Serialize(resp, pretty));
                    await _envoieController.ReponseaDemandeVerificationIdentite(resp);
                    return;
                }

                // 5) Remplit la réponse OK
                resp.resultatVerification = "true";
                resp.typeCompte = dto.typeCompte;
                resp.typeClient = dto.typeClient;
                resp.villeNaissance = dto.villeNaissance;
                resp.villeClient = dto.villeClient;
                resp.nomClient = dto.nomClient;
                resp.paysResidence = dto.paysResidence;
                resp.dateNaissance = dto.dateNaissance;
                resp.adresseComplete = dto.adresseComplete;
                resp.numeroIdentification = dto.numeroIdentification;
                resp.paysNaissance = dto.paysNaissance;
                resp.devise = dto.devise;
                resp.ibanClient = dto.ibanClient;
                resp.systemeIdentification = dto.systemeIdentification;
                resp.codeMembreParticipant = cre.codeMembreParticipant;

                if (string.Equals(resp.typeClient, "C", StringComparison.OrdinalIgnoreCase))
                    resp.numeroRCCMClient = dto.numeroRCCMClient;

                _logger.LogInformation("Réponse envoyée (OK) : {Json}", JsonSerializer.Serialize(resp, pretty));
                await _envoieController.ReponseaDemandeVerificationIdentite(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de la vérification d'identité (endToEndId={E2E}, msgIdDemande={MsgIdDemande}).",
                    cre.endToEndId, cre.msgId);

                // En cas d’exception, on renvoie une réponse KO standardisée
                try
                {
                    resp.resultatVerification = "false";
                    resp.codeRaison = CODE_INVALID_ACCOUNT;


                    _logger.LogInformation("Réponse envoyée (KO par exception) : {Json}", JsonSerializer.Serialize(resp, pretty));
                    await _envoieController.ReponseaDemandeVerificationIdentite(resp);
                }
                catch (Exception inner)
                {
                    _logger.LogError(inner, "Échec de l'envoi de la réponse KO après exception initiale.");
                }
            }
        }


        public async Task RecevoirDemandeDetransfertEtRepondre(TransfertDto cre, string iddemande)
        {
            try
            {


                // La reception de la demande de transfert
                // assure que le compte existe et qu'il n'est ni clôturé ni bloqué
                // Lorsqu'un plafond est fixé pour le compte, il s'assure que l'imputation du montant sur le compte ne conduit pas au dépassement de ce plafond.
                // Lorsque le participant dispose d'un dispositif automatisé de détection de la fraude, il soumet la transaction au système.
                // Lorsque toutes les conditions sont remplies, le participant prépositionne le montant sur le compte pour qu'il soit pris en compte dans le contrôle du respect du plafond éventuel. 
                // Vous devez uniquement prépositionner les fonds Vous ne devez impérativement pas créditer le compte de votre client à ce stade. Le compte doit être crédité qu'à la reception d'un pacs.002 d'avis de crédit.

                // Le code AC03 pour InvalidCreditorAccountNumber: Le numéro de compte du payé est invalide
                // Le code AC07 pour ClosedCreditorAccountNumber: Numéro de compte payé clôturé
                // Le code AC06 pour BlockedAccount: Le compte spécifié est bloqué
                // Le code AG01 pour TransactionForbidden: Transaction interdite sur ce type de compte
                // Le code AM21 pour LimitExceeded: Le montant de la transaction dépasse les limites convenues entre le participant et le client
                // Le code FR01 pour Fraude: Retourné à la suite dune fraude

                // La fonction de reservation de l' AIF fait office de vérification de toutes ces conditions

                //**********************************************Constitution URI******************************************************************

                if (cre.typeTransaction == "DISP") // Alors c'est un transfert ping
                {

                    _logger.LogError("Reception de transfert de disponibilté depuis PI");
                    ReponseAUneDemandeDeTransfert rep = new ReponseAUneDemandeDeTransfert
                    {
                        endToEndId = cre.endToEndId,
                        msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                        msgIdDemande = cre.msgId,
                        statutTransaction = "ACSP"
                    };
                    await _envoieController.ReponseAUneDemandeDeTransfert(rep);

                    return;
                }


                t_transfert new_transfert = _mapper.Map<t_transfert>(cre);
                new_transfert.codeMembreParticipantPaye = _aipdata.codemembre;
                new_transfert.statut_general = STATUT_TRANSFERT.initie;
                new_transfert.etape = ETAPE_TRANSFERT.INITIEE;
                new_transfert.sensFlux = sensFlux.ENTRANT;
                new_transfert.IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30);


                double montant = double.Parse(cre.montant);
                string idSib = new_transfert.IdOperationSib;
                string _compte_produit = _aipdata.compteProduit;


                ReservationFondsBodyDto _body_reservation = new ReservationFondsBodyDto
                {
                    numeroCompte = _compte_produit,
                    montantReserve = montant.ToString(),
                  //  identifiantTransaction = idSib,
                };


                GeneraleRetour res_reservation = await _ServiceAIF.FaireUneReservationDeFonds(_body_reservation, iddemande);
                string responseTransfert = "RJCT";
                string codeRejet = "AM21";
                string msgError = "";

                if (res_reservation.data != null)
                {
                    ReservationDto_AIF data_reservation = JsonConvert.DeserializeObject<ReservationDto_AIF>(res_reservation.data);

                    // Création de la ligne de transfert
                    new_transfert.numEvenementReserv = data_reservation.numeroEvenement;
                    new_transfert.codeOperationReserv = data_reservation.codeOperation;
                    new_transfert.codeAgenceReserv = data_reservation.codeAgenceTransaction;
                }

                if (!Tools.Tools.RetourIsSucces(res_reservation.status))
                {
                    new_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    new_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    new_transfert.motifRejet = res_reservation.detail;
                    msgError = res_reservation.detail;

                    _logger.LogInformation("Traitement dans Reversation NOK");

                }


                if (new_transfert.statut_general != STATUT_TRANSFERT.rejete)
                    responseTransfert = "ACSP";

                if (responseTransfert == "ACSP" || responseTransfert == "RJCT")
                {
                    ReponseAUneDemandeDeTransfert rep = new ReponseAUneDemandeDeTransfert
                    {
                        endToEndId = cre.endToEndId,
                        msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                        msgIdDemande = cre.msgId,
                        statutTransaction = responseTransfert,
                        codeRaison = responseTransfert == "RJCT" ? codeRejet : null,
                        informationsAdditionnelles = responseTransfert == "RJCT" ? msgError : null,
                        //   codeService = "PP2P" //Dans le cas ou il recoit plus de 30 transfert
                    };

                    await _envoieController.ReponseAUneDemandeDeTransfert(rep);

                    _logger.LogInformation("Traitement dans ACSP ou RJCT");

                }

                //*******************************************Gestion de la reponse***************************************************************


                new_transfert.compteClientPayeur = new_transfert.ibanClientPayeur;
                if (string.IsNullOrEmpty(new_transfert.compteClientPayeur))
                    new_transfert.compteClientPayeur = new_transfert.otherClientPayeur;

                new_transfert.compteClientPaye = new_transfert.ibanClientPaye;
                if (string.IsNullOrEmpty(new_transfert.compteClientPaye))
                    new_transfert.compteClientPaye = new_transfert.otherClientPaye;

                _logger.LogInformation("Traitement avant le save");

                var json = JsonConvert.SerializeObject(new_transfert, Formatting.Indented);

                await using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    context.Set<t_transfert>().Add(new_transfert);
                    await context.SaveChangesAsync();
                }
                
             }

            catch (Exception ex)
            {
                _logger.LogInformation($"[RecevoirDemandeDetransfertEtRepondre] Exception  =================> {ex.Message}");
                throw;
            }
        }

        public async Task RecevoirListeDesParticipants(ReponseDemandeListeParticipant _data)
        {
            try
            {
                using (var Cont = _contextFactory.CreateDbContext())
                {

                    if (_data != null && _data.listeParticipant != null && _data.listeParticipant.Any())
                    {
                        try
                        {
                            _logger.LogInformation("Récupération de la liste des participants.");


                            foreach (var participant in _data.listeParticipant)
                            {


                                var participantExistant = await Cont.t_participant
                                .Where(p => p.codeMembreParticipant == participant.codeMembreParticipant).FirstOrDefaultAsync();

                                if (participantExistant != null)
                                {
                                    participantExistant.statut = participant.statut;
                                    participantExistant.codeBanque = participant.codeBanque;
                                    participantExistant.nomOfficiel = participant.nomOfficiel;
                                    Cont.t_participant.Update(participantExistant);
                                }
                                else
                                {
                                    t_participant p = new t_participant
                                    {
                                        codeMembreParticipant = participant.codeMembreParticipant,
                                        statut = participant.statut,
                                        codeBanque = participant.codeBanque,
                                        nomOfficiel = participant.nomOfficiel
                                    };
                                    Cont.t_participant.Add(p);

                                }
                            }

                            await Cont.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Erreur lors du traitement des participants : {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Erreur lors de la création ou de l'utilisation du contexte : {ex.Message}");
            }
        }


        public async Task TraitementEchecTransfert(string msgid, string codeRaison, string descriptionRejet, string iddemande)
        {

            // Rechercher le transfert dans la Base

            using (var Cont = _contextFactory.CreateDbContext())
            {


                // Verifier si c'est une reponse à un retour de fond ou à un transfert

                var transfert = await Cont.t_transfert
                .Where(p => p.msgId == msgid && p.is_delete != true && p.statut_general == STATUT_TRANSFERT.initie)
                .FirstOrDefaultAsync();

                if (transfert != null)
                {

                    CancelReservationBody _bodyCancelReservation = new CancelReservationBody
                    {
                        codeOperation = transfert.codeOperationReserv,
                        numEvenement = transfert.numEvenementReserv,
                        codeAgenceTransaction = transfert.codeAgenceReserv
                    };

                    await _ServiceAIF.LeveeUneReservationDeFonds(_bodyCancelReservation, iddemande);
                    string _desc = descriptionRejet;
                    if (string.IsNullOrEmpty(codeRaison))
                    {
                        _desc = await _codeErreurRepo.GetLibelleErreurAsync(codeRaison, tag_erreur.CODE_RAISON_REJET.ToString());
                        if (string.IsNullOrEmpty(_desc)) _desc = "Transfert echouée";
                        if (!string.IsNullOrEmpty(descriptionRejet)) _desc += " " + descriptionRejet;
                    }

                    // Mise a jour du statut du transfert
                    transfert.etape = ETAPE_TRANSFERT.REJETE;
                    transfert.statut_general = STATUT_TRANSFERT.rejete;
                    transfert.codeRejet = codeRaison;
                    transfert.motifRejet = _desc;
                    Cont.t_transfert.Update(transfert);
                    await Cont.SaveChangesAsync();

                    if (Tools.Tools.canalEstCanalDemandePaiement(transfert.canalCommunication))
                    {
                        var data = new
                        {
                            montant = transfert.montant,
                            emetteur = transfert.nomClientPayeur,
                        };

                        string json = System.Text.Json.JsonSerializer.Serialize(data);
                        JsonDocument details = JsonDocument.Parse(json);

                        t_notification n = new t_notification
                        {

                            type = type_notif.RTP_REJETE.ToString(),
                            estCliquable = true,
                            compte = transfert.compteClientPaye,
                            idObject = transfert.endToEndId,
                            dateAction = DateTime.Now,

                            details = details
                        };

                        await Cont.t_notification.AddAsync(n);
                    }
                }

            }

        }

        public async Task RecevoirReponseRevendicationAlias(ReponseRevendicationDecisionReponseDTO _data, string iddemande)
        {

            // Mise à jour de la revendication dans la base
            using (var Cont = _contextFactory.CreateDbContext())
            {
                var rev = await Cont.t_revendication
                  .Where(p => p.idRevendicationPi == _data.identifiantRevendication && p.is_delete != true
                  && p.statut == statut_revendication.INITIE && p.sensFlux == sensFlux.SORTANT)
                  .FirstOrDefaultAsync();

                if (rev != null)
                {

                    switch ((_data.statut.Trim().ToUpper()))
                    {
                        case "ACCEPTEE":
                            rev.statut = statut_revendication.ACCEPTEE;
                            break;

                        case "REJETEE":
                            rev.statut = statut_revendication.REJETEE;
                            break;

                        case "ECHEC":
                            rev.statut = statut_revendication.ECHEC;
                            break;

                        default:
                            rev.statut = statut_revendication.INITIE;
                            break;
                    }

                    rev.dateCreation = _data.dateCreation;
                    rev.dateModification = _data.dateModification;
                    rev.dateAction = _data.dateAction;
                    rev.raisonRejet = _data.raisonRejet;
                    rev.informationsAdditionnelles = _data.informationsAdditionnelles;
                    Cont.t_revendication.Update(rev);
                    await Cont.SaveChangesAsync();

                }

            }
        }


        public async Task RecevoirRejetDemandePaiement(ReponseaDemandeDePaiementDTO _data, string iddemande)
        {

            // Rechercher le transaction dans la Base

            using (var Cont = _contextFactory.CreateDbContext())
            {
                var transfert = await Cont.t_transfert
                  .Where(p => p.endToEndId == _data.endToEndId && p.is_delete != true)
                  .FirstOrDefaultAsync();

                if (transfert != null)
                {
                    if (transfert.statut_general == STATUT_TRANSFERT.initie)
                    {
                        // Rejeter la demande
                        transfert.statut_general = STATUT_TRANSFERT.rejete;
                        transfert.etape = ETAPE_TRANSFERT.REJETE;
                        transfert.codeRejet = _data.codeRaison;
                        Cont.t_transfert.Update(transfert);

                        var data = new
                        {
                            montant = transfert.montant,
                            emetteur = transfert.nomClientPayeur,
                        };

                        string json = System.Text.Json.JsonSerializer.Serialize(data);
                        JsonDocument details = JsonDocument.Parse(json);

                        t_notification n = new t_notification
                        {

                            type = type_notif.RTP_REJETE.ToString(),
                            estCliquable = true,
                            compte = transfert.compteClientPaye,
                            idObject = transfert.endToEndId,
                            dateAction = DateTime.Now,

                            details = details
                        };

                        await Cont.t_notification.AddAsync(n);
                        await Cont.SaveChangesAsync();

                    }
                }
            }

        }
  
        
        public async Task RecevoirDemandePaiement(DemandeDePaiementDTO cre, string iddemande)
    {
        if (cre is null)
        {
            _logger.LogWarning("[RecevoirDemandePaiement] Payload NULL.");
            return;
        }

        try
        {
            var pretty = new JsonSerializerOptions { WriteIndented = true };
            _logger.LogInformation("DemandeDePaiementDTO reçue : {Json}", System.Text.Json.JsonSerializer.Serialize(cre, pretty));

            // 1) Map DTO -> entité
            var tr = _mapper.Map<t_transfert>(cre);

            // Compléter/forcer les champs clés
            tr.statut_general = STATUT_TRANSFERT.initie;
            tr.etape = ETAPE_TRANSFERT.INITIEE;
            tr.sensFlux = sensFlux.ENTRANT;
            tr.identifiantTransaction = cre.identifiantDemandePaiement;
            tr.codeMembreParticipantPaye = cre.codeMembreParticipantPaye;
            tr.codeMembreParticipantPayeur = _aipdata.codemembre;
            tr.numeroIdentificationClientPayeur = cre.numeroIdentificationClientPayeur;
            tr.systemeIdentificationClientPayeur = cre.systemeIdentificationClientPayeur;
            tr.photoClientPaye = cre.clientDemandeur;

            if (string.IsNullOrWhiteSpace(tr.IdOperationSib))
                tr.IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30);

            // 2) Dates (RFC3339/ISO 8601)
            if (!string.IsNullOrWhiteSpace(cre.dateHeureExecution) &&
                DateTime.TryParse(cre.dateHeureExecution, null, DateTimeStyles.RoundtripKind, out var dExec))
            {
                tr.dateHeureExecution = dExec;
            }

            if (!string.IsNullOrWhiteSpace(cre.dateLimiteAction) &&
                DateTime.TryParse(cre.dateLimiteAction, null, DateTimeStyles.RoundtripKind, out var dLim))
            {
                tr.dateLimiteAction = dLim;
            }

            // 3) Comptes (priorité IBAN puis OTHER)
            tr.compteClientPaye = string.IsNullOrWhiteSpace(tr.ibanClientPaye) ? tr.otherClientPaye : tr.ibanClientPaye;
            tr.compteClientPayeur = string.IsNullOrWhiteSpace(tr.ibanClientPayeur) ? tr.otherClientPayeur : tr.ibanClientPayeur;

            // 4) Montant (InvariantCulture)
            if (!double.TryParse(cre.montant, NumberStyles.Float, CultureInfo.InvariantCulture, out var montant))
            {
                _logger.LogWarning("[RecevoirDemandePaiement] Montant invalide: {Montant}", cre.montant);
                // On peut choisir de rejeter silencieusement ou de continuer avec 0; ici on sort.
                return;
            }

            // 5) Ouverture contexte + enrichissement alias + idempotence + save
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            // Enrichir identité payeur depuis l'alias si manquante
            var alias = await ctx.t_alias
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.is_delete != true &&
                    (a.valeurAlias == cre.aliasClientPayeur || a.shid == cre.aliasClientPayeur));

            if (alias is not null)
            {
                var (sysId, numId) = Tools.Tools.TraiterNumPiece(
                    alias.categorie,
                    alias.identificationNationaleClient,
                    alias.numeroPasseport,
                    alias.identificationFiscale);

                tr.systemeIdentificationClientPayeur ??= sysId;
                tr.numeroIdentificationClientPayeur ??= numId;

                _logger.LogInformation("Alias trouvé pour le payeur ({Alias}). Identité complétée.", cre.aliasClientPayeur);
            }

            // Idempotence : ne pas créer un doublon si on a déjà ce msgId ou endToEndId en INITIE
            var exists = await ctx.t_transfert.AnyAsync(x =>
                x.is_delete != true &&
                x.sensFlux == sensFlux.ENTRANT &&
                x.statut_general == STATUT_TRANSFERT.initie &&
                (x.msgId == tr.msgId || x.endToEndId == tr.endToEndId || x.identifiantTransaction == tr.identifiantTransaction));

            if (exists)
            {
                _logger.LogInformation("[RecevoirDemandePaiement] Transfert déjà INITIÉ (MsgId={MsgId}, E2E={E2E}, Ident={Ident}).",
                    tr.msgId, tr.endToEndId, tr.identifiantTransaction);
                return;
            }

            // 6) Notification pour le payeur (qui doit accepter/refuser la demande)
            var notifPayload = new { montant, emetteur = tr.nomClientPaye };
            string json = System.Text.Json.JsonSerializer.Serialize(notifPayload);
            JsonDocument details = JsonDocument.Parse(json);

            var notif = new t_notification
            {
                type = type_notif.RTP_RECUE.ToString(),
                estCliquable = true,
                compte = tr.compteClientPayeur,
                idObject = tr.endToEndId, 
                dateAction = DateTime.Now,
                details = details
            };

            ctx.t_transfert.Add(tr);
            ctx.t_notification.Add(notif);

            await ctx.SaveChangesAsync();

            _logger.LogInformation("[RecevoirDemandePaiement] Transfert INITIÉ (E2E={E2E}, Ident={Ident}, Montant={Montant}).",
                tr.endToEndId, tr.identifiantTransaction, montant.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecevoirDemandePaiement] Exception: {Msg}", ex.Message);
            throw;
        }
    }
        
        
        
        public async Task RecevoirRetourFondsEtRepondre(RequeteRetourDesFondsDto dto, string iddemande)
    {
        if (dto is null)
        {
            _logger.LogWarning("[RecevoirRetourFondsEtRepondre] Payload NULL");
            return;
        }

        try
        {
            _logger.LogInformation("Réception d'une demande de retour de fonds (E2E={E2E}, MsgId={MsgId}, Montant={Montant})",
                dto.endToEndId, dto.msgId, dto.montantRetourne);

            await using var ctx = await _contextFactory.CreateDbContextAsync();

            // Idempotence: un retour INITIÉ au même montant existe déjà ?
            var retour = await ctx.t_retour_fonds.FirstOrDefaultAsync(r =>
                r.is_delete != true &&
                r.endToEndId == dto.endToEndId &&
                r.statut == statutRetourFond.initie &&
                r.montantRetourne == dto.montantRetourne);

            var isNew = retour is null;
            if (isNew)
            {
                retour = new t_retour_fonds
                {
                    endToEndId = dto.endToEndId,
                    raisonRetour = dto.raisonRetour,
                    montantRetourne = dto.montantRetourne,
                    msgId = dto.msgId,
                    sensFlux = sensFlux.ENTRANT,
                    statut = statutRetourFond.initie,
                    etape = etapeRetourFond.initie,
                };
            }


                var trs = await ctx.t_transfert.FirstOrDefaultAsync(r =>
                   r.is_delete != true &&
                   r.endToEndId == dto.endToEndId);


                // Prépare réservation
                retour.IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30);
            var reservationReq = new ReservationFondsBodyDto
            {
                numeroCompte = _aipdata.compteProduit,
                montantReserve = retour.montantRetourne.ToString(),
             //   identifiantTransaction = retour.IdOperationSib,
            };

            var resReservation = await _ServiceAIF.FaireUneReservationDeFonds(reservationReq, iddemande);

            // Par défaut, on RJCT avec AM21 si la réservation échoue
            string statutPI = "RJCT";
            string codeRejet = "AM21";
            string msgError = resReservation.detail ?? "Réservation impossible";

            // Renseigner infos de réservation si dispo
            if (!string.IsNullOrWhiteSpace(resReservation.data))
            {
                try
                {
                    var dataRes = JsonConvert.DeserializeObject<ReservationDto_AIF>(resReservation.data);
                    retour.numEvenementReserv = dataRes?.numeroEvenement;
                    retour.codeOperationReserv = dataRes?.codeOperation;
                    retour.codeAgenceReserv = dataRes?.codeAgenceTransaction;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[RecevoirRetourFondsEtRepondre] Parse ReservationDto_AIF KO");
                }
            }

            // Si réservation OK => ACSP, sinon on marque rejet
            if (Tools.Tools.RetourIsSucces(resReservation.status))
            {
                statutPI = "ACSP";
            }
            else
            {
                retour.etape = etapeRetourFond.rejete;
                retour.statut = statutRetourFond.rejete;
                retour.motifRejet = msgError;
            }

            // Réponse vers PI
            var rep = new ReponseAUneDemandeDeTransfert
            {
                endToEndId = dto.endToEndId,
                msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                msgIdDemande = dto.msgId,
                statutTransaction = statutPI,
                codeRaison = (statutPI == "RJCT") ? codeRejet : null,
                informationsAdditionnelles = (statutPI == "RJCT") ? msgError : null
            };

            await _envoieController.ReponseAUneDemandeDeTransfert(rep);

            // Persistance
            if (isNew) ctx.t_retour_fonds.Add(retour);
            else ctx.t_retour_fonds.Update(retour);

            await ctx.SaveChangesAsync();

            _logger.LogInformation("Fin traitement retour de fonds (E2E={E2E}) => StatutPI={StatutPI}", dto.endToEndId, statutPI);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecevoirRetourFondsEtRepondre] Exception: {Msg}", ex.Message);
            throw;
        }
    }


        public async Task RecevoirIrrevocabilite(ReponseRecuDemandeDeTransfert data, string iddemande)
        {
            if (data is null) return;

            await using var ctx = await _contextFactory.CreateDbContextAsync();

            // Helpers locaux
            async Task<string> BuildErrorDescAsync(string code, string? extra, string fallback)
            {
                var lib = await _codeErreurRepo.GetLibelleErreurAsync(code, code_datas.CODE_RAISON.ToString());
                if (string.IsNullOrWhiteSpace(lib)) lib = fallback;
                if (!string.IsNullOrWhiteSpace(extra)) lib += " " + extra;
                return lib;
            }

            async Task<string?> FindCodeBanqueAsync(string codeMembre)
            {
                var part = await _participantrepo.searchParticipant(codeMembre);
                return part != null ? (part.codeBanque ?? part.codeMembreParticipant) : null;
            }


            async Task<bool> LeveeReservationAsync(string? codeOp, string? numEvent, string? codeAgence)
            {
                if (string.IsNullOrWhiteSpace(codeOp) || string.IsNullOrWhiteSpace(numEvent) || string.IsNullOrWhiteSpace(codeAgence))
                    return true;

                var body = new CancelReservationBody
                {
                    codeOperation = codeOp,
                    numEvenement = numEvent,
                    codeAgenceTransaction = codeAgence
                };

                try
                {

                    var resLevee = await _ServiceAIF.LeveeUneReservationDeFonds(body, iddemande);

                    if (Tools.Tools.RetourIsSucces(resLevee.status))
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LeveeUneReservationDeFonds KO (codeOp={codeOp}, numEvent={numEvent})", codeOp, numEvent);
                    return false;

                }
            }

            bool LeveeReservationIsOk = false;

            // 1) Cas TRANSFERT DE DISPONIBILITÉ
            var trDispo = await ctx.t_transfert_dispo
                .FirstOrDefaultAsync(t => t.msgId == data.msgIdDemande && t.is_delete != true);

            if (trDispo != null)
            {
                if (trDispo.statut_general != STATUT_TRANSFERT.initie) return;

                switch (data.statutTransaction.ToUpper())
                {
                    case "RJCT":
                        {
                            var desc = await BuildErrorDescAsync(data.codeRaison, data.informationsAdditionnelles, "Transfert échoué");
                            trDispo.statut_general = STATUT_TRANSFERT.rejete;
                            trDispo.etape = ETAPE_TRANSFERT.REJETE;
                            trDispo.codeRejet = data.codeRaison;
                            trDispo.motifRejet = desc;
                            break;
                        }

                    case "ACSC":
                    case "ACCC":
                        {
                            trDispo.statut_general = STATUT_TRANSFERT.irrevocable;
                            trDispo.etape = ETAPE_TRANSFERT.VALIDE; // étape finale
                            trDispo.dateHeureIrrevocabilite = data.dateHeureIrrevocabilite;
                            break;
                        }
                }

                ctx.t_transfert_dispo.Update(trDispo);
                await ctx.SaveChangesAsync();
                return;
            }

            // 2) Cas TRANSFERT NORMAL
            var transfert = await ctx.t_transfert
                .FirstOrDefaultAsync(t => t.msgId == data.msgIdDemande && t.is_delete != true);

            if (transfert != null)
            {
                _logger.LogInformation("Réception irrévocabilité TRANSFERT (msgIdDemande={MsgIdDemande})", data.msgIdDemande);

                if (transfert.statut_general != STATUT_TRANSFERT.initie) return;

                // Marquer irrévocabilité "globale" (l’événement est arrivé)
                transfert.statut_general = STATUT_TRANSFERT.irrevocable;
                transfert.etape = ETAPE_TRANSFERT.IRREVOCABLE;
                transfert.dateHeureIrrevocabilite = data.dateHeureIrrevocabilite;
                ctx.t_transfert.Update(transfert);
                await ctx.SaveChangesAsync();



               LeveeReservationIsOk = await LeveeReservationAsync(transfert.codeOperationReserv, transfert.numEvenementReserv, transfert.codeAgenceReserv);
                   
                    // Lever la réservation dans tous les cas où elle a été posée et si le transfert est VALIDE , si elle echoue tenter 3 fois
                    // LeveeReservationIsOk = await RetryAsync(
                    //     async () => await LeveeReservationAsync(transfert.codeOperationReserv, transfert.numEvenementReserv, transfert.codeAgenceReserv),
                    //    maxAttempts: 3,
                    //   baseDelayMs: 300 );


                switch (data.statutTransaction)
                {
                    case "RJCT":
                        {
                            var desc = await BuildErrorDescAsync(data.codeRaison, data.informationsAdditionnelles, "Transfert échoué");

                            transfert.statut_general = STATUT_TRANSFERT.rejete;
                            transfert.etape = ETAPE_TRANSFERT.REJETE;
                            transfert.codeRejet = data.codeRaison;
                            transfert.motifRejet = desc;
                           
                            break;
                        }

                    case "ACSC": // Avis de débit (sort l’argent du payeur)
                        {
                            var codeBanquePaye = await FindCodeBanqueAsync(transfert.codeMembreParticipantPaye) ?? "";
                            var body = new OrdreDeDebitBodyDto
                            {
                                msgId = transfert.msgId,
                                endToEndId = transfert.endToEndId,
                                identifiantTransaction = transfert.IdOperationSib,
                                compteClientPayeur = transfert.ibanClientPayeur,
                                nomClientPayeur = transfert.nomClientPayeur,
                                montant = transfert.montant.ToString(),
                                compteClientPaye = transfert.compteClientPaye,
                                codeMembreParticipantPaye = codeBanquePaye,
                                nomClientPaye = transfert.nomClientPaye,
                                motif = transfert.motif,
                            };

                            var ret = await _ServiceAIF.OrdreDeDebit(body, iddemande);

                            if (!Tools.Tools.RetourIsSucces(ret.status))
                            {
                                transfert.etape = ETAPE_TRANSFERT.DEBIT_ECHOUE;
                                transfert.motifRejet = ret.detail;
                                transfert.codeRejet = ret.data;

                                if (LeveeReservationIsOk == true)
                                {
                                    /// Alors faire une reservation si l'ordre de debit echoue
                                    var bodyReservation = new ReservationFondsBodyDto
                                    {
                                        numeroCompte = transfert.compteClientPayeur,
                                        montantReserve = transfert.montant.ToString(),
                                       // identifiantTransaction = transfert.IdOperationSib,
                                        dateEcheance = DateTime.Now.AddDays(8).ToString("yyyy-MM-dd")
                                    };

                                    var res_reservation = await _ServiceAIF.FaireUneReservationDeFonds(bodyReservation, iddemande);

                                    if (!Tools.Tools.RetourIsSucces(ret.status))
                                    {
                                        if (res_reservation.data != null)
                                        {
                                            var data_reservation = JsonConvert.DeserializeObject<ReservationDto_AIF>(res_reservation.data);
                                            transfert.numEvenementReserv = data_reservation?.numeroEvenement;
                                            transfert.codeOperationReserv = data_reservation?.codeOperation;
                                            transfert.codeAgenceReserv = data_reservation?.codeAgenceTransaction;
                                        }
                                    }
                                }
                                
                            }
                            else
                            {
                                transfert.etape = ETAPE_TRANSFERT.VALIDE;
                            }

                            break;
                        }

                    case "ACCC": // Avis de crédit (crédite le bénéficiaire)
                        {
                            var codeBanquePayeur = await FindCodeBanqueAsync(transfert.codeMembreParticipantPayeur) ?? "";
                            var body = new OrdreDeCreditBodyDto
                            {
                                msgId = transfert.msgId,
                                endToEndId = transfert.endToEndId,
                                identifiantTransaction = transfert.IdOperationSib,
                                compteClientPayeur = transfert.compteClientPayeur,
                                nomClientPayeur = transfert.nomClientPayeur,
                                montant = transfert.montant.ToString(),
                                compteClientPaye = transfert.compteClientPaye,
                                codeMembreParticipantPayeur = codeBanquePayeur,
                                nomClientPaye = transfert.nomClientPaye,
                                motif = transfert.motif
                            };

                            var ret = await _ServiceAIF.OrdreDeCredit(body, iddemande);

                            if (!Tools.Tools.RetourIsSucces(ret.status))
                            {
                                // Échec crédit : marquer rejet + déclencher un retour de fonds
                                transfert.etape = ETAPE_TRANSFERT.REJETE;
                                transfert.motifRejet = ret.detail;
                                transfert.codeRejet = ret.data;

                                _logger.LogInformation("Déclenchement d’un retour de fonds (échec crédit).");

                                var tr = new t_retour_fonds
                                {
                                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                                    endToEndId = transfert.endToEndId,
                                    montantRetourne = transfert.montant.ToString(),
                                    sensFlux = sensFlux.SORTANT,
                                    statut = statutRetourFond.initie,
                                    etape = etapeRetourFond.initie,
                                    raisonRetour = "MD06", // “RefundRequestByEndCustomer” selon vos tables
                                    IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30)
                                };
                                await ctx.t_retour_fonds.AddAsync(tr);

                                var rtf = new RequeteRetourDesFondsDto
                                {
                                    endToEndId = tr.endToEndId,
                                    msgId = tr.msgId,
                                    montantRetourne = tr.montantRetourne,
                                    raisonRetour = tr.raisonRetour
                                };
                                await _envoieController.RetournerLesFonds(rtf);
                            }
                            else
                            {
                                transfert.etape = ETAPE_TRANSFERT.VALIDE;
                            }

                            break;
                        }
                }

            
                ctx.t_transfert.Update(transfert);
                await ctx.SaveChangesAsync();

                _logger.LogInformation("Fin traitement irrévocabilité TRANSFERT (E2E={E2E})", transfert.endToEndId);
                return;
            }

            // 3) Cas RETOUR DE FONDS
            _logger.LogInformation("Réception irrévocabilité RETOUR DE FONDS (E2E={E2E})", data.endToEndId);

            var retour = await ctx.t_retour_fonds
                .FirstOrDefaultAsync(r => r.endToEndId == data.endToEndId && r.is_delete != true);

            var trConcerne = await ctx.t_transfert
                .FirstOrDefaultAsync(t => t.endToEndId == data.endToEndId && t.is_delete != true);

            if (retour == null || trConcerne == null) return;

            if (retour.statut != statutRetourFond.initie) return;

            bool transfertValide = (trConcerne.etape == ETAPE_TRANSFERT.VALIDE);

            // Marque l’irrévocabilité du retour
            retour.statut = statutRetourFond.irrevocable;
            retour.etape = etapeRetourFond.irrevocable;
            retour.dateHeureIrrevocabilite = data.dateHeureIrrevocabilite;
            ctx.t_retour_fonds.Update(retour);

    
            // Lever la réservation seulement si le transfert initial avait vraiment été validé
            if (transfertValide)
                await LeveeReservationAsync(retour.codeOperationReserv, retour.numEvenementReserv, retour.codeAgenceReserv);



            switch (data.statutTransaction)
            {
                case "RJCT":
                    {
                        _logger.LogInformation("Irrevocabilité RETOUR: RJCT");

                        var desc = await BuildErrorDescAsync(data.codeRaison, data.informationsAdditionnelles, "Retour de fonds échoué");
                        retour.etape = etapeRetourFond.rejete;
                        retour.statut = statutRetourFond.rejete;
                        retour.codeRejet = data.codeRaison;
                        retour.motifRejet = desc;

                        trConcerne.retourEtape = etapeRetourFond.rejete;
                        trConcerne.retourStatut = STATUT_TRANSFERT.rejete;

                        break;
                    }

                case "ACSC": // Avis de débit
                    {
                        _logger.LogInformation("Irrevocabilité RETOUR: ACSC");

                        if (!transfertValide)
                        {
                            // Pas d’écriture bancaire à faire (transfert initial non totalement validé)
                            retour.etape = etapeRetourFond.valide;
                            retour.statut = statutRetourFond.irrevocable;

                            trConcerne.retourEtape = etapeRetourFond.valide;
                            trConcerne.retourStatut = STATUT_TRANSFERT.irrevocable;
                           
                        }
                        else
                        {
                            var codeBanquePaye = await FindCodeBanqueAsync(trConcerne.codeMembreParticipantPaye) ?? "";

                            var body = new OrdreDeDebitBodyDto
                            {
                                msgId = retour.msgId,
                                endToEndId = retour.endToEndId,
                                identifiantTransaction = retour.IdOperationSib,
                                compteClientPayeur = trConcerne.ibanClientPayeur,
                                nomClientPayeur = trConcerne.nomClientPayeur,
                                montant = retour.montantRetourne,
                                compteClientPaye = trConcerne.compteClientPaye,
                                codeMembreParticipantPaye = codeBanquePaye,
                                nomClientPaye = trConcerne.nomClientPaye,
                                motif = trConcerne.motif
                            };

                            var ret = await _ServiceAIF.OrdreDeDebit(body, iddemande);
                            if (!Tools.Tools.RetourIsSucces(ret.status))
                            {
                                retour.etape = etapeRetourFond.rejete;
                                retour.statut = statutRetourFond.rejete;
                                retour.motifRejet = ret.detail;
                                retour.codeRejet = ret.data;

                                trConcerne.retourEtape = etapeRetourFond.rejete;
                                trConcerne.retourStatut = STATUT_TRANSFERT.rejete;
                            }
                            else
                            {
                                retour.etape = etapeRetourFond.valide;
                                retour.statut = statutRetourFond.irrevocable;

                                trConcerne.retourEtape = etapeRetourFond.valide;
                                trConcerne.retourStatut = STATUT_TRANSFERT.irrevocable;

                            }
                        }

                        break;
                    }

                case "ACCC": // Avis de crédit
                    {
                        if (!transfertValide)
                        {
                            retour.etape = etapeRetourFond.valide;
                            retour.statut = statutRetourFond.irrevocable;

                            trConcerne.retourEtape = etapeRetourFond.valide;
                            trConcerne.retourStatut = STATUT_TRANSFERT.irrevocable;
                        }
                        else
                        {
                            var codeBanquePayeur = await FindCodeBanqueAsync(trConcerne.codeMembreParticipantPaye) ?? "";

                            var body = new OrdreDeCreditBodyDto
                            {
                                msgId = retour.msgId,
                                endToEndId = retour.endToEndId,
                                identifiantTransaction = retour.IdOperationSib,
                                compteClientPayeur = trConcerne.compteClientPaye,
                                nomClientPayeur = trConcerne.nomClientPaye,
                                montant = retour.montantRetourne,
                                compteClientPaye = trConcerne.compteClientPayeur,
                                codeMembreParticipantPayeur = codeBanquePayeur,
                                nomClientPaye = trConcerne.nomClientPayeur,
                                motif = trConcerne.motif
                            };

                            var ret = await _ServiceAIF.OrdreDeCredit(body, iddemande);
                            if (!Tools.Tools.RetourIsSucces(ret.status))
                            {
                                retour.etape = etapeRetourFond.rejete;
                                retour.motifRejet = ret.detail;
                                retour.codeRejet = ret.data;

                                trConcerne.retourEtape = etapeRetourFond.rejete;
                                trConcerne.retourStatut = STATUT_TRANSFERT.rejete;
                            }
                            else
                            {
                                retour.etape = etapeRetourFond.valide;

                                trConcerne.retourEtape = etapeRetourFond.valide;
                                trConcerne.retourStatut = STATUT_TRANSFERT.irrevocable;
                            }
                        }

                        break;
                    }
            }

           
            if (retour.etape == etapeRetourFond.valide)
                trConcerne.etape = ETAPE_TRANSFERT.DESACTVE;
                trConcerne.statut_general = STATUT_TRANSFERT.desactive;

            /// si annulation sur le transfert // Flager le statut de l'annulation sur la ligne du transfert
            if (trConcerne.annulationStatut == STATUT_TRANSFERT.initie)
                trConcerne.annulationStatut = trConcerne.retourStatut;

           
            ctx.t_retour_fonds.Update(retour);
            ctx.t_transfert.Update(trConcerne);
            await ctx.SaveChangesAsync();

            _logger.LogInformation("Fin traitement irrévocabilité RETOUR DE FONDS (E2E={E2E})", data.endToEndId);
        }


        public async Task RecevoirRejetRetourDeFonds(ReponseReceiveDemandeAnnulationDto _data, string iddemande)
        {
            if (_data is null)
            {
                _logger.LogWarning("[RecevoirRejetRetourDeFonds] Payload nul.");
                return;
            }

            try
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();

                // 1) Récupérer le retour de fonds encore INITIÉ
                var rf = await ctx.t_retour_fonds
                    .FirstOrDefaultAsync(p => p.endToEndId == _data.endToEndId
                                           && p.is_delete != true
                                           && p.statut == statutRetourFond.initie);

                if (rf is null)
                {
                    _logger.LogWarning("[RecevoirRejetRetourDeFonds] Aucun retour_fonds INITIÉ pour E2E={E2E}.", _data.endToEndId);
                    return;
                }

                // 2) Récupérer le transfert concerné (si besoin de notifier/propager l’état)
                var transfert = await ctx.t_transfert
                    .FirstOrDefaultAsync(p => p.endToEndId == _data.endToEndId && p.is_delete != true);

                // 3) Déterminer code + libellé

                // On tente d’obtenir un libellé “métier”, sinon on prend une valeur par défaut + détails fournis
                string desc = await _codeErreurRepo.GetLibelleErreurAsync(
                    _data.raison,
                    tag_erreur.CODE_RAISON_DEMANDE_RETOUR_FONDS.ToString()  
                );

                if (string.IsNullOrWhiteSpace(desc)) desc = "Retour de fonds rejeté";

                // 4) Mettre à jour le retour de fonds
                rf.codeRejet = _data.raison;
                rf.statut = statutRetourFond.rejete;
                rf.etape = etapeRetourFond.rejete;
                rf.motifRejet = desc;
                ctx.t_retour_fonds.Update(rf);

                // 5) Répercuter sur le transfert (champs “retour”, pas “annulation”)
                if (transfert is not null)
                {
                    transfert.retourStatut = STATUT_TRANSFERT.rejete;
                    transfert.retourEtape = etapeRetourFond.rejete;
                    transfert.retourStatutRaison = $"{_data.raison}:{desc}";
                    ctx.t_transfert.Update(transfert);

                    // 6) Notifier l’émetteur du retour (client payeur)
                    var dataNotif = new { montant = transfert.montant, emetteur = transfert.nomClientPaye };
                    string json = System.Text.Json.JsonSerializer.Serialize(dataNotif);
                    JsonDocument details = JsonDocument.Parse(json);

                    var n = new t_notification
                    {
                        type = type_notif.ANNULATION_REJETEE.ToString(),
                        estCliquable = true,
                        compte = transfert.compteClientPayeur,
                        idObject = transfert.endToEndId,
                        dateAction = DateTime.Now,
                        details = details
                    };
                    await ctx.t_notification.AddAsync(n);
                }

                // 7) Persist
                await ctx.SaveChangesAsync();

                _logger.LogInformation(
                    "[RecevoirRejetRetourDeFonds] Rejet traité pour E2E={E2E} | Code={Code} | Desc={Desc}",
                    _data.endToEndId, _data.raison, desc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RecevoirRejetRetourDeFonds] Exception : {Msg}", ex.Message);
                throw;
            }
        }

        public async Task RecevoirDemandeDeRetourDeFonds(ReceiveDemandeAnnulationDto retreq, string iddemande)
        {
            if (retreq is null)
            {
                _logger.LogWarning("[RecevoirDemandeDeRetourDeFonds] Payload nul.");
                return;
            }

            try
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();

                var transfert = await ctx.t_transfert
                    .FirstOrDefaultAsync(p => p.endToEndId == retreq.endToEndId && p.is_delete != true);

                if (transfert is null)
                {
                    _logger.LogWarning("[RecevoirDemandeDeRetourDeFonds] Transfert introuvable (E2E={E2E}).", retreq.endToEndId);
                    return;
                }

                
                // Marque l'annulation côté transfert
                transfert.annulationStatut = STATUT_TRANSFERT.initie;
                transfert.annulationRaison = retreq.raison;
                transfert.annulationDate = DateTime.Now;
                ctx.t_transfert.Update(transfert); // 

                // Ligne d'annulation (flux ENTRANT)
                var ann = new t_annulation_transfert
                {
                    endToEndId = retreq.endToEndId,
                    codeMembreParticipantPayeur = retreq.codeMembreParticipantPayeur,
                    codeMembreParticipantPaye = _aipdata.codemembre,
                    raison = retreq.raison,
                    msgId = retreq.msgId,
                    sensFlux = sensFlux.ENTRANT,
                    statut = statutAnnulation.initie
                };
                await ctx.t_annulation_transfert.AddAsync(ann);

                // Notification (au bénéficiaire local)
                var payload = new { montant = transfert.montant, emetteur = transfert.nomClientPayeur };
                string json = System.Text.Json.JsonSerializer.Serialize(payload);
                JsonDocument details = JsonDocument.Parse(json);

                var notif = new t_notification
                {
                    type = type_notif.ANNULATION_DEMANDEE.ToString(),
                    estCliquable = true,
                    compte = transfert.compteClientPaye,
                    idObject = retreq.endToEndId,
                    dateAction = DateTime.Now,
                    details = details
                };
                await ctx.t_notification.AddAsync(notif);

                await ctx.SaveChangesAsync();

                _logger.LogInformation(
                    "[RecevoirDemandeDeRetourDeFonds] Annulation INITIÉE pour E2E={E2E}, raison={Raison}.",
                    retreq.endToEndId, retreq.raison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RecevoirDemandeDeRetourDeFonds] Exception : {Msg}", ex.Message);
                throw;
            }
        }


        public async Task TraiterCasRevendication(string id, string statut, DateTime? eventDate)
        {

            string[] partsId = id.Split('-');
            string alias = $"{partsId[0]}";

            switch (statut)
            {
                case "INITIEE":
                    using (var Cont = _contextFactory.CreateDbContext())
                    {

                        string _compte_owner = "";

                        ///Recherche qui appartient l'alias
                        var data_alias = await Cont.t_alias
           .Where(p => (p.valeurAlias == alias || p.shid == alias) && p.is_delete != true)
           .FirstOrDefaultAsync();

                        if (data_alias != null)
                            _compte_owner = data_alias.ibanOrOther;

                        t_revendication rev_recu = new t_revendication
                        {
                            alias = alias,
                            idRevendicationPi = id,
                            statut = statut_revendication.INITIE,
                            sensFlux = sensFlux.ENTRANT,
                            pspDetenteur = _aipdata.codemembre,
                            dateDemande = eventDate,
                            dateCreation = eventDate,
                            compte = _compte_owner
                        };

                        Cont.t_revendication.Update(rev_recu);


                        var data = new
                        {
                            cle = alias,
                        };

                        string json = JsonSerializer.Serialize(data);
                        JsonDocument details = JsonDocument.Parse(json);

                        t_notification n = new t_notification
                        {

                            type = type_notif.REVENDICATION_INITIEE.ToString(),
                            estCliquable = true,
                            compte = rev_recu.compte,
                            idObject = rev_recu.Id.ToString(),
                            dateAction = DateTime.Now,
                            details = details
                        };


                        await Cont.SaveChangesAsync();
                    }
                    break;
                case "ACCEPTEE":
                case "REJETEE":

                    using (var Cont = _contextFactory.CreateDbContext())
                    {

                        var revendications = await Cont.t_revendication
                            .Where(p => p.idRevendicationPi == id && p.is_delete != true && p.statut == statut_revendication.INITIE)
                            .ToListAsync();


                        // Suppression de l'alias de la bd
                        t_alias aliasTrouve = await Cont.t_alias.Where(a => a.valeurAlias == alias && a.is_delete != true).FirstOrDefaultAsync();

                        bool bSaveContext = false;
                        if (aliasTrouve != null)
                        {

                            aliasTrouve.aliasMbnoOld = aliasTrouve.valeurAlias;
                            aliasTrouve.dateSuppressionAliasMbno = DateTime.UtcNow;

                            aliasTrouve.valeurAlias = aliasTrouve.shid;
                            aliasTrouve.typeAlias = "SHID";
                            Cont.t_alias.Update(aliasTrouve);
                            bSaveContext = true;
                        }


                        if (revendications.Any())
                        {
                            var nouveauStatut = statut == "ACCEPTEE" ? statut_revendication.ACCEPTEE : statut_revendication.REJETEE;

                            foreach (var rev in revendications)
                            {
                                rev.statut = nouveauStatut;
                                Cont.t_revendication.Update(rev);



                                var data = new
                                {
                                    cle = rev.alias,
                                };


                                string json = JsonSerializer.Serialize(data);
                                JsonDocument details = JsonDocument.Parse(json);


                                type_notif StatutNotif = (rev.statut == statut_revendication.ACCEPTEE) ? type_notif.REVENDICATION_ACCEPTE : type_notif.REVENDICATION_REJETE;

                                t_notification n = new t_notification
                                {

                                    type = StatutNotif.ToString(),
                                    estCliquable = true,
                                    compte = rev.compte,
                                    idObject = rev.Id.ToString(),
                                    dateAction = DateTime.Now,
                                    details = details
                                };
                                Cont.t_revendication.Add(rev);

                            }
                            bSaveContext = true;
                        }




                        if (bSaveContext == true)
                            await Cont.SaveChangesAsync();


                    }
                    break;

                case "CLOTUREE":

                    using (var Cont = _contextFactory.CreateDbContext())
                    {

                        var revendications = await Cont.t_revendication
                            .Where(p => p.idRevendicationPi == id && p.is_delete != true)
                            .ToListAsync();

                        if (revendications.Any())
                        {
                            foreach (var rev in revendications)
                            {
                                rev.dateCloture = eventDate;
                                Cont.t_revendication.Update(rev);
                            }

                            await Cont.SaveChangesAsync();
                        }

                    }
                    break;

                default:
                    Console.WriteLine("Type inconnu");
                    break;
            }

        }

        public async Task RecevoirNotificationInfoWarn(RcpNotificationInfoWarnDto _body, string iddemande)
        {
            try
            {

                string[] parts = _body.evenementDescription.Split('-');
                string action = parts[0];       // "REVENDICATION"
                string id = parts[1] + '-' + parts[2];       // "REVENDICATION"
                string statut = parts[^1];

                switch (action)
                {

                    case "REVENDICATION":
                        await TraiterCasRevendication(id, statut, _body.evenementDate);
                        break;
                    default:
                        break;
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"[RecevoirNotificationInfoWarn] Exception =================> {ex.Message}");
                throw;
            }
        }

        public async Task saveMessage(typeMessage type, string body, bool bmsgIdEstmsgDemande = false)
        {
            try
            {
                _logger.LogError($"[saveMessage de reponse a recherche alias  =================> BEFORE");

                using (var context = _contextFactory.CreateDbContext())
                {
                    await MessageService.SaveMessageAsync(context, _logger, type, body, bmsgIdEstmsgDemande);
                }
                _logger.LogError($"[saveMessage de reponse a recherche alias  =================> AFTER");

            }
            catch (Exception ex)
            {

                _logger.LogError($"[saveMessage] Exception =================> {ex.Message} =========>INner exception ==> {ex.InnerException}");
                throw;
            }
        }



        private static async Task<bool> RetryAsync(Func<Task<bool>> action, int maxAttempts = 3,int baseDelayMs = 300,Action<int, Exception?>? onAttemptFail = null,CancellationToken ct = default)
        {
            if (maxAttempts <= 0) maxAttempts = 1;

            var rng = new Random();

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    var ok = await action();
                    if (ok) return true;
                }
                catch (Exception ex)
                {
                    onAttemptFail?.Invoke(attempt, ex);
                    // on continue les tentatives sauf si dernière
                    if (attempt == maxAttempts) return false;
                }

                if (attempt < maxAttempts)
                {
                    // backoff exponentiel:  base * 2^(attempt-1)  + 0–120ms jitter
                    var delay = (int)(baseDelayMs * Math.Pow(2, attempt - 1)) + rng.Next(0, 121);
                    try { await Task.Delay(delay, ct); } catch (TaskCanceledException) { return false; }
                }
            }

            return false;
        }


        public async Task saveMessageReponse(int id, string body)
        {
            try
            {
                using (var Cont = _contextFactory.CreateDbContext())
                {
                    var msg = await Cont.t_message.Where(p => p.Id == id)
                        .OrderByDescending(p => p.date_message)
                        .FirstOrDefaultAsync();

                    {
                        msg.date_reponse = DateTime.Now;
                        msg.reponse_message = body;
                        Cont.t_message.Update(msg);
                        await Cont.SaveChangesAsync();
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"[saveMessageReponse] Exception =================> {ex.Message}");
                throw;
            }
        }


     
    }

}