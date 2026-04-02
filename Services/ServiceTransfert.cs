using InteroperabiliteProject.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.RequestToReceiveDto;
using InteroperabiliteProject.Implementation;
using InteroperabiliteProject.Controllers;
using InteroperabiliteProject.Event;
using AutoMapper;
using InteroperabiliteProject.DtoAppMobile;
using InteroperabiliteProject.DtoAppBusiness;
using InteroperabiliteProject.Dtos.EnvoieController;
using InteroperabiliteProject.DtoAppMobile.Annulation;
using System.Text.Json;
using InteroperabiliteProject.ContextDb;
using Microsoft.EntityFrameworkCore;
using InteroperabiliteProject.Facturation.Dtos;
using InteroperabiliteProject.DtoAIP;
using ask.Dtos.RequestToSendDto;
using ask.Dtos.RequestToReceiveDto;
using ask.Dtos.General;


namespace InteroperabiliteProject.ServicceAIP
{
    public class ServiceTransfert
    {
        private readonly IDbContextFactory<InteropContext> _contextFactory;
        private readonly ILogger<AliasRepo> _logger;
        private readonly ServiceAIF _serviceAIF;
        private readonly EnvoieController _envoieController;
        private readonly EventService _eventService;
        private readonly AIPDATA _aipdata;
        private readonly IemployeRepo _aliasRepo;
        private readonly IclientRepo _clientRepo;
        private readonly ItransfertRepo _transfertRepo;
        private readonly ItransfertDispoRepo _transfertDispoRepo;
        private readonly IoperationmasseRepo _opMasseRepo;
        private readonly InotificationRepo _notificationRepo;
        private readonly IMapper _imapper;
        private readonly IdatasRepo _datarepo;
        private readonly ICodeErreurRepo _codeErreurRepo;
        private readonly IEntiteRepo _participantRepo;
        private readonly ItransfertAutoriseRepo _transfertAutorepo;
        private readonly ItransfertPlafondRepo _transfertPlafondrepo;
        private readonly Iannulation_transfert _annulationTransfertRepo;
        private readonly IRetourFondRepo _retourFondrepo;
        private readonly IParametreSystemeRepo _parametreSystemeRepo;
        private readonly IscheduledRepo _scheduledrepo;
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public ServiceTransfert(ILogger<AliasRepo> logger, IDbContextFactory<InteropContext> dbContextFactory, ServiceAIF serviceAIF, IOptions<AIPDATA> aipdata, EnvoieController envoieController, EventService eventService, IemployeRepo aliasRepo, IParametreSystemeRepo parametreSystemeRepo, ICodeErreurRepo codeErreurRepo, ItransfertDispoRepo transfertDispoRepo, IclientRepo clientRepo, InotificationRepo notificationRepo, ItransfertRepo transfertRepo, IMapper imapper, IdatasRepo datarepo, ItransfertAutoriseRepo transfertAutorepo, ItransfertPlafondRepo transfertPlafondrepo, IscheduledRepo scheduledRepo, IEntiteRepo participantRepo, IoperationmasseRepo opMasseRepo, Iannulation_transfert annulationTransfertRepo, IRetourFondRepo retourFondrepo
)
        {
            _logger = logger;
            _serviceAIF = serviceAIF;
            _envoieController = envoieController;
            _eventService = eventService;
            _participantRepo = participantRepo;
            _parametreSystemeRepo = parametreSystemeRepo;
            _scheduledrepo = scheduledRepo;
            _aliasRepo = aliasRepo;
            _codeErreurRepo = codeErreurRepo;
            _transfertRepo = transfertRepo;
            _transfertDispoRepo = transfertDispoRepo;
            _notificationRepo = notificationRepo;
            _clientRepo = clientRepo;
            _aipdata = aipdata.Value;
            _imapper = imapper;
            _datarepo = datarepo;
            _transfertAutorepo = transfertAutorepo;
            _transfertPlafondrepo = transfertPlafondrepo;
            _retourFondrepo = retourFondrepo;
            _annulationTransfertRepo = annulationTransfertRepo;

            jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            _opMasseRepo = opMasseRepo;
            _contextFactory = dbContextFactory;

        }


   
        public async Task<GeneraleRetour> VerificationIdentite(string codePSP, string iban, string other, string iddemande)
        {
            const string _script = "Vérification d'identité";

            try
            {
                _logger.LogInformation("[{Script}] codeParticipant={codePSP}, IBAN={iban}, Other={other}, IdDemande={iddemande}",
                    _script, codePSP, iban, other, iddemande);

                // 1) Entrées requises
                var typeInit = Tools.Tools.DeterminerTypeInitiation("", iban, other);
                var ibanOrOther = (typeInit == Type_initie.iban) ? iban : other;

                if (string.IsNullOrWhiteSpace(codePSP) || string.IsNullOrWhiteSpace(ibanOrOther))
                    return new GeneraleRetour { status = 400, detail = "Les données envoyées ne sont pas conformes" };

                // 2) Cas local (PSP courant) : on interroge AIF puis équivalence AIP
                if (codePSP == _aipdata.codemembre)
                {
                    var retReqAIF = await _serviceAIF.GetClientCompte(ibanOrOther, iddemande);
                    if (!retReqAIF.operationStatus)
                        return new GeneraleRetour { status = retReqAIF.status, detail = retReqAIF.erreur };

                    var ret_AIF = JsonConvert.DeserializeObject<Message>(retReqAIF.data);
                    if (ret_AIF == null)
                        return new GeneraleRetour { status = 502, detail = "Réponse AIF invalide" };

                    var retReqAIP = await _serviceAIF.GetEquivalenceClientCompte(ret_AIF);
                    _logger.LogInformation("[{Script}] Réponse équivalence AIF => {Statut}", _script, retReqAIP.operationStatus);

                    if (!retReqAIP.operationStatus)
                        return new GeneraleRetour { status = retReqAIP.status, detail = "Le numéro de compte est invalide ou manquant" };

                    var ret_AIP = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationAIF>(retReqAIP.data);
                    if (ret_AIP == null)
                        return new GeneraleRetour { status = 502, detail = "Réponse AIP invalide" };

                    var dto = MapToDataPaye(ret_AIP, _aipdata.devise);
                    // endToEnd pour la voie locale
                    dto.endToEndId = Tools.Tools.GenerateEndToEndCode(_aipdata);

                    return new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(dto, jsonSerializerSettings) };
                }

                // 3) Cas remote PSP : on envoie une DemandeVerificationIdentite puis on attend l’event
                var body = new RequeteDemandeVerificationIdentiteAip
                {
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    endToEndId = Tools.Tools.GenerateEndToEndCode(_aipdata),
                    msgIddemande = Tools.Tools.GenererMessageId("PIUMOA"),
                    codeMembreParticipant = codePSP,
                    ibanClient = string.IsNullOrWhiteSpace(iban) ? null : iban,
                    otherClient = string.IsNullOrWhiteSpace(other) ? null : other
                };

                var resultat = await _envoieController.DemandeVerificationIdentite(body);
                if (resultat == null)
                    return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };

                if (!resultat.operationResult)
                    return new GeneraleRetour { status = 400, detail = resultat.messageResult };

                var requestId = _eventService.RegisterRequest(resultat.idoperation);
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto env = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON_REJET_VERIFICATION.ToString());

                if (!Tools.Tools.RetourIsSucces(env.status_code))
                    return (new GeneraleRetour { status = env.status_code, detail = env.desc_error });

                ReponseAUneDemandeDeVerificationIdentite rep = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationIdentite>(env.data);
                
                // 4) Traitement du résultat côté AIP
                if (string.Equals(rep.resultatVerification, "false", StringComparison.OrdinalIgnoreCase))
                {
                    string desc = await _codeErreurRepo.GetLibelleErreurAsync(rep.codeRaison, tag_erreur.CODE_RAISON_REJET_VERIFICATION.ToString());

                    if (string.IsNullOrWhiteSpace(desc))
                        desc = "Le numéro de compte est invalide ou manquant";

                    return new GeneraleRetour { status = 404, detail = desc };
                }

                var dtoRemote = MapToDataPaye(rep, _aipdata.devise);
                // endToEnd fourni par AIP pour la voie remote
                dtoRemote.endToEndId = rep.endToEndId;

                return new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(dtoRemote, jsonSerializerSettings) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Script}] Erreur inattendue", _script);
                return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
            }
        }


        private static DataPayeDto MapToDataPaye(ReponseAUneDemandeDeVerificationAIF src, string defaultDevise)
        {
            var dto = new DataPayeDto
            {
                participant = src.codeMembreParticipant,
                iban = src.ibanClient,
                numeroRCCM = src.numeroRCCMClient,
                dateNaissance = src.dateNaissance,
                paysNaissance = src.paysNaissance,
                paysResidence = src.paysResidence,
                villeNaissance = src.villeNaissance,
                devise = string.IsNullOrEmpty(src.devise) ? defaultDevise : src.devise,
                typeCompte = src.typeCompte,
                type = src.typeClient,
                ville = src.villeClient,
                adresseComplete = src.adresseComplete,
                nom = src.nomClient,
                other = src.otherClient
            };

            if (!string.Equals(dto.typeCompte, "TRAL", StringComparison.OrdinalIgnoreCase))
            {
                dto.numeroIdentification = src.numeroIdentification;
                dto.systemeIdentification = src.systemeIdentification;
            }

            return dto;
        }

        private static DataPayeDto MapToDataPaye(ReponseAUneDemandeDeVerificationIdentite src, string defaultDevise)
        {
            var dto = new DataPayeDto
            {
                participant = src.codeMembreParticipant,
                iban = src.ibanClient,
                numeroRCCM = src.numeroRCCMClient,
                dateNaissance = src.dateNaissance,
                paysNaissance = src.paysNaissance,
                paysResidence = src.paysResidence,
                villeNaissance = src.villeNaissance,
                devise = string.IsNullOrEmpty(src.devise) ? defaultDevise : src.devise,
                typeCompte = src.typeCompte,
                type = src.typeClient,
                ville = src.villeClient,
                adresseComplete = src.adresseComplete,
                nom = src.nomClient,
                other = src.otherClient
            };

            if (!string.Equals(dto.typeCompte, "TRAL", StringComparison.OrdinalIgnoreCase))
            {
                dto.numeroIdentification = src.numeroIdentification;
                dto.systemeIdentification = src.systemeIdentification;
            }

            return dto;
        }



        public async Task<GeneraleRetour> RechercheAlias(string alias, bool enabledInternalTransfer, bool bDemandePaiement = false)
        {
            const string _script = "Recherche d'alias";

            try
            {
                _logger.LogInformation("[{Script}] RechercheAlias => alias={Alias}, virementInterne={EnabledInternalTransfer}, demandePaiement={DemandePaiement}",
                    _script, alias, enabledInternalTransfer, bDemandePaiement);

                if (string.IsNullOrWhiteSpace(alias))
                    return new GeneraleRetour { status = 400, title = "Bad Request", detail = "Veuillez saisir l'alias à rechercher" };

                var res_data = new DataPayeDto();

                // 1) Recherche locale si virement interne activé
                if (enabledInternalTransfer)
                {
                    var local = await _aliasRepo.SearchAliasByAlias(alias);
                    _logger.LogInformation("[{Script}] Alias local trouvé = {Found}", _script, local != null);

                    if (local != null)
                    {
                        // Mapping local -> DataPayeDto
                        res_data.nom = (local.categorie == "B" || local.categorie == "G") ? local.denominationSociale : local.nomClient;
                        res_data.iban = local.iban;
                        res_data.typeCompte = local.typeCompte;
                        res_data.type = local.categorie;
                        res_data.participant = local.participant;
                        res_data.paysResidence = local.paysResidence;
                        res_data.adresseComplete = local.adresse;
                        res_data.villeNaissance = local.villeNaissance;
                        res_data.ville = local.ville;
                        res_data.other = local.other;
                        res_data.paysNaissance = local.PaysNaissance;
                        res_data.endToEndId = Tools.Tools.GenerateEndToEndCode(_aipdata);
                        res_data.dateNaissance = local.dateNaissance;
                        res_data.devise = _aipdata.devise;
                        res_data.alias = local.valeurAlias;
                        res_data.photo = local.photo;

                        if (res_data.type == "C")
                            res_data.numeroRCCM = local.identificationRccm;

                        if (!string.Equals(res_data.typeCompte, "TRAL", StringComparison.OrdinalIgnoreCase))
                        {
                            var (sysId, numId) = Tools.Tools.TraiterNumPiece(res_data.type, local.identificationNationaleClient, local.numeroPasseport, local.identificationFiscale);
                            res_data.systemeIdentification = sysId;
                            res_data.numeroIdentification = numId;
                        }

                        return new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(res_data, jsonSerializerSettings) };
                    }
                }

                // 2) Recherche via AIP
                var body = new RequeteDemandeDeRechercheAliasClient { alias = alias };
                var retReq = await _envoieController.DemandeDeRechercheAlias(body);
                if (!retReq.operationResult)
                    return new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.messageResult };

                var requestId = _eventService.RegisterRequest(retReq.idoperation);
                var evtMessage = await _eventService.GetTasks(requestId);

                ReponseTraiteDto env = await TraiterRetourEvenement(evtMessage, tag_erreur.CODE_RAISON_REJET_RECHERCHE_ALIAS.ToString());

                if (!Tools.Tools.RetourIsSucces(env.status_code))
                    return (new GeneraleRetour { status = env.status_code, detail = env.desc_error });

                ReponseADemandeRechercheAlias rep = JsonConvert.DeserializeObject<ReponseADemandeRechercheAlias>(env.data);

                if (env.status_code != 200 || rep == null)
                    return new GeneraleRetour { status = env.status_code, detail = env.desc_error };

                _logger.LogInformation("[{Script}] Réponse AIP: statut={Statut}", _script, rep.statut);

                if (!string.Equals(rep.statut, "SUCCES", StringComparison.OrdinalIgnoreCase))
                {
                    var msg404 = bDemandePaiement
                        ? "L'alias du payeur n'existe pas dans PI"
                        : "L'alias du bénéficiaire n'existe pas dans PI";
                    return new GeneraleRetour { status = 404, title = "Not Found", detail = msg404 };
                }

                // Mapping remote -> DataPayeDto
                res_data.nom = (rep.categorie == "B" || rep.categorie == "G") ? rep.denominationSociale : rep.nom;
                res_data.iban = rep.iban;
                res_data.typeCompte = rep.typeCompte;
                res_data.type = rep.categorie;
                res_data.participant = rep.participant;
                res_data.paysResidence = rep.paysResidence;
                res_data.adresseComplete = rep.adresse;
                res_data.alias = rep.valeurAlias;
                res_data.villeNaissance = rep.villeNaissance;
                res_data.ville = rep.ville;
                res_data.other = rep.other;
                res_data.paysNaissance = rep.paysNaissance;
                res_data.dateNaissance = rep.dateNaissance;
                res_data.endToEndId = rep.endToEndId;
                res_data.devise = _aipdata.devise;
                res_data.photo = rep.photo;

                if (res_data.type == "C")
                    res_data.numeroRCCM = rep.identificationRccm;

                if (!string.Equals(res_data.typeCompte, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    var (sysId, numId) = Tools.Tools.TraiterNumPiece(res_data.type, rep.identificationNationale, rep.numeroPasseport, rep.identificationFiscale);
                    res_data.systemeIdentification = sysId;
                    res_data.numeroIdentification = numId;
                }

                return new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(res_data, jsonSerializerSettings) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Script}] Erreur inattendue", _script);
                return new GeneraleRetour { status = 500, title = "Internal Server Error", detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
            }
        }

      
        public async Task<GeneraleRetour> InitierTransfert(string alias_connecte, QueryBodyTransactionDto data_transfert, string iddemande)
        {
            const string _script = "SERVICE D'INITIATION DU TRANSFERT";

            try
            {
                _logger.LogInformation("{Script} - Début. Canal={Canal}, AliasPayeur={Alias}", _script, data_transfert?.canal, alias_connecte);

                // -------- Validation d’entrée
                var invalidParams = new List<InvalidParam>();

                var validator = new QueryBodyTransactionDtoValidator();
                var results = validator.Validate(data_transfert ?? new QueryBodyTransactionDto());
                if (!results.IsValid)
                {
                    invalidParams.AddRange(results.Errors.Select(e => new InvalidParam { name = e.PropertyName, reason = e.ErrorMessage }));
                }

                if (!Tools.Tools.canalEstCanalTransfertPaiement(data_transfert.canal))
                    invalidParams.Add(new InvalidParam { name = "canal", reason = "Le canal choisi n'est pas valable pour un paiement ou un transfert" });

                if (string.IsNullOrWhiteSpace(alias_connecte))
                    invalidParams.Add(new InvalidParam { name = "aliasPayeur", reason = "L'alias du payeur est requis" });

                // coordonnées obligatoires si canal ≠ 300/999
                if (data_transfert.canal != "300" && data_transfert.canal != "999")
                {
                    if (string.IsNullOrWhiteSpace(data_transfert.longitude) || string.IsNullOrWhiteSpace(data_transfert.latitude))
                        invalidParams.Add(new InvalidParam { name = "coordonnees", reason = "Les coordonnées (latitude/longitude) sont obligatoires pour ce canal" });
                }

                if (Tools.Tools.canal_BesoinIDTrans(data_transfert.canal) && string.IsNullOrWhiteSpace(data_transfert.txId))
                    invalidParams.Add(new InvalidParam { name = "txId", reason = "Le TxId est requis pour ce canal" });

                if (invalidParams.Count > 0)
                    return new GeneraleRetour { status = 400, detail = "Les données envoyées ne sont pas conformes", invalidParams = invalidParams };

                // -------- Récupération du payeur local
                var data_payeur = await _aliasRepo.SearchAliasByAlias(alias_connecte);
                if (data_payeur == null)
                    return new GeneraleRetour { status = 404, detail = "L'alias du payeur est introuvable dans notre système" };

                // -------- Règle TRAL (plafond)
                if (string.Equals(data_payeur.typeCompte, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    var plafondList = await _datarepo.getDataInListByCode<ItemData>(code_datas.PLAFOND_TRAL.ToString());
                    decimal plafondTral = 0m;

                    if (plafondList != null && plafondList.Count > 0)
                    {
                        // Essaie d’extraire un montant décimal
                        var raw = plafondList[0].Montant?.ToString();
                        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out plafondTral))
                            plafondTral = 0m;
                    }

                    // montant: passe à decimal si possible
                    var montant = Convert.ToDecimal(data_transfert.montant);

                    if (plafondTral > 0m && montant > plafondTral)
                    {
                        var lib = $"Un client avec un compte de type TRAL (compte non identifié) ne peut pas envoyer ou recevoir des transferts dont le montant dépasse {plafondTral:N0} {_aipdata.devise}.";
                        return new GeneraleRetour { status = 403, detail = lib };
                    }
                }


                // -------- Détermination du bénéficiaire (paye) : identité vs alias
                DataPayeDto data_paye = null;

                var typeInitiation = Tools.Tools.DeterminerTypeInitiation(data_transfert.alias, data_transfert.iban, data_transfert.othr);

                if (typeInitiation == Type_initie.iban || typeInitiation == Type_initie.other)
                {
                    var res_rech_ID = await VerificationIdentite(data_transfert.payePSP, data_transfert.iban, data_transfert.othr, iddemande);
                    if (!Tools.Tools.RetourIsSucces(res_rech_ID.status))
                        return new GeneraleRetour { status = res_rech_ID.status, detail = res_rech_ID.detail };

                    data_paye = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_ID.data ?? string.Empty);
                    if (data_paye == null)
                        return new GeneraleRetour { status = 502, detail = "Réponse d'identification bénéficiaire invalide" };
                }
                else if (typeInitiation == Type_initie.alias)
                {
                    var res_rech_alias = await RechercheAlias(data_transfert.alias, _aipdata.enabledInternalTransfer, bDemandePaiement: true);
                    if (!Tools.Tools.RetourIsSucces(res_rech_alias.status))
                        return new GeneraleRetour { status = res_rech_alias.status, detail = res_rech_alias.detail };

                    data_paye = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_alias.data ?? string.Empty);
                    if (data_paye == null)
                        return new GeneraleRetour { status = 502, detail = "Réponse de recherche d'alias invalide" };
                }
                else
                {
                    return new GeneraleRetour { status = 400, detail = "Type d'initiation inconnu (alias/iban/other requis)" };
                }

                // -------- Vérification de l’institution bénéficiaire
                var p = await _participantRepo.searchParticipant(data_paye.participant);
                if (p == null || string.Equals(p.statut, "DLTD", StringComparison.OrdinalIgnoreCase))
                    return new GeneraleRetour { status = 404, detail = "Institution du bénéficiaire inconnue dans PI" };

                if (string.Equals(p.statut, "DSBL", StringComparison.OrdinalIgnoreCase))
                    return new GeneraleRetour { status = 403, detail = "Institution du bénéficiaire momentanément indisponible" };


                // -------- Determination des frais du transfert 

                DeterminerFraisDto d = new DeterminerFraisDto
                {
                    Montant = Convert.ToDecimal(data_transfert.montant),
                    CategoriePayeurCode = data_payeur.categorie,
                    CategoriePayeCode = data_paye.type,
                    PaysPayeCode = data_paye.paysResidence,
                    PaysPayeurCode = data_payeur.paysResidence,
                };



                double montantfrais = await CalculerLesFrais(d);

                // -------- Construction du transfert (mapping)
                var new_transfert = new t_transfert
                {
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    montant = Convert.ToDouble(data_transfert.montant), // garde double si ton modèle l’impose
                    motif = data_transfert.motif,
                    canalCommunication = data_transfert.canal,
                    montantFrais = montantfrais,

                    // Payeur
                    compteClientPayeur = string.IsNullOrWhiteSpace(data_payeur.iban) ? data_payeur.other : data_payeur.iban,
                    dateHeureAcceptation = DateTime.Now,
                    identifiantTransaction = data_transfert.txId,
                    typeDocumentReference = data_transfert.typeDocumentReference,
                    numeroDocumentReference = data_transfert.numeroDocumentReference,
                    latitudeClientPayeur = data_transfert.latitude,
                    longitudeClientPayeur = data_transfert.longitude,
                    referenceBulk = data_transfert.referenceBulk,
                    typeTransaction = data_transfert.typeTransaction,
                    codeMembreParticipantPayeur = data_payeur.participant,
                    aliasClientPayeur = data_payeur.valeurAlias,
                    nomClientPayeur = data_payeur.nomClient,
                    dateNaissanceClientPayeur = data_payeur.dateNaissance,
                    typeCompteClientPayeur = data_payeur.typeCompte,
                    villeClientPayeur = data_payeur.ville,
                    villeNaissanceClientPayeur = data_payeur.villeNaissance,
                    adresseClientPayeur = data_payeur.adresse,
                    ibanClientPayeur = data_payeur.iban,
                    otherClientPayeur = data_payeur.other,
                    paysClientPayeur = data_payeur.paysResidence,
                    paysNaissanceClientPayeur = data_payeur.PaysNaissance,
                    deviseCompteClientPayeur =  _aipdata.devise ?? "XOF",
                    typeClientPayeur = data_payeur.categorie,
                    sensFlux = sensFlux.SORTANT,

                    // Payé
                    compteClientPaye = string.IsNullOrWhiteSpace(data_paye.iban) ? data_paye.other : data_paye.iban,
                    endToEndId = data_paye.endToEndId,
                    nomClientPaye = data_paye.nom,
                    dateNaissanceClientPaye = data_paye.dateNaissance,
                    typeCompteClientPaye = data_paye.typeCompte,
                    villeClientPaye = data_paye.ville,
                    villeNaissanceClientPaye = data_paye.villeNaissance,
                    adresseClientPaye = data_paye.adresseComplete,
                    ibanClientPaye = data_paye.iban,
                    otherClientPaye = data_paye.other,
                    paysClientPaye = data_paye.paysResidence,
                    paysNaissanceClientPaye = data_paye.paysNaissance,
                    codeMembreParticipantPaye = data_paye.participant,
                    typeClientPaye = data_paye.type,
                    deviseCompteClientPaye = data_paye.devise ?? _aipdata.devise ?? "XOF",

                    // Statuts initiaux
                    statut_general = STATUT_TRANSFERT.initie,
                    etape = ETAPE_TRANSFERT.INITIEE
                };

                if (new_transfert.typeClientPayeur == "C")
                    new_transfert.numeroRCCMClientPayeur = data_payeur.identificationRccm;

                if (!string.Equals(new_transfert.typeCompteClientPayeur, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    var (sysId, numId) = Tools.Tools.TraiterNumPiece(
                        new_transfert.typeClientPayeur,
                        data_payeur.identificationNationaleClient,
                        data_payeur.numeroPasseport,
                        data_payeur.identificationFiscale);
                    new_transfert.systemeIdentificationClientPayeur = sysId;
                    new_transfert.numeroIdentificationClientPayeur = numId;
                }

                if (!string.Equals(new_transfert.typeCompteClientPaye, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    new_transfert.systemeIdentificationClientPaye = data_paye.systemeIdentification;
                    new_transfert.numeroIdentificationClientPaye = data_paye.numeroIdentification;
                }

                if (new_transfert.typeClientPaye == "C")
                    new_transfert.numeroRCCMClientPaye = data_paye.numeroRCCM;

                if (typeInitiation == Type_initie.alias)
                    new_transfert.aliasClientPaye = data_paye.alias;

                // -------- Règles d’autorisation (canal/catégories) & plafonds
                var autorise = await _transfertAutorepo.AvoirAutorisations(data_payeur.categorie,data_paye.type, data_transfert.canal);
                if (!autorise)
                {
                    new_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    new_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    new_transfert.motifRejet = $"Opération non autorisée de {data_payeur.categorie} vers {data_paye.type} via canal {data_transfert.canal}.";
                    await _transfertRepo.AddAsync(new_transfert);
                    return new GeneraleRetour { status = 403, detail = "Transaction non autorisée" };
                }

                var plafondOK = await _transfertPlafondrepo.VerifiePlafond(data_payeur.categorie, data_paye.type, new_transfert.montant);
                if (!plafondOK)
                {
                    const string lib = "Le montant dépasse le plafond autorisé.";
                    new_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    new_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    new_transfert.motifRejet = lib;
                    await _transfertRepo.AddAsync(new_transfert);
                    return new GeneraleRetour { status = 403, detail = lib };
                }

                // -------- Persistance & retour
                await _transfertRepo.AddAsync(new_transfert);
                return new GeneraleRetour { status = 201, detail = "Transfert initié", data = JsonConvert.SerializeObject(new_transfert) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} - Erreur inattendue", _script);
                return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
            }
        }




        public async Task<GeneraleRetour> InitierTransfertOrPaiement(t_alias payeurAliasData, QueryBodyInitierPaiementOrTransfertAIFDto transfertData, string iddemande)
        {
            const string _script = "SERVICE D'INITIATION DU TRANSFERT";

            try
            {

             
              
                // -------- Règle TRAL (plafond)
                if (string.Equals(payeurAliasData.typeCompte, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    var plafondList = await _datarepo.getDataInListByCode<ItemData>(code_datas.PLAFOND_TRAL.ToString());
                    decimal plafondTral = 0m;

                    if (plafondList != null && plafondList.Count > 0)
                    {
                        // Essaie d’extraire un montant décimal
                        var raw = plafondList[0].Montant?.ToString();
                        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out plafondTral))
                            plafondTral = 0m;
                    }

                    // montant: passe à decimal si possible
                    var montant = Convert.ToDecimal(transfertData.montant);

                    if (plafondTral > 0m && montant > plafondTral)
                    {
                        var lib = $"Un client avec un compte de type TRAL (compte non identifié) ne peut pas envoyer ou recevoir des transferts dont le montant dépasse {plafondTral:N0} {_aipdata.devise}.";
                        return new GeneraleRetour { status = 403, detail = lib };
                    }
                }


                // -------- Détermination du bénéficiaire (paye) : identité vs alias
                DataPayeDto payeData = null;

                var typeInitiation = Tools.Tools.DeterminerTypeInitiation(transfertData.payeAlias, transfertData.payeIban, transfertData.payeOthr);

                if (typeInitiation == Type_initie.iban || typeInitiation == Type_initie.other)
                {
                    var res_rech_ID = await VerificationIdentite(transfertData.payePSP, transfertData.payeIban, transfertData.payeOthr, iddemande);
                    if (!Tools.Tools.RetourIsSucces(res_rech_ID.status))
                        return new GeneraleRetour { status = res_rech_ID.status, detail = res_rech_ID.detail };

                    payeData = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_ID.data ?? string.Empty);
                    if (payeData == null)
                        return new GeneraleRetour { status = 502, detail = "Réponse d'identification bénéficiaire invalide" };
                }
                else if (typeInitiation == Type_initie.alias)
                {
                    var res_rech_alias = await RechercheAlias(transfertData.payeAlias, _aipdata.enabledInternalTransfer, bDemandePaiement: false);
                    if (!Tools.Tools.RetourIsSucces(res_rech_alias.status))
                        return new GeneraleRetour { status = res_rech_alias.status, detail = res_rech_alias.detail };

                    payeData = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_alias.data ?? string.Empty);
                    if (payeData == null)
                        return new GeneraleRetour { status = 502, detail = "Réponse de recherche d'alias invalide" };
                }
                else
                {
                    return new GeneraleRetour { status = 400, detail = "Type d'initiation inconnu (alias/iban/other requis)" };
                }

                // -------- Vérification de l’institution bénéficiaire
                var p = await _participantRepo.searchParticipant(payeData.participant);
                if (p == null || string.Equals(p.statut, "DLTD", StringComparison.OrdinalIgnoreCase))
                    return new GeneraleRetour { status = 404, detail = "Institution du bénéficiaire inconnue dans PI" };

                if (string.Equals(p.statut, "DSBL", StringComparison.OrdinalIgnoreCase))
                    return new GeneraleRetour { status = 403, detail = "Institution du bénéficiaire momentanément indisponible" };


                // -------- Determination des frais du transfert 

                DeterminerFraisDto d = new DeterminerFraisDto
                {
                    Montant = Convert.ToDecimal(transfertData.montant),
                    CategoriePayeurCode = payeurAliasData.categorie,
                    CategoriePayeCode = payeData.type,
                    PaysPayeCode = payeData.paysResidence,
                    PaysPayeurCode = payeurAliasData.paysResidence,
                };



                double montantfrais = await CalculerLesFrais(d);

                // -------- Construction du transfert (mapping)
                var new_transfert = new t_transfert
                {
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    montant = Convert.ToDouble(transfertData.montant), // garde double si ton modèle l’impose
                    motif = transfertData.motif,
                    canalCommunication = transfertData.canal,
                    montantFrais = montantfrais,

                    // Payeur
                    compteClientPayeur = string.IsNullOrWhiteSpace(payeurAliasData.iban) ? payeurAliasData.other : payeurAliasData.iban,
                    dateHeureAcceptation = DateTime.Now,
                    identifiantTransaction = transfertData.txId,
                    typeDocumentReference = transfertData.typeDocumentReference,
                    numeroDocumentReference = transfertData.numeroDocumentReference,
                    latitudeClientPayeur = transfertData.latitude,
                    longitudeClientPayeur = transfertData.longitude,
                    referenceBulk = transfertData.referenceBulk,
                    typeTransaction = transfertData.typeTransaction,
                    codeMembreParticipantPayeur = payeurAliasData.participant,
                    aliasClientPayeur = payeurAliasData.valeurAlias,
                    nomClientPayeur = payeurAliasData.nomClient,
                    dateNaissanceClientPayeur = payeurAliasData.dateNaissance,
                    typeCompteClientPayeur = payeurAliasData.typeCompte,
                    villeClientPayeur = payeurAliasData.ville,
                    villeNaissanceClientPayeur = payeurAliasData.villeNaissance,
                    adresseClientPayeur = payeurAliasData.adresse,
                    ibanClientPayeur = payeurAliasData.iban,
                    otherClientPayeur = payeurAliasData.other,
                    paysClientPayeur = payeurAliasData.paysResidence,
                    paysNaissanceClientPayeur = payeurAliasData.PaysNaissance,
                    deviseCompteClientPayeur = _aipdata.devise ?? "XOF",
                    typeClientPayeur = payeurAliasData.categorie,
                    sensFlux = sensFlux.SORTANT,

                    // Payé
                    compteClientPaye = string.IsNullOrWhiteSpace(payeData.iban) ? payeData.other : payeData.iban,
                    endToEndId = payeData.endToEndId,
                    nomClientPaye = payeData.nom,
                    dateNaissanceClientPaye = payeData.dateNaissance,
                    typeCompteClientPaye = payeData.typeCompte,
                    villeClientPaye = payeData.ville,
                    villeNaissanceClientPaye = payeData.villeNaissance,
                    adresseClientPaye = payeData.adresseComplete,
                    ibanClientPaye = payeData.iban,
                    otherClientPaye = payeData.other,
                    paysClientPaye = payeData.paysResidence,
                    paysNaissanceClientPaye = payeData.paysNaissance,
                    codeMembreParticipantPaye = payeData.participant,
                    typeClientPaye = payeData.type,
                    deviseCompteClientPaye = payeData.devise ?? _aipdata.devise ?? "XOF",

                    // Statuts initiaux
                    statut_general = STATUT_TRANSFERT.initie,
                    etape = ETAPE_TRANSFERT.INITIEE
                };

                if (new_transfert.typeClientPayeur == "C")
                    new_transfert.numeroRCCMClientPayeur = payeurAliasData.identificationRccm;

                if (!string.Equals(new_transfert.typeCompteClientPayeur, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    var (sysId, numId) = Tools.Tools.TraiterNumPiece(
                        new_transfert.typeClientPayeur,
                        payeurAliasData.identificationNationaleClient,
                        payeurAliasData.numeroPasseport,
                        payeurAliasData.identificationFiscale);
                    new_transfert.systemeIdentificationClientPayeur = sysId;
                    new_transfert.numeroIdentificationClientPayeur = numId;
                }

                if (!string.Equals(new_transfert.typeCompteClientPaye, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    new_transfert.systemeIdentificationClientPaye = payeData.systemeIdentification;
                    new_transfert.numeroIdentificationClientPaye = payeData.numeroIdentification;
                }

                if (new_transfert.typeClientPaye == "C")
                    new_transfert.numeroRCCMClientPaye = payeData.numeroRCCM;

                if (typeInitiation == Type_initie.alias)
                    new_transfert.aliasClientPaye = payeData.alias;

                // -------- Règles d’autorisation (canal/catégories) & plafonds
                var autorise = await _transfertAutorepo.AvoirAutorisations(payeurAliasData.categorie, payeData.type, transfertData.canal);
                if (!autorise)
                {
                    new_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    new_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    new_transfert.motifRejet = $"Opération non autorisée de {payeurAliasData.categorie} vers {payeData.type} via canal {transfertData.canal}.";
                    await _transfertRepo.AddAsync(new_transfert);
                    return new GeneraleRetour { status = 403, detail = "Transaction non autorisée" };
                }

                var plafondOK = await _transfertPlafondrepo.VerifiePlafond(payeurAliasData.categorie, payeData.type, new_transfert.montant);
                if (!plafondOK)
                {
                    const string lib = "Le montant dépasse le plafond autorisé.";
                    new_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    new_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    new_transfert.motifRejet = lib;
                    await _transfertRepo.AddAsync(new_transfert);
                    return new GeneraleRetour { status = 403, detail = lib };
                }

                // -------- Persistance 
                await _transfertRepo.AddAsync(new_transfert);


                // -------- Retour 
                if (transfertData.confirmation == true)
                    return new GeneraleRetour { status = 201, detail = "Transfert initié", data = JsonConvert.SerializeObject(new_transfert) };
                else
                    return await ConfirmerTransfert(new_transfert,iddemande);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} - Erreur inattendue", _script);
                return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
            }
        }



        //public async Task<GeneraleRetour> InitierDemandePaiement( QueryBodyDemandePaiementAIFDto rtpData, string iddemande)
        //{
        //    const string _script = "SERVICE D'INITIATION DE DEMANDE DE PAIEMENT";

        //    try
        //    {
        //        // ---- Validations d'entrée
              
                
              
        //        // ---- Données DEMANDEUR (payé)
        //        var data_demandeur = await _aliasRepo.SearchAliasByAlias(alias_demandeur);
        //        if (data_demandeur == null)
        //            return new GeneraleRetour { status = 404, detail = "L'alias payé est introuvable dans notre système" };

        //        // ---- Données PAYEUR (via recherche d’alias)
        //        var res_rech_alias = await RechercheAlias(alias_payeur, _aipdata.enabledInternalTransfer, bDemandePaiement: true);
        //        if (!Tools.Tools.RetourIsSucces(res_rech_alias.status))
        //            return new GeneraleRetour { status = res_rech_alias.status, detail = res_rech_alias.detail };

        //        var data_payeur = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_alias.data);
        //        if (data_payeur == null)
        //            return new GeneraleRetour { status = 500, detail = "Erreur de lecture des données du payeur" };

        //        // ---- Vérif institution du payeur si interopérable (diff. membre PI)
        //        bool transfertInternePossible = _aipdata.enabledInternalTransfer &&
        //                                        string.Equals(data_payeur.participant, _aipdata.codemembre, StringComparison.OrdinalIgnoreCase);

        //        if (!transfertInternePossible)
        //        {
        //            var p = await _participantRepo.searchParticipant(data_payeur.participant);
        //            if (p == null || p.statut == "DLTD")
        //                return new GeneraleRetour { status = 404, detail = "Institution du payeur inconnue dans PI" };
        //            if (p.statut == "DSBL")
        //                return new GeneraleRetour { status = 409, detail = "Institution du payeur momentanément indisponible" };
        //        }


        //        // ---- Construction du RTP (transfert)
        //        var new_rtp = new t_transfert
        //        {
        //            identifiantTransaction = Tools.Tools.GenerateAlphaNumeriquevalue(35),
        //            msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
        //            // Utilise decimal → double pour compat avec ton modèle
        //            montant = (double)Convert.ToDecimal(rtpData.montant),
        //            motif = rtpData.motif,
        //            canalCommunication = rtpData.canal,
        //            latitudeClientPaye = rtpData.latitude,
        //            longitudeClientPaye = rtpData.longitude,
        //            identifiantMandat = rtpData.identifiantMandat,
        //            signatureNumeriqueMandat = rtpData.signatureNumeriqueMandat,
        //            tauxRemisePaiementImmediat = rtpData.tauxRemisePaiementImmediat,
        //            autorisationModificationMontant = rtpData.autorisationModificationMontant,
        //            montantRemisePaiementImmediat = rtpData.montantRemisePaiementImmediat,
        //            typeDocumentReference = rtpData.typeDocumentReference,
        //            numeroDocumentReference = rtpData.numeroDocumentReference,
        //            montantAchat = rtpData.montantAchat,
        //            montantRetrait = rtpData.montantRetrait,
        //            fraisRetrait = rtpData.fraisRetrait,
        //            dateLimiteAction = rtpData.dateHeureLimiteAction,
        //            dateHeureExecution = rtpData.dateHeureExecution,

        //            // DEMANDEUR (payé)
        //            codeMembreParticipantPaye = data_demandeur.participant,
        //            aliasClientPaye = data_demandeur.valeurAlias,
        //            nomClientPaye = data_demandeur.nomClient,
        //            dateNaissanceClientPaye = data_demandeur.dateNaissance,
        //            typeCompteClientPaye = data_demandeur.typeCompte,
        //            villeClientPaye = data_demandeur.ville,
        //            villeNaissanceClientPaye = data_demandeur.villeNaissance,
        //            adresseClientPaye = data_demandeur.adresse,
        //            ibanClientPaye = data_demandeur.iban,
        //            otherClientPaye = data_demandeur.other,
        //            paysClientPaye = data_demandeur.paysResidence,
        //            paysNaissanceClientPaye = data_demandeur.PaysNaissance,
        //            deviseCompteClientPaye = string.IsNullOrWhiteSpace(_aipdata.devise) ? "XOF" : _aipdata.devise,
        //            typeClientPaye = data_demandeur.categorie,
        //            sensFlux = sensFlux.SORTANT,

        //            // PAYEUR
        //            endToEndId = data_payeur.endToEndId,
        //            nomClientPayeur = data_payeur.nom,
        //            dateNaissanceClientPayeur = data_payeur.dateNaissance,
        //            typeCompteClientPayeur = data_payeur.typeCompte,
        //            villeClientPayeur = data_payeur.ville,
        //            villeNaissanceClientPayeur = data_payeur.villeNaissance,
        //            adresseClientPayeur = data_payeur.adresseComplete,
        //            ibanClientPayeur = data_payeur.iban,
        //            otherClientPayeur = data_payeur.other,
        //            paysClientPayeur = data_payeur.paysResidence,
        //            paysNaissanceClientPayeur = data_payeur.paysNaissance,
        //            codeMembreParticipantPayeur = data_payeur.participant,
        //            typeClientPayeur = data_payeur.type,
        //            deviseCompteClientPayeur = string.IsNullOrWhiteSpace(data_payeur.devise) ? _aipdata.devise : data_payeur.devise,

        //            etape = ETAPE_TRANSFERT.INITIEE,
        //            statut_general = STATUT_TRANSFERT.initie
        //        };

        //        // Fallbacks compte (iban → other)
        //        new_rtp.compteClientPaye = !string.IsNullOrWhiteSpace(data_demandeur.iban) ? data_demandeur.iban : data_demandeur.other;
        //        new_rtp.compteClientPayeur = !string.IsNullOrWhiteSpace(data_payeur.iban) ? data_payeur.iban : data_payeur.other;

        //        // RCCM entreprise si C
        //        if (new_rtp.typeClientPaye == "C")
        //            new_rtp.numeroRCCMClientPaye = data_demandeur.identificationRccm;
        //        if (new_rtp.typeClientPayeur == "C")
        //            new_rtp.numeroRCCMClientPayeur = data_payeur.numeroRCCM;

        //        // Identité (non requise pour TRAL)
        //        if (!string.Equals(new_rtp.typeCompteClientPaye, "TRAL", StringComparison.OrdinalIgnoreCase))
        //        {
        //            var idPaye = Tools.Tools.TraiterNumPiece(new_rtp.typeClientPaye,
        //                                                     data_demandeur.identificationNationaleClient,
        //                                                     data_demandeur.numeroPasseport,
        //                                                     data_demandeur.identificationFiscale);


        //            new_rtp.systemeIdentificationClientPaye = idPaye.Item1;
        //            new_rtp.numeroIdentificationClientPaye = idPaye.Item2;
        //        }

        //        if (!string.Equals(new_rtp.typeCompteClientPayeur, "TRAL", StringComparison.OrdinalIgnoreCase))
        //        {
        //            new_rtp.systemeIdentificationClientPayeur = data_payeur.systemeIdentification;
        //            new_rtp.numeroIdentificationClientPayeur = data_payeur.numeroIdentification;
        //        }

        //        // Alias payeur si l’initiation est bien par alias
        //        if (Tools.Tools.DeterminerTypeInitiation(data_payeur.alias, data_payeur.iban, data_payeur.other) == Type_initie.alias)
        //            new_rtp.aliasClientPayeur = data_payeur.alias;

        //        // ---- Règles d’autorisation & plafonds
        //        bool autorise = await _transfertAutorepo.AvoirAutorisations(data_payeur.type, data_demandeur.categorie, new_rtp.canalCommunication);
        //        if (!autorise)
        //        {
        //            new_rtp.statut_general = STATUT_TRANSFERT.rejete;
        //            new_rtp.etape = ETAPE_TRANSFERT.REJETE;
        //            new_rtp.motifRejet = $"Demande de paiement non autorisée de {new_rtp.typeClientPaye} vers {new_rtp.typeClientPayeur} avec canal {new_rtp.canalCommunication}";
        //            await _transfertRepo.AddAsync(new_rtp);
        //            return new GeneraleRetour { status = 400, detail = new_rtp.motifRejet };
        //        }

        //        bool plafondOk = await _transfertPlafondrepo.VerifiePlafond(new_rtp.typeClientPayeur, new_rtp.typeClientPaye, new_rtp.montant);
        //        if (!plafondOk)
        //        {
        //            const string lib = "Le montant dépasse le plafond autorisé.";
        //            new_rtp.statut_general = STATUT_TRANSFERT.rejete;
        //            new_rtp.etape = ETAPE_TRANSFERT.REJETE;
        //            new_rtp.motifRejet = lib;
        //            await _transfertRepo.AddAsync(new_rtp);
        //            return new GeneraleRetour { status = 400, detail = lib };
        //        }

        //        // ---- Persist & retour
        //        await _transfertRepo.AddAsync(new_rtp);
        //        return new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(new_rtp) };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "{Script} ====> Erreur", _script);
        //        return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
        //    }
        //}





        public async Task<GeneraleRetour> InitierTransfertDeDisponibilite(string alias_connecte, QueryBodyTransactionDispoDto data_transfert, string iddemande)
        {
            string _script = "SERVICE D'INTIATION DU TRANSFERT PING";

            try
            {

                _logger.LogInformation($"{_script}... DEBUT");

                List<InvalidParam> invalidParams = new List<InvalidParam>();

                var validator = new QueryBodyTransactionDispoDtoValidator();
                var results = validator.Validate(data_transfert);

                if (!results.IsValid)
                {
                    invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();
                }



                if (string.IsNullOrEmpty(alias_connecte))
                    invalidParams.Add(new InvalidParam { name = "type", reason = "Alias du payeur est requis" });


                if (invalidParams.Count > 0)
                    return (new GeneraleRetour { status = 400, detail = "Les données envoyées ne sont pas conformes", invalidParams = invalidParams });


                ///// Données client PAYEUR
                t_alias data_payeur = await _aliasRepo.SearchAliasByAlias(alias_connecte);
                if (data_payeur == null)
                    return (new GeneraleRetour { status = 404, detail = "L'alias du payeur est introuvable dans notre système" });


                DataPayeDto data_paye = new DataPayeDto();


                Type_initie? typeInitiation = Tools.Tools.DeterminerTypeInitiation(data_transfert.alias, data_transfert.iban, data_transfert.othr);

                // Cas Vérification d'identité
                if ((typeInitiation == Type_initie.iban || typeInitiation == Type_initie.other))
                {
                    GeneraleRetour res_rech_ID = await VerificationIdentite(data_transfert.payePSP, data_transfert.iban, data_transfert.othr, iddemande);
                    if (!Tools.Tools.RetourIsSucces(res_rech_ID.status))
                        return (new GeneraleRetour { status = res_rech_ID.status, detail = res_rech_ID.detail });

                    data_paye = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_ID.data);
                }

                // Cas Recherche d'alias
                if (typeInitiation == Type_initie.alias)
                {
                    GeneraleRetour res_rech_alias = await RechercheAlias(data_transfert.alias, false);

                    if (!Tools.Tools.RetourIsSucces(res_rech_alias.status))
                        return (new GeneraleRetour { status = res_rech_alias.status, detail = res_rech_alias.detail });

                    data_paye = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_alias.data);

                }

                _logger.LogInformation($"Prepapration au transfere   =======================> avec message ");

                /// Initiation du transfert
                t_transfert_dispo new_transfert = new t_transfert_dispo();

                new_transfert.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                new_transfert.montant = (double)data_transfert.montant;
                new_transfert.motif = data_transfert.motif;
                new_transfert.canalCommunication = "999";
                new_transfert.dateHeureAcceptation = DateTime.Now;
                //System.Threading.Thread.Sleep(4000);
                new_transfert.latitudeClientPayeur = data_transfert.latitude;
                new_transfert.longitudeClientPayeur = data_transfert.longitude;
                new_transfert.typeTransaction = "DISP";
                new_transfert.codeMembreParticipantPayeur = data_payeur.participant;
                new_transfert.aliasClientPayeur = data_payeur.valeurAlias;
                new_transfert.nomClientPayeur = data_payeur.nomClient;
                new_transfert.dateNaissanceClientPayeur = data_payeur.dateNaissance;
                new_transfert.typeCompteClientPayeur = data_payeur.typeCompte;
                new_transfert.villeClientPayeur = data_payeur.ville;
                new_transfert.villeNaissanceClientPayeur = data_payeur.villeNaissance;
                new_transfert.adresseClientPayeur = data_payeur.adresse;
                new_transfert.ibanClientPayeur = data_payeur.iban;
                new_transfert.otherClientPayeur = data_payeur.other;
                new_transfert.paysClientPayeur = data_payeur.paysResidence;
                new_transfert.paysNaissanceClientPayeur = data_payeur.PaysNaissance;
                new_transfert.deviseCompteClientPayeur = _aipdata.devise;
                new_transfert.typeClientPayeur = data_payeur.categorie;
                new_transfert.sensFlux = sensFlux.SORTANT;

                new_transfert.compteClientPayeur = data_payeur.iban;
                if (string.IsNullOrEmpty(new_transfert.compteClientPayeur))
                    new_transfert.compteClientPayeur = data_payeur.other;

                new_transfert.compteClientPaye = data_paye.iban;
                if (string.IsNullOrEmpty(new_transfert.compteClientPaye))
                    new_transfert.compteClientPaye = data_paye.other;



                if (new_transfert.typeClientPayeur == "C")
                    new_transfert.numeroRCCMClientPayeur = data_payeur.identificationRccm;

                // Parametre non obligatoire pour TRAL
                if (new_transfert.typeCompteClientPayeur != "TRAL")
                {
                    var ret_piece = Tools.Tools.TraiterNumPiece(new_transfert.typeClientPayeur, data_payeur.identificationNationaleClient, data_payeur.numeroPasseport, data_payeur.identificationFiscale);
                    new_transfert.systemeIdentificationClientPayeur = ret_piece.Item1;
                    new_transfert.numeroIdentificationClientPayeur = ret_piece.Item2;
                }

                ///// FIN Données client PAYEUR

                ///// Données client PAYE

                new_transfert.endToEndId = data_paye.endToEndId;
                new_transfert.nomClientPaye = data_paye.nom;
                new_transfert.dateNaissanceClientPaye = data_paye.dateNaissance;
                new_transfert.typeCompteClientPaye = data_paye.typeCompte;
                new_transfert.villeClientPaye = data_paye.ville;
                new_transfert.villeNaissanceClientPaye = data_paye.villeNaissance;
                new_transfert.adresseClientPaye = data_paye.adresseComplete;
                new_transfert.ibanClientPaye = data_paye.iban;
                new_transfert.otherClientPaye = data_paye.other;
                new_transfert.paysClientPaye = data_paye.paysResidence;
                new_transfert.paysNaissanceClientPaye = data_paye.paysNaissance;
                new_transfert.codeMembreParticipantPaye = data_paye.participant;
                new_transfert.typeClientPaye = data_paye.type;

                new_transfert.deviseCompteClientPaye = (string.IsNullOrEmpty(data_paye.devise)) ? _aipdata.devise : data_paye.devise;

                if (new_transfert.typeCompteClientPaye != "TRAL")
                {
                    new_transfert.systemeIdentificationClientPaye = data_paye.systemeIdentification;
                    new_transfert.numeroIdentificationClientPaye = data_paye.numeroIdentification;
                }

                if (new_transfert.typeClientPaye == "C")
                    new_transfert.numeroRCCMClientPaye = data_paye.numeroRCCM;

                if (typeInitiation == Type_initie.alias)
                    new_transfert.aliasClientPaye = data_paye.alias;

                new_transfert.statut_general = STATUT_TRANSFERT.initie;
                new_transfert.etape = ETAPE_TRANSFERT.INITIEE;

                //// Vérification du canal de transfert
                _logger.LogInformation($"Vérification du canal de transfert  {data_payeur.categorie} vers {data_paye.type} avec canal {new_transfert.canalCommunication}");
                bool b = await _transfertAutorepo.AvoirAutorisations(data_payeur.categorie, data_paye.type,  new_transfert.canalCommunication);
                if (b == false)
                {

                    new_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    new_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    new_transfert.motifRejet = $"Opération non autorisée de  {data_payeur.categorie} vers {data_paye.type} avec canal {new_transfert.canalCommunication}";
                    await _transfertDispoRepo.AddAsync(new_transfert);
                    return (new GeneraleRetour { status = 403, detail = "Transaction non autorisée" });

                }

                await _transfertDispoRepo.AddAsync(new_transfert);
                return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(new_transfert) });


            }
            catch (Exception ex)
            {
                _logger.LogError($"Service Initiation de transfert : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }
       
        public async Task<GeneraleRetour> InitierDemandePaiement( string alias_demandeur,string alias_payeur,QueryBodyTransactionDto data_rtp, string iddemande)
        {
            const string _script = "SERVICE D'INITIATION DE DEMANDE DE PAIEMENT";

            try
            {
                // ---- Validations d'entrée
                if (string.IsNullOrWhiteSpace(alias_demandeur))
                    return new GeneraleRetour { status = 400, detail = "Alias du payé est requis" };

                if (string.IsNullOrWhiteSpace(alias_payeur))
                    return new GeneraleRetour { status = 400, detail = "Alias du payeur est requis" };

                if (data_rtp == null)
                    return new GeneraleRetour { status = 400, detail = "Corps de requête manquant" };

                if (string.IsNullOrWhiteSpace(data_rtp.canal))
                    return new GeneraleRetour { status = 400, detail = "Canal de la demande est requis" };

                if (data_rtp.montant <= 0)
                    return new GeneraleRetour { status = 400, detail = "Montant de la demande n'est pas valide" };

                if (!Tools.Tools.canalEstCanalDemandePaiement(data_rtp.canal))
                    return new GeneraleRetour { status = 400, detail = "Le canal choisi n'est pas valable pour une demande de paiement" };

                if (string.IsNullOrWhiteSpace(data_rtp.longitude) || string.IsNullOrWhiteSpace(data_rtp.latitude))
                    return new GeneraleRetour { status = 400, detail = "Les coordonnées du client payeur sont obligatoires" };

                if (Tools.Tools.canal_BesoinIDTrans(data_rtp.canal) && string.IsNullOrWhiteSpace(data_rtp.txId))
                    return new GeneraleRetour { status = 400, detail = "Le TxId est requis" };

                // ---- Données DEMANDEUR (payé)
                var data_demandeur = await _aliasRepo.SearchAliasByAlias(alias_demandeur);
                if (data_demandeur == null)
                    return new GeneraleRetour { status = 404, detail = "L'alias payé est introuvable dans notre système" };

                // ---- Données PAYEUR (via recherche d’alias)
                var res_rech_alias = await RechercheAlias(alias_payeur, _aipdata.enabledInternalTransfer, bDemandePaiement: true);
                if (!Tools.Tools.RetourIsSucces(res_rech_alias.status))
                    return new GeneraleRetour { status = res_rech_alias.status, detail = res_rech_alias.detail };

                var data_payeur = JsonConvert.DeserializeObject<DataPayeDto>(res_rech_alias.data);
                if (data_payeur == null)
                    return new GeneraleRetour { status = 500, detail = "Erreur de lecture des données du payeur" };

                // ---- Vérif institution du payeur si interopérable (diff. membre PI)
                bool transfertInternePossible = _aipdata.enabledInternalTransfer &&
                                                string.Equals(data_payeur.participant, _aipdata.codemembre, StringComparison.OrdinalIgnoreCase);

                if (!transfertInternePossible)
                {
                    var p = await _participantRepo.searchParticipant(data_payeur.participant);
                    if (p == null || p.statut == "DLTD")
                        return new GeneraleRetour { status = 404, detail = "Institution du payeur inconnue dans PI" };
                    if (p.statut == "DSBL")
                        return new GeneraleRetour { status = 409, detail = "Institution du payeur momentanément indisponible" };
                }


                // ---- Construction du RTP (transfert)
                var new_rtp = new t_transfert
                {
                    identifiantTransaction = Tools.Tools.GenerateAlphaNumeriquevalue(35),
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    // Utilise decimal → double pour compat avec ton modèle
                    montant = (double)Convert.ToDecimal(data_rtp.montant),
                    motif = data_rtp.motif,
                    canalCommunication = data_rtp.canal,
                    latitudeClientPaye = data_rtp.latitude,
                    longitudeClientPaye = data_rtp.longitude,
                    identifiantMandat = data_rtp.identifiantMandat,
                    signatureNumeriqueMandat = data_rtp.signatureNumeriqueMandat,
                    tauxRemisePaiementImmediat = data_rtp.tauxRemisePaiementImmediat,
                    autorisationModificationMontant = data_rtp.autorisationModificationMontant,
                    montantRemisePaiementImmediat = data_rtp.montantRemisePaiementImmediat,
                    typeDocumentReference = data_rtp.typeDocumentReference,
                    numeroDocumentReference = data_rtp.numeroDocumentReference,
                    montantAchat = data_rtp.montantAchat,
                    montantRetrait = data_rtp.montantRetrait,
                    fraisRetrait = data_rtp.fraisRetrait,
                    dateLimiteAction = data_rtp.dateHeureLimiteAction,
                    dateHeureExecution = data_rtp.dateHeureExecution,

                    // DEMANDEUR (payé)
                    codeMembreParticipantPaye = data_demandeur.participant,
                    aliasClientPaye = data_demandeur.valeurAlias,
                    nomClientPaye = data_demandeur.nomClient,
                    dateNaissanceClientPaye = data_demandeur.dateNaissance,
                    typeCompteClientPaye = data_demandeur.typeCompte,
                    villeClientPaye = data_demandeur.ville,
                    villeNaissanceClientPaye = data_demandeur.villeNaissance,
                    adresseClientPaye = data_demandeur.adresse,
                    ibanClientPaye = data_demandeur.iban,
                    otherClientPaye = data_demandeur.other,
                    paysClientPaye = data_demandeur.paysResidence,
                    paysNaissanceClientPaye = data_demandeur.PaysNaissance,
                    deviseCompteClientPaye = string.IsNullOrWhiteSpace(_aipdata.devise) ? "XOF" : _aipdata.devise,
                    typeClientPaye = data_demandeur.categorie,
                    sensFlux = sensFlux.SORTANT,

                    // PAYEUR
                    endToEndId = data_payeur.endToEndId,
                    nomClientPayeur = data_payeur.nom,
                    dateNaissanceClientPayeur = data_payeur.dateNaissance,
                    typeCompteClientPayeur = data_payeur.typeCompte,
                    villeClientPayeur = data_payeur.ville,
                    villeNaissanceClientPayeur = data_payeur.villeNaissance,
                    adresseClientPayeur = data_payeur.adresseComplete,
                    ibanClientPayeur = data_payeur.iban,
                    otherClientPayeur = data_payeur.other,
                    paysClientPayeur = data_payeur.paysResidence,
                    paysNaissanceClientPayeur = data_payeur.paysNaissance,
                    codeMembreParticipantPayeur = data_payeur.participant,
                    typeClientPayeur = data_payeur.type,
                    deviseCompteClientPayeur = string.IsNullOrWhiteSpace(data_payeur.devise) ? _aipdata.devise : data_payeur.devise,

                    etape = ETAPE_TRANSFERT.INITIEE,
                    statut_general = STATUT_TRANSFERT.initie
                };

                // Fallbacks compte (iban → other)
                new_rtp.compteClientPaye = !string.IsNullOrWhiteSpace(data_demandeur.iban) ? data_demandeur.iban : data_demandeur.other;
                new_rtp.compteClientPayeur = !string.IsNullOrWhiteSpace(data_payeur.iban) ? data_payeur.iban : data_payeur.other;

                // RCCM entreprise si C
                if (new_rtp.typeClientPaye == "C")
                    new_rtp.numeroRCCMClientPaye = data_demandeur.identificationRccm;
                if (new_rtp.typeClientPayeur == "C")
                    new_rtp.numeroRCCMClientPayeur = data_payeur.numeroRCCM;

                // Identité (non requise pour TRAL)
                if (!string.Equals(new_rtp.typeCompteClientPaye, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    var idPaye = Tools.Tools.TraiterNumPiece(new_rtp.typeClientPaye,
                                                             data_demandeur.identificationNationaleClient,
                                                             data_demandeur.numeroPasseport,
                                                             data_demandeur.identificationFiscale);

                   
                    new_rtp.systemeIdentificationClientPaye = idPaye.Item1;
                    new_rtp.numeroIdentificationClientPaye = idPaye.Item2;
                }

                if (!string.Equals(new_rtp.typeCompteClientPayeur, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    new_rtp.systemeIdentificationClientPayeur = data_payeur.systemeIdentification;
                    new_rtp.numeroIdentificationClientPayeur = data_payeur.numeroIdentification;
                }

                // Alias payeur si l’initiation est bien par alias
                if (Tools.Tools.DeterminerTypeInitiation(data_payeur.alias, data_payeur.iban, data_payeur.other) == Type_initie.alias)
                    new_rtp.aliasClientPayeur = data_payeur.alias;

                // ---- Règles d’autorisation & plafonds
                bool autorise = await _transfertAutorepo.AvoirAutorisations(data_payeur.type,data_demandeur.categorie, new_rtp.canalCommunication);
                if (!autorise)
                {
                    new_rtp.statut_general = STATUT_TRANSFERT.rejete;
                    new_rtp.etape = ETAPE_TRANSFERT.REJETE;
                    new_rtp.motifRejet = $"Demande de paiement non autorisée de {new_rtp.typeClientPaye} vers {new_rtp.typeClientPayeur} avec canal {new_rtp.canalCommunication}";
                    await _transfertRepo.AddAsync(new_rtp);
                    return new GeneraleRetour { status = 400, detail = new_rtp.motifRejet };
                }

                bool plafondOk = await _transfertPlafondrepo.VerifiePlafond(new_rtp.typeClientPayeur, new_rtp.typeClientPaye, new_rtp.montant);
                if (!plafondOk)
                {
                    const string lib = "Le montant dépasse le plafond autorisé.";
                    new_rtp.statut_general = STATUT_TRANSFERT.rejete;
                    new_rtp.etape = ETAPE_TRANSFERT.REJETE;
                    new_rtp.motifRejet = lib;
                    await _transfertRepo.AddAsync(new_rtp);
                    return new GeneraleRetour { status = 400, detail = lib };
                }

                // ---- Persist & retour
                await _transfertRepo.AddAsync(new_rtp);
                return new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(new_rtp) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} ====> Erreur", _script);
                return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
            }
        }

        public async Task<GeneraleRetour> DemandeTransfert(t_alias data_payeur, DataPayeDto data_paye, QueryBodyTransactionDto data_transfert, string iddemande)
        {
            string _script = "SERVICE DEMANDE DE TRANSFERT";

            try
            {

                _logger.LogInformation($"{_script}... DEBUT");

                //// Vérification du canal de transfert

                if (string.IsNullOrEmpty(data_transfert.canal))
                    return (new GeneraleRetour { status = 400, detail = "Canal de transfert obligatoire" });

                if (!Tools.Tools.canalEstCanalTransfertPaiement(data_transfert.canal))
                    return (new GeneraleRetour { status = 400, detail = "Le canal choisi n'est pas valable pour un paiement ou un transfert" });


                bool b = await _transfertAutorepo.AvoirAutorisations(data_payeur.categorie, data_paye.type,  data_transfert.canal);
                if (b == false)
                {
                    _logger.LogInformation($"Opération non autorisée de  {data_payeur.categorie} vers {data_paye.type} avec canal {data_transfert.canal}");
                    return (new GeneraleRetour { status = 403, detail = $"Transaction non autorisée" });
                }

                bool bOk = await _transfertPlafondrepo.VerifiePlafond(data_payeur.categorie, data_paye.type, (double)data_transfert.montant);
                if (bOk == false)
                    return (new GeneraleRetour { status = 403, detail = "Le montant dépasse le plafond autorisé." });


                if (data_transfert.canal != "300" && data_transfert.canal != "999") // Alors longitude et latitude obligatoire
                {
                    if (string.IsNullOrEmpty(data_transfert.longitude) || string.IsNullOrEmpty(data_transfert.latitude))
                    {
                        return (new GeneraleRetour { status = 400, detail = "Les coordonnées du client sont obligatoires" });
                    }
                }

                if (Tools.Tools.canal_BesoinIDTrans(data_transfert.canal))
                    if (string.IsNullOrEmpty(data_transfert.txId))
                        return (new GeneraleRetour { status = 400, detail = "Le TxId est requis" });

                // Verification du MsgId et endToEnd

                bool bVerif = await _transfertRepo.EndToEndEstUnique(data_paye.endToEndId);

                if (!bVerif)
                    return (new GeneraleRetour { status = 400, detail = "Les données envoyées ne sont pas conformes" });


                t_transfert new_transfert = new t_transfert {

                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    montant = (double)data_transfert.montant,
                    motif = data_transfert.motif,
                    canalCommunication = data_transfert.canal,
                    dateHeureAcceptation = DateTime.Now,
                    typeDocumentReference = data_transfert.typeDocumentReference,
                    numeroDocumentReference = data_transfert.numeroDocumentReference,
                    latitudeClientPayeur = data_transfert.latitude,
                    longitudeClientPayeur = data_transfert.longitude,
                    identifiantTransaction = data_transfert.txId,
                    statut_general = STATUT_TRANSFERT.initie,
                    etape = ETAPE_TRANSFERT.INITIEE,
                    IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30),
                    sensFlux = sensFlux.INTERNE,

                    codeMembreParticipantPayeur = data_payeur.participant,
                    aliasClientPayeur = data_payeur.valeurAlias,
                    nomClientPayeur = data_payeur.nomClient,
                    dateNaissanceClientPayeur = data_payeur.dateNaissance,
                    typeCompteClientPayeur = data_payeur.typeCompte,
                    villeClientPayeur = data_payeur.ville,
                    villeNaissanceClientPayeur = data_payeur.villeNaissance,
                    adresseClientPayeur = data_payeur.adresse,
                    ibanClientPayeur = data_payeur.iban,
                    otherClientPayeur = data_payeur.other,
                    paysClientPayeur = data_payeur.paysResidence,
                    paysNaissanceClientPayeur = data_payeur.PaysNaissance,
                    deviseCompteClientPayeur = _aipdata.devise,
                    typeClientPayeur = data_payeur.categorie,


                    endToEndId = data_paye.endToEndId,
                    nomClientPaye = data_paye.nom,
                    dateNaissanceClientPaye = data_paye.dateNaissance,
                    typeCompteClientPaye = data_paye.typeCompte,
                    villeClientPaye = data_paye.ville,
                    villeNaissanceClientPaye = data_paye.villeNaissance,
                    adresseClientPaye = data_paye.adresseComplete,
                    ibanClientPaye = data_paye.iban,
                    otherClientPaye = data_paye.other,
                    paysClientPaye = data_paye.paysResidence,
                    paysNaissanceClientPaye = data_paye.paysNaissance,
                    codeMembreParticipantPaye = data_paye.participant,
                    typeClientPaye = data_paye.type,
                    deviseCompteClientPaye = (string.IsNullOrEmpty(data_paye.devise)) ? _aipdata.devise : data_paye.devise,

                };

                if (new_transfert.typeClientPayeur == "C")
                    new_transfert.numeroRCCMClientPayeur = data_payeur.identificationRccm;

                if (new_transfert.typeClientPaye == "C")
                    new_transfert.numeroRCCMClientPaye = data_paye.numeroRCCM;

                // Parametre non obligatoire pour TRAL
                if (new_transfert.typeCompteClientPayeur != "TRAL")
                {
                    var ret_piece = Tools.Tools.TraiterNumPiece(new_transfert.typeClientPayeur, data_payeur.identificationNationaleClient, data_payeur.numeroPasseport, data_payeur.identificationFiscale);
                    new_transfert.systemeIdentificationClientPayeur = ret_piece.Item1;
                    new_transfert.numeroIdentificationClientPayeur = ret_piece.Item2;
                }

                if (new_transfert.typeCompteClientPaye != "TRAL")
                {
                    new_transfert.systemeIdentificationClientPaye = data_paye.systemeIdentification;
                    new_transfert.numeroIdentificationClientPaye = data_paye.numeroIdentification;
                }

                if (Tools.Tools.DeterminerTypeInitiation(data_paye.alias, data_paye.iban, data_paye.other) == Type_initie.alias)
                    new_transfert.aliasClientPaye = data_paye.alias;

                return await ConfirmerTransfert(new_transfert,iddemande);
            }
            catch (Exception ex)
            {

                _logger.LogError($"{_script} ====> : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }

        public async Task<GeneraleRetour> ConfirmerTransfert(t_transfert current_transfert, string iddemande)
        {
            const string SCRIPT = "SERVICE CONFIRMATION DE TRANSFERT";
            string TAG_REJET = tag_erreur.CODE_RAISON_REJET.ToString();

            try
            {
                // --- Guards
                if (current_transfert == null)
                    return new GeneraleRetour { status = 400, detail = "Transfert manquant." };

                // déjà rejeté / validé / irrévocable => conflit
                if (current_transfert.statut_general is STATUT_TRANSFERT.rejete
                    or STATUT_TRANSFERT.irrevocable)
                {
                    return new GeneraleRetour { status = 409, detail = "Le transfert a déjà été traité." };
                }

                // comptes essentiels
                if (string.IsNullOrWhiteSpace(current_transfert.aliasClientPayeur) &&
                    string.IsNullOrWhiteSpace(current_transfert.ibanClientPayeur) &&
                    string.IsNullOrWhiteSpace(current_transfert.otherClientPayeur))
                    return new GeneraleRetour { status = 400, detail = "Compte payeur manquant." };

                if (string.IsNullOrWhiteSpace(current_transfert.aliasClientPaye) &&
                    string.IsNullOrWhiteSpace(current_transfert.ibanClientPaye) &&
                    string.IsNullOrWhiteSpace(current_transfert.otherClientPaye))
                    return new GeneraleRetour { status = 400, detail = "Compte payé manquant." };

                // --- Préparation statut et identifiants
                current_transfert.etape = ETAPE_TRANSFERT.CONFIRME;
                current_transfert.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                current_transfert.IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30);
                await _transfertRepo.UpdateAsync(current_transfert);

                // --- Construire la requête PI
                var rq = Tools.BuildAndMap.BuildTransfertDto(current_transfert, _aipdata.devise);

                // -------------------------- TRANSFERT INTERNE --------------------------
                if (_aipdata.enabledInternalTransfer == true
                    && rq.codeMembreParticipantPaye == _aipdata.codemembre
                    && rq.codeMembreParticipantPaye == rq.codeMembreParticipantPayeur)
                {
                    current_transfert.sensFlux = sensFlux.INTERNE;

                    var data_transfert_interne = new TransfertInterneBodyDto
                    {
                        msgId = current_transfert.msgId,
                        endToEndId = current_transfert.endToEndId,
                        identifiantTransaction = current_transfert.IdOperationSib,
                        compteClientPayeur = current_transfert.compteClientPayeur ?? current_transfert.ibanClientPayeur ?? current_transfert.otherClientPayeur,
                        nomClientPayeur = current_transfert.nomClientPayeur,
                        montant = current_transfert.montant.ToString(),
                        compteClientPaye = current_transfert.compteClientPaye ?? current_transfert.ibanClientPaye ?? current_transfert.otherClientPaye,
                        nomClientPaye = current_transfert.nomClientPaye,
                        motif = current_transfert.motif
                    };

                    var resInterne = await _serviceAIF.OrdreDeTransfertInterne(data_transfert_interne, iddemande);

                    if (!Tools.Tools.RetourIsSucces(resInterne.status))
                    {
                        current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                        current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                        current_transfert.motifRejet = resInterne.detail;
                        current_transfert.codeRejet = resInterne.data; // code raison
                    }
                    else
                    {
                        current_transfert.dateHeureIrrevocabilite = DateTime.Now;
                        current_transfert.statut_general = STATUT_TRANSFERT.irrevocable;
                        current_transfert.etape = ETAPE_TRANSFERT.VALIDE;
                    }

                    await _transfertRepo.UpdateAsync(current_transfert);
                    return resInterne;
                }

                // -------------------------- TRANSFERT INTEROP --------------------------
                // 1) Réservation de fonds
                var reserver = new ReservationFondsBodyDto
                {
                    numeroCompte = current_transfert.ibanClientPayeur, // IBAN prioritaire
                    montantReserve = current_transfert.montant.ToString(),
                    identifiantTransaction = current_transfert.IdOperationSib,
                };

                var resResa = await _serviceAIF.FaireUneReservationDeFonds(reserver, iddemande);

                if (!string.IsNullOrEmpty(resResa.data))
                {
                    try
                    {
                        var dto = JsonConvert.DeserializeObject<ReservationDto_AIF>(resResa.data);
                        current_transfert.numEvenementReserv = dto?.numeroEvenement;
                        current_transfert.codeOperationReserv = dto?.codeOperation;
                        current_transfert.codeAgenceReserv = dto?.codeAgenceTransaction;
                    }
                    catch { /* si parsing rate, on ne bloque pas */ }
                }

                if (!Tools.Tools.RetourIsSucces(resResa.status))
                {
                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.motifRejet = resResa.detail;
                    await _transfertRepo.UpdateAsync(current_transfert);

                    resResa.detail = "Une erreur est survenue pendant la réservation de fonds";
                    return resResa;
                }

                await _transfertRepo.UpdateAsync(current_transfert);

                // 2) Envoi vers PI
                var resPi = await _envoieController.DemandeDeTransfert(rq);
                if (!resPi.operationResult)
                    return new GeneraleRetour { status = (int)resPi._statuscode, detail = resPi.messageResult };


                // Attente évènement PI (msgId ou e2e) avec tag de rejet d'annulation
                var (ok, rep) = await WaitPiAsync(rq.msgId, rq.endToEndId, TAG_REJET);
                if (!ok && rep is not null)
                {
                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.motifRejet = resResa.detail;
                    await _transfertRepo.UpdateAsync(current_transfert);


                    /// Annulation de la reservation
                    CancelReservationBody _bodyLeveeReservation = new CancelReservationBody
                    {
                        codeOperation = current_transfert.codeOperationReserv,
                        numEvenement = current_transfert.numEvenementReserv,
                        codeAgenceTransaction = current_transfert.codeAgenceReserv
                    };

                    await _serviceAIF.LeveeUneReservationDeFonds(_bodyLeveeReservation, iddemande);


                    _logger.LogWarning("Transfert rejeté (EndToEndId={E2E}, MsgId={MsgId}) => {@Rep}", rq.endToEndId, rq.msgId, rep);
                    return new GeneraleRetour { status = rep.status_code, detail = rep.desc_error };
                }


                return new GeneraleRetour { status = 200, detail = "Opération réussie" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{SCRIPT} : erreur inattendue", SCRIPT);
                return new GeneraleRetour { status = 500 };
            }
        }

      
        public async Task<GeneraleRetour> ConfirmerTransfertDeDisponibilite(t_transfert_dispo current_transfert, string iddemande)
        {
            string _script = "SERVICE CONFIRMATION DE TRANSFERT DE DISPONIBILITE";

            try
            {


                TransfertDto rq = new TransfertDto();

                rq.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                current_transfert.msgId = rq.msgId;
                current_transfert.etape = ETAPE_TRANSFERT.CONFIRME;
                await _transfertDispoRepo.UpdateAsync(current_transfert);

                rq.endToEndId = current_transfert.endToEndId;
                rq.montant = current_transfert.montant.ToString();
                rq.motif = current_transfert.motif;
                rq.identifiantTransaction = current_transfert.identifiantTransaction;
                rq.referenceBulk = current_transfert.referenceBulk;
                rq.typeTransaction = current_transfert.typeTransaction;
                rq.canalCommunication = current_transfert.canalCommunication;
                rq.dateHeureAcceptation = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                rq.typeDocumentReference = current_transfert.typeDocumentReference;
                rq.numeroDocumentReference = current_transfert.numeroDocumentReference;
                if (current_transfert.montantRemisePaiementImmediat > 0)
                    rq.montant = (current_transfert.montant - current_transfert.montantRemisePaiementImmediat).ToString();

                if (current_transfert.tauxRemisePaiementImmediat > 0)
                    rq.montant = (current_transfert.montant - current_transfert.montant * (current_transfert.tauxRemisePaiementImmediat / 100)).ToString();

                if (current_transfert.montantAchat > 0) rq.montantAchat = current_transfert.montantAchat.ToString();
                if (current_transfert.montantRetrait > 0) rq.montantRetrait = current_transfert.montantRetrait.ToString();
                if (current_transfert.fraisRetrait > 0) rq.fraisRetrait = current_transfert.fraisRetrait.ToString();


                //  rq.latitudeClientPayeur = current_transfert.latitudeClientPayeur;
                //  rq.longitudeClientPayeur = current_transfert.longitudeClientPayeur;
                rq.codeMembreParticipantPayeur = current_transfert.codeMembreParticipantPayeur;
                rq.aliasClientPayeur = current_transfert.aliasClientPayeur;
                rq.nomClientPayeur = current_transfert.nomClientPayeur;
                rq.dateNaissanceClientPayeur = current_transfert.dateNaissanceClientPayeur;
                rq.typeCompteClientPayeur = current_transfert.typeCompteClientPayeur;
                rq.villeClientPayeur = current_transfert.villeClientPayeur;
                rq.villeNaissanceClientPayeur = current_transfert.villeNaissanceClientPayeur;
                rq.adresseClientPayeur = current_transfert.adresseClientPayeur;
                rq.ibanClientPayeur = current_transfert.ibanClientPayeur;
                rq.otherClientPayeur = current_transfert.otherClientPayeur;
                rq.paysClientPayeur = current_transfert.paysClientPayeur;
                rq.paysNaissanceClientPayeur = current_transfert.paysNaissanceClientPayeur;
                rq.deviseCompteClientPayeur = _aipdata.devise;
                rq.typeClientPayeur = current_transfert.typeClientPayeur;

                if (rq.typeClientPayeur == "C")
                    rq.numeroRCCMClientPayeur = current_transfert.numeroRCCMClientPayeur;

                // Parametre non obligatoire pour TRAL
                if (rq.typeCompteClientPayeur != "TRAL")
                {
                    rq.systemeIdentificationClientPayeur = current_transfert.systemeIdentificationClientPayeur;
                    rq.numeroIdentificationClientPayeur = current_transfert.numeroIdentificationClientPayeur;
                }

                rq.nomClientPaye = current_transfert.nomClientPaye;
                rq.dateNaissanceClientPaye = current_transfert.dateNaissanceClientPaye;
                rq.typeCompteClientPaye = current_transfert.typeCompteClientPaye;
                rq.villeClientPaye = current_transfert.villeClientPaye;
                rq.villeNaissanceClientPaye = current_transfert.villeNaissanceClientPaye;
                rq.adresseClientPaye = current_transfert.adresseClientPaye;
                rq.ibanClientPaye = current_transfert.ibanClientPaye;
                rq.otherClientPaye = current_transfert.otherClientPaye;
                rq.paysClientPaye = current_transfert.paysClientPaye;
                rq.paysNaissanceClientPaye = current_transfert.paysNaissanceClientPaye;
                rq.codeMembreParticipantPaye = current_transfert.codeMembreParticipantPaye;
                rq.typeClientPaye = current_transfert.typeClientPaye;
                rq.typeTransaction = current_transfert.typeTransaction;

                rq.deviseCompteClientPaye = (string.IsNullOrEmpty(current_transfert.deviseCompteClientPaye)) ? _aipdata.devise : current_transfert.deviseCompteClientPaye;

                if (rq.typeCompteClientPaye != "TRAL")
                {
                    rq.systemeIdentificationClientPaye = current_transfert.systemeIdentificationClientPaye;
                    rq.numeroIdentificationClientPaye = current_transfert.numeroIdentificationClientPaye;
                }

                if (rq.typeClientPaye == "C")
                    rq.numeroRCCMClientPaye = current_transfert.numeroRCCMClientPaye;

                if (!string.IsNullOrEmpty(current_transfert.aliasClientPaye))
                    rq.aliasClientPaye = current_transfert.aliasClientPaye;



                // Envoi du transfert a PI
                var resDemandeTransfert = await _envoieController.DemandeDeTransfert(rq);

                if (!resDemandeTransfert.operationResult) // Si Envoi a PI Echouée
                    return (new GeneraleRetour { status = (int)resDemandeTransfert._statuscode, detail = resDemandeTransfert.messageResult });


                return (new GeneraleRetour { status = 200, detail = "Opération réussie" });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{_script} de transfert : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }

        public async Task<GeneraleRetour> ConfirmerTransfertAvecOrdreDeDebit(t_transfert current_transfert, string iddemande)
        {
            const string SCRIPT = "SERVICE CONFIRMATION DE TRANSFERT";

            try
            {
                // --- Préparation transferts / IDs
                current_transfert.etape = ETAPE_TRANSFERT.CONFIRME;

                if (string.IsNullOrWhiteSpace(current_transfert.identifiantTransaction))
                    current_transfert.identifiantTransaction = Tools.Tools.GenerateAlphaNumeriquevalue(30);

                current_transfert.IdOperationSib = current_transfert.identifiantTransaction;

                // msgId DOIT être stocké sur l'entité (bugfix)
                current_transfert.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                await _transfertRepo.UpdateAsync(current_transfert);

                // --- Construire la requête PI (mapping factorisé)
                var rq = Tools.BuildAndMap.BuildTransfertDto(current_transfert, _aipdata.devise);

                // -------------------------- TRANSFERT INTERNE --------------------------
                if (rq.codeMembreParticipantPaye == _aipdata.codemembre
                    && rq.codeMembreParticipantPayeur == _aipdata.codemembre)
                {
                    current_transfert.sensFlux = sensFlux.INTERNE;

                    var dataTransfertInterne = new TransfertInterneBodyDto
                    {
                        msgId = current_transfert.msgId,
                        endToEndId = current_transfert.endToEndId,
                        identifiantTransaction = current_transfert.IdOperationSib,
                        compteClientPayeur = current_transfert.compteClientPayeur ?? current_transfert.ibanClientPayeur ?? current_transfert.otherClientPayeur,
                        nomClientPayeur = current_transfert.nomClientPayeur,
                        montant = current_transfert.montant.ToString(),
                        compteClientPaye = current_transfert.compteClientPaye ?? current_transfert.ibanClientPaye ?? current_transfert.otherClientPaye,
                        nomClientPaye = current_transfert.nomClientPaye,
                        motif = current_transfert.motif
                    };

                    var resInterne = await _serviceAIF.OrdreDeTransfertInterne(dataTransfertInterne, iddemande);

                    if (!Tools.Tools.RetourIsSucces(resInterne.status))
                    {
                        current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                        current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                        current_transfert.motifRejet = resInterne.detail;
                        current_transfert.codeRejet = resInterne.data; // code raison
                    }
                    else
                    {
                        current_transfert.dateHeureIrrevocabilite = DateTime.Now;
                        current_transfert.statut_general = STATUT_TRANSFERT.irrevocable;
                        current_transfert.etape = ETAPE_TRANSFERT.VALIDE;
                    }

                    await _transfertRepo.UpdateAsync(current_transfert);
                    return resInterne;
                }

                // -------------------------- TRANSFERT INTEROP --------------------------
                // 1) Réservation de fonds
                var reserver = new ReservationFondsBodyDto
                {
                    numeroCompte = current_transfert.ibanClientPayeur, // IBAN prioritaire
                    montantReserve = current_transfert.montant.ToString(),
                    identifiantTransaction = current_transfert.IdOperationSib,
                };

                var resResa = await _serviceAIF.FaireUneReservationDeFonds(reserver, iddemande);

                if (!string.IsNullOrEmpty(resResa.data))
                {
                    try
                    {
                        var dto = JsonConvert.DeserializeObject<ReservationDto_AIF>(resResa.data);
                        current_transfert.numEvenementReserv = dto?.numeroEvenement;
                        current_transfert.codeOperationReserv = dto?.codeOperation;
                        current_transfert.codeAgenceReserv = dto?.codeAgenceTransaction;
                    }
                    catch { /* on ne bloque pas si le parsing échoue */ }
                }

                if (!Tools.Tools.RetourIsSucces(resResa.status))
                {
                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.motifRejet = resResa.detail;
                    await _transfertRepo.UpdateAsync(current_transfert);
                    return resResa;
                }

                await _transfertRepo.UpdateAsync(current_transfert);

                // 2) Envoi du transfert à PI
                var resPi = await _envoieController.DemandeDeTransfert(rq);
                if (!resPi.operationResult)
                    return new GeneraleRetour { status = (int)resPi._statuscode, detail = resPi.messageResult };

                // 3) Attente évènement PI avec timeout (pattern Task.WhenAny)
                int TIMEOUT_SECONDS = _aipdata.timeOutReponse ?? 30;

                var reqId = _eventService.RegisterRequest(resPi.idoperation);
                var waitEventTask = _eventService.GetTasks(reqId);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS));

                var completed = await Task.WhenAny(waitEventTask, timeoutTask);

                if (completed != waitEventTask)
                {
                    // Timeout => on considère OK (comportement inchangé par rapport à tes autres méthodes)
                    _logger.LogWarning("Transfert {E2E}: pas de réponse PI après {Timeout}s, on continue comme si OK.", rq.endToEndId, TIMEOUT_SECONDS);
                    return new GeneraleRetour { status = 200, detail = "Opération en cours" };
                }

                var message = await waitEventTask;
                var env = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON_REJET.ToString());

                // 4) Si PI rejette, lever la réservation + MAJ statut
                if (!Tools.Tools.RetourIsSucces(env.status_code))
                {
                    var cancel = new CancelReservationBody
                    {
                        codeOperation = current_transfert.codeOperationReserv,
                        numEvenement = current_transfert.numEvenementReserv,
                        codeAgenceTransaction = current_transfert.codeAgenceReserv
                    };

                    // On tente la levée de fond (pas bloquant)
                    try { await _serviceAIF.LeveeUneReservationDeFonds(cancel, iddemande); } catch { /* best effort */ }

                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.codeRejet = env.code_error;
                    current_transfert.motifRejet = env.desc_error;
                    await _transfertRepo.UpdateAsync(current_transfert);

                    return new GeneraleRetour { status = env.status_code, detail = env.desc_error };
                }

                // 5) Décodage de la réponse succès
                var rep = JsonConvert.DeserializeObject<ReponseRecuDemandeDeTransfert>(env.data ?? "{}");

                // Prépare corps levée de réservation (servira dans les 2 branches suivantes)
                var levee = new CancelReservationBody
                {
                    codeOperation = current_transfert.codeOperationReserv,
                    numEvenement = current_transfert.numEvenementReserv,
                    codeAgenceTransaction = current_transfert.codeAgenceReserv
                };

                

                // 6) Si ACCC/ACSC => Ordre de débit, sinon rejet
                if (rep != null && (rep.statutTransaction == "ACCC" || rep.statutTransaction == "ACSC"))
                {
                    // Récup code banque du payé pour l’ordre de débit
                    string codeBanquePaye = string.Empty;
                    var p = await _participantRepo.searchParticipant(current_transfert.codeMembreParticipantPaye);
                    if (p != null) codeBanquePaye = p.codeBanque;

                    var ordre = new OrdreDeDebitBodyDto
                    {
                        msgId = current_transfert.msgId,
                        endToEndId = current_transfert.endToEndId,
                        identifiantTransaction = current_transfert.IdOperationSib,
                        compteClientPayeur = current_transfert.ibanClientPayeur,
                        nomClientPayeur = current_transfert.nomClientPayeur,
                        montant = current_transfert.montant.ToString(),
                        compteClientPaye = current_transfert.compteClientPaye,
                        codeMembreParticipantPaye = codeBanquePaye,
                        nomClientPaye = current_transfert.nomClientPaye,
                        motif = current_transfert.motif
                    };



                    var resLevee = await _serviceAIF.LeveeUneReservationDeFonds(levee, iddemande);
                    if (!Tools.Tools.RetourIsSucces(resLevee.status))
                    {
                        current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                        current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                        current_transfert.motifRejet = resLevee.detail;
                        current_transfert.codeRejet = resLevee.data;
                        await _transfertRepo.UpdateAsync(current_transfert);
                        return resLevee;
                    }

                    var resDebit = await _serviceAIF.OrdreDeDebit(ordre, iddemande);
                    if (!Tools.Tools.RetourIsSucces(resDebit.status))
                    {
                        current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                        current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                        current_transfert.motifRejet = resDebit.detail;
                        current_transfert.codeRejet = resDebit.data;
                        await _transfertRepo.UpdateAsync(current_transfert);
                        return resDebit;
                    }

                    // Débit OK
                    current_transfert.statut_general = STATUT_TRANSFERT.irrevocable;
                    current_transfert.etape = ETAPE_TRANSFERT.VALIDE;
                    current_transfert.dateHeureIrrevocabilite = DateTime.Now;
                    await _transfertRepo.UpdateAsync(current_transfert);

                    return new GeneraleRetour { status = resDebit.status, detail = resDebit.detail };
                }
                else
                {
                    // Rejet opérationnel -> lever la réservation + MAJ statut
                    try { await _serviceAIF.LeveeUneReservationDeFonds(levee, iddemande); } catch { /* best effort */ }

                    string desc = "Transfert échoué";
                    if (rep != null)
                    {
                        var lib = await _datarepo.getItemDescriptionByCodeAndKey(code_datas.CODE_RAISON.ToString(), rep.codeRaison);
                        if (!string.IsNullOrEmpty(lib)) desc = lib;
                        if (!string.IsNullOrEmpty(rep.informationsAdditionnelles)) desc += " " + rep.informationsAdditionnelles;
                    }

                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.codeRejet = rep?.codeRaison;
                    current_transfert.motifRejet = desc;
                    await _transfertRepo.UpdateAsync(current_transfert);

                    return new GeneraleRetour { status = 403, detail = desc };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{SCRIPT} : erreur inattendue", SCRIPT);
                return new GeneraleRetour { status = 500 };
            }
        }

        public async Task<GeneraleRetour> ConfirmerTransfertTest(t_transfert current_transfert, string iddemande)
        {
            string _script = "SERVICE CONFIRMATION DE TRANSFERT TEST";

            try
            {

                current_transfert.etape = ETAPE_TRANSFERT.CONFIRME;
                await _transfertRepo.UpdateAsync(current_transfert);

                _logger.LogInformation($"{_script}... DEBUT");

                TransfertDto rq = new TransfertDto();

                current_transfert.IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30);
                string IdentifiantSIB = current_transfert.IdOperationSib;


                rq.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                rq.montant = current_transfert.montant.ToString();
                rq.motif = current_transfert.motif;
                rq.referenceBulk = current_transfert.referenceBulk;
                rq.typeTransaction = current_transfert.typeTransaction;
                rq.canalCommunication = current_transfert.canalCommunication;
                rq.dateHeureAcceptation = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                rq.identifiantTransaction = current_transfert.identifiantTransaction;
                rq.typeDocumentReference = current_transfert.typeDocumentReference;
                rq.numeroDocumentReference = current_transfert.numeroDocumentReference;
                rq.latitudeClientPayeur = current_transfert.latitudeClientPayeur;
                rq.longitudeClientPayeur = current_transfert.longitudeClientPayeur;

                rq.codeMembreParticipantPayeur = current_transfert.codeMembreParticipantPayeur;
                rq.aliasClientPayeur = current_transfert.aliasClientPayeur;
                rq.nomClientPayeur = current_transfert.nomClientPayeur;
                rq.dateNaissanceClientPayeur = current_transfert.dateNaissanceClientPayeur;
                rq.typeCompteClientPayeur = current_transfert.typeCompteClientPayeur;
                rq.villeClientPayeur = current_transfert.villeClientPayeur;
                rq.villeNaissanceClientPayeur = current_transfert.villeNaissanceClientPayeur;
                rq.adresseClientPayeur = current_transfert.adresseClientPayeur;
                rq.ibanClientPayeur = current_transfert.ibanClientPayeur;
                rq.otherClientPayeur = current_transfert.otherClientPayeur;
                rq.paysClientPayeur = current_transfert.paysClientPayeur;
                rq.paysNaissanceClientPayeur = current_transfert.paysNaissanceClientPayeur;
                rq.deviseCompteClientPayeur = _aipdata.devise;
                rq.typeClientPayeur = current_transfert.typeClientPayeur;
                rq.typeDocumentReference = current_transfert.typeDocumentReference;
                rq.numeroDocumentReference = current_transfert.numeroDocumentReference;

                if (current_transfert.montantRemisePaiementImmediat > 0)
                    rq.montant = (current_transfert.montant - current_transfert.montantRemisePaiementImmediat).ToString();

                if (current_transfert.tauxRemisePaiementImmediat > 0)
                    rq.montant = (current_transfert.montant - current_transfert.montant * (current_transfert.tauxRemisePaiementImmediat / 100)).ToString();

                if (current_transfert.montantAchat > 0) rq.montantAchat = current_transfert.montantAchat.ToString();
                if (current_transfert.montantRetrait > 0) rq.montantRetrait = current_transfert.montantRetrait.ToString();
                if (current_transfert.fraisRetrait > 0) rq.fraisRetrait = current_transfert.fraisRetrait.ToString();


                if (rq.typeClientPayeur == "C")
                    rq.numeroRCCMClientPayeur = current_transfert.numeroRCCMClientPayeur;

                // Parametre non obligatoire pour TRAL
                if (rq.typeCompteClientPayeur != "TRAL")
                {
                    rq.systemeIdentificationClientPayeur = current_transfert.systemeIdentificationClientPayeur;
                    rq.numeroIdentificationClientPayeur = current_transfert.numeroIdentificationClientPayeur;
                }

                rq.endToEndId = current_transfert.endToEndId;
                rq.nomClientPaye = current_transfert.nomClientPaye;
                rq.dateNaissanceClientPaye = current_transfert.dateNaissanceClientPaye;
                rq.typeCompteClientPaye = current_transfert.typeCompteClientPaye;
                rq.villeClientPaye = current_transfert.villeClientPaye;
                rq.villeNaissanceClientPaye = current_transfert.villeNaissanceClientPaye;
                rq.adresseClientPaye = current_transfert.adresseClientPaye;
                rq.ibanClientPaye = current_transfert.ibanClientPaye;
                rq.otherClientPaye = current_transfert.otherClientPaye;
                rq.paysClientPaye = current_transfert.paysClientPaye;
                rq.paysNaissanceClientPaye = current_transfert.paysNaissanceClientPaye;
                rq.codeMembreParticipantPaye = current_transfert.codeMembreParticipantPaye;
                rq.typeClientPaye = current_transfert.typeClientPaye;
                rq.deviseCompteClientPaye = (string.IsNullOrEmpty(current_transfert.deviseCompteClientPaye)) ? _aipdata.devise : current_transfert.deviseCompteClientPaye;

                if (rq.typeCompteClientPaye != "TRAL")
                {
                    rq.systemeIdentificationClientPaye = current_transfert.systemeIdentificationClientPaye;
                    rq.numeroIdentificationClientPaye = current_transfert.numeroIdentificationClientPaye;
                }

                if (rq.typeClientPaye == "C")
                    rq.numeroRCCMClientPaye = current_transfert.numeroRCCMClientPaye;

                if (!string.IsNullOrEmpty(current_transfert.aliasClientPaye))
                    rq.aliasClientPaye = current_transfert.aliasClientPaye;


           
                current_transfert.numEvenementReserv = "TEST";
                current_transfert.codeOperationReserv = "TEST";
                current_transfert.codeAgenceReserv = "TEST";


                // Mise de la ligne dans la base de données
                await _transfertRepo.UpdateAsync(current_transfert);

                // Envoi du transfert a PI
                var resDemandeTransfert = await _envoieController.DemandeDeTransfert(rq);

                if (!resDemandeTransfert.operationResult) // Si Envoi a PI Echouée
                    return (new GeneraleRetour { status = 400, detail = resDemandeTransfert.messageResult });

                //*************************Enregistrement de l'ID de la requette **************************
                var requestId = _eventService.RegisterRequest(resDemandeTransfert.idoperation);
                //*************************En attente de la tache consacré a cette requette****************
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto reponseTraiteDto = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON_REJET.ToString());

                if (!Tools.Tools.RetourIsSucces(reponseTraiteDto.status_code))
                {
                    // Annuler la reservation
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.codeRejet = reponseTraiteDto.code_error;
                    current_transfert.motifRejet = reponseTraiteDto.desc_error;
                    await _transfertRepo.UpdateAsync(current_transfert);
                    return (new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error });
                }

                //************************Traitement de la reponse ***************************************
                _logger.LogInformation($"{_script} Retour demande transfert : {reponseTraiteDto.data}");

                var ret = JsonConvert.DeserializeObject<ReponseRecuDemandeDeTransfert>(reponseTraiteDto.data);

                if (ret != null && (ret.statutTransaction == "ACCC" || ret.statutTransaction == "ACSC"))
                {

                    // Mettre le statut a Irrevocable // Reception de l'avis de debit
                    current_transfert.statut_general = STATUT_TRANSFERT.irrevocable;
                    current_transfert.etape = ETAPE_TRANSFERT.IRREVOCABLE;
                    //  await _transfertRepo.UpdateAsync(current_transfert);

                    string _codeBanquePaye = "";

                    t_participant p = await _participantRepo.searchParticipant(current_transfert.codeMembreParticipantPaye);

                    current_transfert.etape = ETAPE_TRANSFERT.VALIDE;

                    await _transfertRepo.UpdateAsync(current_transfert);

                    return (new GeneraleRetour { status = 200, detail = "Transfert effectué avec succès" });
                }
                else
                {

                    // Reception de rejet du transfert

                    //// Annulation de la réservation de fonds

                    string _desc = await _datarepo.getItemDescriptionByCodeAndKey(code_datas.CODE_RAISON.ToString(), ret.codeRaison);
                    if (string.IsNullOrEmpty(_desc)) _desc = "Transfert echouée";
                    if (!string.IsNullOrEmpty(ret.informationsAdditionnelles)) _desc += " " + ret.informationsAdditionnelles;

                    // Mise a jour du statu du transfert
                    current_transfert.etape = ETAPE_TRANSFERT.REJETE;
                    current_transfert.statut_general = STATUT_TRANSFERT.rejete;
                    current_transfert.codeRejet = ret.codeRaison;
                    current_transfert.motifRejet = _desc;
                    await _transfertRepo.UpdateAsync(current_transfert);

                    return (new GeneraleRetour { status = 403, detail = _desc });
                }

                //*****************************************SI REPONSE EST OK ALORS LANCE L'EVENEMENT***********************************
                //}


            }
            catch (Exception ex)
            {
                _logger.LogError($"{_script} de transfert : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }


        public async Task<GeneraleRetour> ConfirmerEtEnvoyerDemandePaiement(t_transfert rtp, string iddemande)
        {
            const string _script = "SERVICE CONFIRMATION DE DEMANDE DE PAIEMENT";

            try
            {
                // 1) Confirmer + (re)générer un msgId
                rtp.etape = ETAPE_TRANSFERT.CONFIRME;
                rtp.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                await _transfertRepo.UpdateAsync(rtp);

                _logger.LogInformation("{Script}... DEBUT (MsgId={MsgId}, EndToEndId={E2E})", _script, rtp.msgId, rtp.endToEndId);

                var rq = new DemandeDePaiementDTO
                {
                    clientDemandeur = string.IsNullOrWhiteSpace(rtp.photoClientPaye) ? "X" : rtp.photoClientPaye,

                    msgId = rtp.msgId,
                    montant = Convert.ToString(rtp.montant, System.Globalization.CultureInfo.InvariantCulture),
                    motif = rtp.motif,
                    canalCommunication = rtp.canalCommunication,
                    identifiantDemandePaiement = rtp.identifiantTransaction,
                    typeDocumentReference = rtp.typeDocumentReference,
                    numeroDocumentReference = rtp.numeroDocumentReference,
                    latitudeClientPaye = rtp.latitudeClientPaye,
                    longitudeClientPaye = rtp.longitudeClientPaye, // FIX

                    // Payeur
                    codeMembreParticipantPayeur = rtp.codeMembreParticipantPayeur,
                    aliasClientPayeur = rtp.aliasClientPayeur,
                    nomClientPayeur = rtp.nomClientPayeur,
                    dateNaissanceClientPayeur = rtp.dateNaissanceClientPayeur,
                    typeCompteClientPayeur = rtp.typeCompteClientPayeur,
                    villeClientPayeur = rtp.villeClientPayeur,
                    villeNaissanceClientPayeur = rtp.villeNaissanceClientPayeur,
                    adresseClientPayeur = rtp.adresseClientPayeur,
                    ibanClientPayeur = rtp.ibanClientPayeur,
                    otherClientPayeur = rtp.otherClientPayeur,
                    paysClientPayeur = rtp.paysClientPayeur,
                    paysNaissanceClientPayeur = rtp.paysNaissanceClientPayeur,
                    deviseCompteClientPayeur = _aipdata.devise,
                    typeClientPayeur = rtp.typeClientPayeur,
                    identifiantMandat = rtp.identifiantMandat,
                    signatureNumeriqueMandat = rtp.signatureNumeriqueMandat,
                    autorisationModificationMontant = rtp.autorisationModificationMontant,

                    // Bénéficiaire (demandeur)
                    codeMembreParticipantPaye = rtp.codeMembreParticipantPaye,
                    endToEndId = rtp.endToEndId,
                    nomClientPaye = rtp.nomClientPaye,
                    dateNaissanceClientPaye = rtp.dateNaissanceClientPaye,
                    typeCompteClientPaye = rtp.typeCompteClientPaye,
                    villeClientPaye = rtp.villeClientPaye,
                    villeNaissanceClientPaye = rtp.villeNaissanceClientPaye,
                    adresseClientPaye = rtp.adresseClientPaye,
                    ibanClientPaye = rtp.ibanClientPaye,
                    otherClientPaye = rtp.otherClientPaye,
                    paysClientPaye = rtp.paysClientPaye,
                    paysNaissanceClientPaye = rtp.paysNaissanceClientPaye,
                    typeClientPaye = rtp.typeClientPaye,
                    deviseCompteClientPaye = string.IsNullOrEmpty(rtp.deviseCompteClientPaye) ? _aipdata.devise : rtp.deviseCompteClientPaye
                };

                if (rtp.tauxRemisePaiementImmediat != null) rq.tauxRemisePaiementImmediat = Convert.ToString(rtp.tauxRemisePaiementImmediat, System.Globalization.CultureInfo.InvariantCulture);
                if (rtp.montantRemisePaiementImmediat != null) rq.montantRemisePaiementImmediat = Convert.ToString(rtp.montantRemisePaiementImmediat, System.Globalization.CultureInfo.InvariantCulture);
                if (rtp.montantAchat != null) rq.montantAchat = Convert.ToString(rtp.montantAchat, System.Globalization.CultureInfo.InvariantCulture);
                if (rtp.montantRetrait != null) rq.montantRetrait = Convert.ToString(rtp.montantRetrait, System.Globalization.CultureInfo.InvariantCulture);
                if (rtp.fraisRetrait != null) rq.fraisRetrait = Convert.ToString(rtp.fraisRetrait, System.Globalization.CultureInfo.InvariantCulture);

                if (!(new[] { "500", "521", "631" }).Contains(rq.canalCommunication))
                {
                    rq.dateHeureExecution = Tools.Tools.ConvertirDateTimeEnFormatJson(rtp.dateHeureExecution);
                    rq.dateLimiteAction = Tools.Tools.ConvertirDateTimeEnFormatJson(rtp.dateLimiteAction);
                }

                if (rq.typeClientPayeur == "C")
                    rq.numeroRCCMClientPayeur = rtp.numeroRCCMClientPayeur;

                if (!string.Equals(rq.typeCompteClientPayeur, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    rq.systemeIdentificationClientPayeur = rtp.systemeIdentificationClientPayeur;
                    rq.numeroIdentificationClientPayeur = rtp.numeroIdentificationClientPayeur;
                }

                if (!string.Equals(rq.typeCompteClientPaye, "TRAL", StringComparison.OrdinalIgnoreCase))
                {
                    rq.systemeIdentificationClientPaye = rtp.systemeIdentificationClientPaye;
                    rq.numeroIdentificationClientPaye = rtp.numeroIdentificationClientPaye;
                }

                if (rq.typeClientPaye == "C")
                    rq.numeroRCCMClientPaye = rtp.numeroRCCMClientPaye;

                if (!string.IsNullOrEmpty(rtp.aliasClientPaye))
                    rq.aliasClientPaye = rtp.aliasClientPaye;

                // 3) Cas interne

                _logger.LogInformation("codeMembreParticipantPayeur === " + rtp.codeMembreParticipantPayeur);
                _logger.LogInformation("codeMembreParticipantPaye === " + rtp.codeMembreParticipantPaye);
                _logger.LogInformation("enabledInternalTransfer === " + _aipdata.enabledInternalTransfer);

                if (_aipdata.enabledInternalTransfer == true
                    && rq.codeMembreParticipantPaye == _aipdata.codemembre
                    && rq.codeMembreParticipantPaye == rq.codeMembreParticipantPayeur)

                {
                    rtp.sensFlux = sensFlux.INTERNE;
                    rtp.etape = ETAPE_TRANSFERT.INITIEE;
                    rtp.statut_general = STATUT_TRANSFERT.initie;
                    await _transfertRepo.UpdateAsync(rtp);

                    _logger.LogInformation("Demande de paiement interne");


                    ////  Notifier celui qui envoie

                    var notif_demandeur_rtp = new t_notification
                    {
                        type = type_notif.RTP_INITIEE.ToString(),
                        estCliquable = true,
                        compte = rtp.compteClientPaye, // le demandeur reçoit la demande
                        idObject = rtp.endToEndId,
                        dateAction = DateTime.Now,
                        details = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new { montant = rq.montant, payeur = rq.nomClientPayeur }))
                    };

                    _logger.LogInformation("Notifier le demandeur OK");

                    ////  Notifier le payeur

                    var notif_payeur = new t_notification
                    {
                        type = type_notif.RTP_RECUE.ToString(),
                        estCliquable = true,
                        compte = rtp.compteClientPayeur, // le PAYEUR reçoit la demande
                        idObject = rtp.endToEndId,
                        dateAction = DateTime.Now,
                        details = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new { montant = rq.montant, emetteur = rq.nomClientPaye }))
                    };

                    await _notificationRepo.AddRangeAsync(new[] { notif_demandeur_rtp, notif_payeur });

                    _logger.LogInformation("Notifier le payeur OK");

                    return new GeneraleRetour
                    {
                        status = 200,
                        detail = $"Vous avez envoyé une demande de paiement à {rq.nomClientPayeur}",
                        data = JsonConvert.SerializeObject(rtp)
                    };
                }

                // 4) Cas interopérable (envoi AIP)
                var resDemande = await _envoieController.DemandeDePaiement(rq);
                if (!resDemande.operationResult)
                {
                    rtp.etape = ETAPE_TRANSFERT.REJETE;
                    rtp.statut_general = STATUT_TRANSFERT.rejete;
                    rtp.motifRejet = resDemande.messageResult;
                    await _transfertRepo.UpdateAsync(rtp);

                    return new GeneraleRetour { status = (int)resDemande._statuscode, detail = resDemande.messageResult };
                }

                // --- Attente d'événement avec Task.WhenAny (pattern conservé) ---
                int TIMEOUT_SECONDS = _aipdata.timeOutReponse ?? 30;
                var requestId = _eventService.RegisterRequest(rq.msgId);
                var waitTask = _eventService.GetTasks(requestId);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS));

                var completed = await Task.WhenAny(waitTask, timeoutTask);

                if (completed == waitTask)
                {
                    var message = await waitTask;

                    ReponseTraiteDto env = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON_REJET.ToString());

                    if (!Tools.Tools.RetourIsSucces(env.status_code))
                        return (new GeneraleRetour { status = env.status_code, detail = env.desc_error });


                    if (env.status_code != 200)
                    {
                        _logger.LogWarning("{Script} - Rejet AIP (MsgId={MsgId}, Code={Code}, Desc={Desc})", _script, rq.msgId, env.code_error, env.desc_error);

                        rtp.etape = ETAPE_TRANSFERT.REJETE;
                        rtp.statut_general = STATUT_TRANSFERT.rejete;
                        rtp.motifRejet = env.desc_error;
                        rtp.codeRejet = env.code_error;
                        await _transfertRepo.UpdateAsync(rtp);

                        return new GeneraleRetour { status = env.status_code, detail = env.desc_error };
                    }
                }
                else
                {
                    // Timeout → on considère OK (comportement existant)
                    _logger.LogWarning("{Script} - Aucune réponse après {Timeout}s (MsgId={MsgId}) : on considère la demande comme OK.",
                        _script, TIMEOUT_SECONDS, rq.msgId);
                }

                // Succès
                rtp.etape = ETAPE_TRANSFERT.INITIEE;
                rtp.statut_general = STATUT_TRANSFERT.initie;
                await _transfertRepo.UpdateAsync(rtp);


                ////  Notifier celui qui envoie
                var dataInitie = new { montant = rq.montant, payeur = rq.nomClientPayeur };
                using var detailsInitie = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dataInitie));

                var notif_demandeur = new t_notification
                {
                    type = type_notif.RTP_INITIEE.ToString(),
                    estCliquable = true,
                    compte = rtp.compteClientPaye, // le demandeur reçoit la demande
                    idObject = rtp.endToEndId,
                    dateAction = DateTime.Now,
                    details = detailsInitie
                };

                await _notificationRepo.AddAsync(notif_demandeur);





                return new GeneraleRetour
                {
                    status = 200,
                    detail = $"Vous avez envoyé une demande de paiement à {rq.nomClientPayeur}",
                    data = JsonConvert.SerializeObject(rtp)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} - Erreur inattendue", _script);
                return new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." };
            }
        }


        public TransactionDto ConvertirTransfertEnTransfertDto(t_transfert t,string? myIbanOrOther,Dictionary<string, string> countryDictionary,List<t_participant> participants)
        {
            bool isClientPayeur = string.Equals(myIbanOrOther, t.compteClientPayeur, StringComparison.OrdinalIgnoreCase);

            // côté autre client
            var clientPays = isClientPayeur ? t.paysClientPaye : t.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? t.codeMembreParticipantPaye : t.codeMembreParticipantPayeur;

            // pays (safe)
            countryDictionary.TryGetValue(clientPays ?? string.Empty, out var nomPays);

            // participant (safe)
            var participantTrouve = participants?.FirstOrDefault(p => p.codeMembreParticipant == clientCodeParticipant);
            var pspNom = participantTrouve?.nomOfficiel;

            return Tools.BuildAndMap.BuildTransactionDto(t, isClientPayeur, nomPays, pspNom);
        }


        public async Task<TransactionDto> ConvertirTransfertEnTransfertDto(t_transfert t, string? myIbanOrOther)
        {
            bool isClientPayeur = string.Equals(myIbanOrOther, t.compteClientPayeur, StringComparison.OrdinalIgnoreCase);

            var clientPays = isClientPayeur ? t.paysClientPaye : t.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? t.codeMembreParticipantPaye : t.codeMembreParticipantPayeur;

            // Récupérations async (safe null)
            var participantTrouve = await _participantRepo.searchParticipant(clientCodeParticipant);
            var nomPays = await _datarepo.getItemDescriptionByCodeAndKey(code_datas.PAYS.ToString(), clientPays);

            return Tools.BuildAndMap.BuildTransactionDto(t, isClientPayeur, nomPays, participantTrouve?.nomOfficiel);
        }


        public async Task<TransactionDto> ConvertirTransfertEnTransfertDispoDto(t_transfert_dispo t, string? myIbanOrOther)
        {

            bool isClientPayeur = (myIbanOrOther == t.compteClientPayeur);

            // Créer et retourner un nouvel objet TransactionMobileDto
            var clientCompte = isClientPayeur ? t.compteClientPaye : t.compteClientPayeur;
            var clientPays = isClientPayeur ? t.paysClientPaye : t.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? t.codeMembreParticipantPaye : t.codeMembreParticipantPayeur;

            var clientConnecteCompte = isClientPayeur ? t.compteClientPayeur : t.compteClientPaye;
            var clientConnecteAlias = isClientPayeur ? t.aliasClientPayeur : t.aliasClientPaye;

            t_participant participantTrouve = await _participantRepo.searchParticipant(clientCodeParticipant);
            string nomPays = await _datarepo.getItemDescriptionByCodeAndKey(code_datas.PAYS.ToString(), clientPays);


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
                dateOperation = t.r_createdon,
                clientPhoto = isClientPayeur ? t.photoClientPaye : t.photoClientPayeur,
                clientCompte = clientCompte,
                clientPaysNom = nomPays,
                clientPSPNom = participantTrouve.nomOfficiel,
                montantFrais = t.fraisRetrait,
                retraitAchat = t.montantAchat,
                facture = t.numeroDocumentReference,
                dateExpiration = t.dateLimiteAction
            };

        }

        public TransactionBusinessDto CreateTransactionBusinessDto(t_transfert t, List<string>? myIbanOrOther)
        {
            // Déterminer si le client est payé


            bool isClientPayeur = ((myIbanOrOther.Contains(t.ibanClientPayeur) || myIbanOrOther.Contains(t.otherClientPayeur)) && myIbanOrOther.Count > 0);

            // Créer et retourner un nouvel objet TransactionBusinessDto
            var clientIban = isClientPayeur ? t.ibanClientPaye : t.ibanClientPayeur;
            var clientOther = isClientPayeur ? t.otherClientPaye : t.otherClientPayeur;
            var clientPays = isClientPayeur ? t.paysClientPaye : t.paysClientPayeur;

            return new TransactionBusinessDto
            {
                endToEndId = t.endToEndId,
                montant = t.montant,
                sens = isClientPayeur ? "debit" : "credit",
                motif = t.motif,
                clientPSP = isClientPayeur ? t.codeMembreParticipantPaye : t.codeMembreParticipantPayeur,
                clientNom = isClientPayeur ? t.nomClientPaye : t.nomClientPayeur,
                clientPays = clientPays,
                dateIrrevocabilite = Tools.Tools.ConvertirDateTimeEnFormatJson(t.dateHeureAcceptation),
                clientCompte = !string.IsNullOrEmpty(clientIban) ? clientIban : clientOther
            };
        }

        public TransactionDto ConvertirSouscriptionEnTransfertDto(t_scheduled s, string? myIbanOrOther, Dictionary<string, string> countryDictionary, List<t_participant> participants)
        {

            bool isClientPayeur = string.Equals(myIbanOrOther, s.compteClientPayeur, StringComparison.OrdinalIgnoreCase);

            // côté autre client
            var clientPays = isClientPayeur ? s.paysClientPaye : s.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? s.codeMembreParticipantPaye : s.codeMembreParticipantPayeur;

            // pays (safe)
            countryDictionary.TryGetValue(clientPays ?? string.Empty, out var nomPays);

            // participant (safe)
            var participantTrouve = participants?.FirstOrDefault(p => p.codeMembreParticipant == clientCodeParticipant);
            var pspNom = participantTrouve?.nomOfficiel;

            return Tools.BuildAndMap.BuildSouscriptionDto(s, isClientPayeur, nomPays, pspNom);

        }

       
        public async Task<GeneraleRetour> RefuserUneDemandePaiement(t_transfert demande_paiement, string raison, string? iddemande)
        {
            const string SCRIPT = "SERVICE REPONSE A UNE DEMANDE DE PAIEMENT";

            try
            {
                // --- Guards de base
                if (demande_paiement == null)
                    return new GeneraleRetour { status = 400, detail = "Demande de paiement manquante." };

                if (!Tools.Tools.canalEstCanalDemandePaiement(demande_paiement.canalCommunication) || demande_paiement.is_delete == true)
                    return new GeneraleRetour { status = 404, detail = "La demande de paiement n'existe pas dans notre système" };

                if (demande_paiement.statut_general != STATUT_TRANSFERT.initie)
                    return new GeneraleRetour { status = 403, detail = "La demande de paiement est déjà traitée." };

                if (string.IsNullOrWhiteSpace(raison))
                    raison = "CUST"; // si aucun code n’est fourni

                // -------------------------- DEMANDE INTERNE --------------------------
                if (demande_paiement.sensFlux == sensFlux.INTERNE)
                {
                    demande_paiement.etape = ETAPE_TRANSFERT.REJETE;
                    demande_paiement.statut_general = STATUT_TRANSFERT.rejete;
                    await _transfertRepo.UpdateAsync(demande_paiement);

                    // Notifier le client qui a émis la demande (côté « payé » chez toi)
                    var data = new { montant = demande_paiement.montant, emetteur = demande_paiement.nomClientPayeur };
                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    using var details = JsonDocument.Parse(json);

                    var n = new t_notification
                    {
                        type = type_notif.RTP_REJETE.ToString(),
                        estCliquable = true,
                        compte = demande_paiement.compteClientPaye,
                        idObject = demande_paiement.endToEndId,
                        dateAction = DateTime.Now,
                        details = details
                    };
                    await _notificationRepo.AddAsync(n);

                    return new GeneraleRetour
                    {
                        status = 200,
                        detail = "La demande rejetée avec succès",
                        data = JsonConvert.SerializeObject(demande_paiement)
                    };
                }

                // -------------------------- DEMANDE INTEROP --------------------------
                var reponse = new ReponseaDemandeDePaiementDTO
                {
                    endToEndId = demande_paiement.endToEndId,
                    identifiantDemandePaiement = demande_paiement.identifiantTransaction,
                    referenceBulk = demande_paiement.referenceBulk,
                    msgIdDemande = demande_paiement.msgId,
                    statut = "RJCT",
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    codeRaison = raison
                };

                var res_dt = await _envoieController.ReponseAuneDemandeDePaiement(reponse);

                if (!res_dt.operationResult)
                    return new GeneraleRetour { status = (int)res_dt._statuscode, detail = res_dt.messageResult };

                // ---- Attente d’event (Task.WhenAny conservé)
                int TIMEOUT_SECONDS = _aipdata.timeOutReponse ?? 30;

                var requestIdMsg = _eventService.RegisterRequest(reponse.msgId);
                var waitEventIdMsgTask = _eventService.GetTasks(requestIdMsg);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS), CancellationToken.None);

                var completed = await Task.WhenAny(waitEventIdMsgTask, timeoutTask);

                if (completed == waitEventIdMsgTask)
                {
                    var message = await waitEventIdMsgTask;
                    var reponseTraiteDto = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON.ToString());

                    _logger.LogWarning(
                        "{SCRIPT} rejet: retour PI (EndToEndId={EndToEndId}, MsgId={MsgId}) => {@ReponseTraite}",
                        SCRIPT, reponse.endToEndId, reponse.msgId, reponseTraiteDto
                    );

                    if (reponseTraiteDto.status_code != 200)
                        return new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error };
                }
                else
                {
                    // Timeout => considéré OK
                    _logger.LogWarning(
                        "{SCRIPT} rejet: aucune réponse d’événement après {Timeout}s, on considère la demande comme OK. (EndToEndId={EndToEndId})",
                        SCRIPT, TIMEOUT_SECONDS, reponse.endToEndId
                    );
                }

                // Mise à jour finale
                demande_paiement.etape = ETAPE_TRANSFERT.REJETE;
                demande_paiement.statut_general = STATUT_TRANSFERT.rejete;
                await _transfertRepo.UpdateAsync(demande_paiement);

                return new GeneraleRetour
                {
                    status = 200,
                    detail = "La demande rejetée avec succès",
                    data = JsonConvert.SerializeObject(demande_paiement)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{SCRIPT}: erreur inattendue", SCRIPT);
                return new GeneraleRetour { status = 500 };
            }
        }

        public async Task<GeneraleRetour> RefuserUneAnnulation(t_transfert trsfert, string raison, string? iddemande)
        {
            string _script = "SERVICE REPONSE A UNE DEMANDE D'ANNULATION";

            try
            {



 
                var raisonPossible = new List<string> { "CUST", "AC04", "ARDT" };


                List<InvalidParam> invalidParams = new();


                if (!raisonPossible.Contains(raison))
                {
                    invalidParams.Add(new InvalidParam
                    {
                        name = "raison",
                        reason = $"La valeur '{raison}' n'est pas autorisée. Les raisons possibles sont : {string.Join(", ", raisonPossible)}"
                    });
                }

                if (invalidParams.Count > 0)
                    return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes", invalidParams = invalidParams });


            
                ///// Rechercher la demande d'annulation
                ///


                t_annulation_transfert ta = await _annulationTransfertRepo.CheckifReceptionExisteByEndToEnd(trsfert.endToEndId);

                if (ta == null)
                    return (new GeneraleRetour { status = 404, detail = "Aucune demande d'annulation trouvée pour cette transaction." });


                if (ta.statut != statutAnnulation.initie)
                    return (new GeneraleRetour { status = 403, detail = "La demande d'annulation est déjà traité " });


                if (ta.sensFlux == sensFlux.INTERNE) // Annulation en interne
                {

                    ta.statut = statutAnnulation.rejete;
                    ta.raisonRejetDemande = raison;
                    await _annulationTransfertRepo.UpdateAsync(ta);

                    trsfert.annulationStatut = STATUT_TRANSFERT.rejete;
                    trsfert.annulationStatutRaison = raison;
                    await _transfertRepo.UpdateAsync(trsfert);



                    // Notifier celui qui a envoyé l'annulation
                    /// 
                    var data = new
                    {
                        montant = trsfert.montant,
                        emetteur = trsfert.nomClientPaye,
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    JsonDocument details = JsonDocument.Parse(json);

                    t_notification n = new t_notification
                    {

                        type = type_notif.ANNULATION_REJETEE.ToString(),
                        estCliquable = true,
                        compte = trsfert.compteClientPayeur,
                        idObject = trsfert.endToEndId,
                        dateAction = DateTime.Now,
                        details = details
                    };

                    await _notificationRepo.AddAsync(n);

                    /// Changer le statut de toutes les autres demandes d'annulation en cours
                    await _annulationTransfertRepo.UpdateAllAnnulationsEnCours(trsfert.endToEndId, statutAnnulation.rejete);

                    return (new GeneraleRetour { status = 200, detail = "La demande est rejetée avec succès", data = JsonConvert.SerializeObject(trsfert) });
                }
                else

                {
                    int TIMEOUT_SECONDS = _aipdata.timeOutReponse ?? 30;


                    ReponseSendDemandeAnnulationDto _rep_demande = new ReponseSendDemandeAnnulationDto
                    {
                        endToEndId = trsfert.endToEndId,
                        msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                        msgIdDemande = ta.msgId,
                        codeMembreParticipantPayeur = ta.codeMembreParticipantPayeur,
                        statut = "RJCR",
                        raison = raison
                    };

                    var res_dt = await _envoieController.RepondreAUneDemandeAnnulation(_rep_demande);

                    if (res_dt == null)
                        return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes" });


                    if (res_dt.operationResult == false)
                        return (new GeneraleRetour { status = (int)res_dt._statuscode, detail = res_dt.messageResult });


                    // --- Attente de retour d'événement (msgId OU endToEndId) avec timeout 30s ---
                    var requestIdMsg = _eventService.RegisterRequest(_rep_demande.msgId);
                    var waitEventIdMsgTask = _eventService.GetTasks(requestIdMsg);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS), CancellationToken.None);

                    var completed = await Task.WhenAny(waitEventIdMsgTask, timeoutTask);

                    if (completed == waitEventIdMsgTask)
                    {
                        var message = await waitEventIdMsgTask;
                        var reponseTraiteDto = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON.ToString());

                        _logger.LogWarning(
                            "Rejet de la demande rejetée waitEventIdMsgTask (EndToEndId={EndToEndId}, MsgId={MsgId}) => {@ReponseTraite}",
                            _rep_demande.endToEndId, _rep_demande.msgId, reponseTraiteDto
                        );

                        if (reponseTraiteDto.status_code != 200)
                        {
                         return new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error };
                        }
                    }
                   
                    else
                    {
                        // Timeout (aucun event en 30s) => on considère OK
                        _logger.LogWarning(
                            "Rejet de la demande {EndToEndId}: aucune réponse d’événement après {Timeout}s, on considère la demande comme OK.",
                            _rep_demande.endToEndId, TIMEOUT_SECONDS
                        );
                    }
                

                    ta.statut = statutAnnulation.rejete;
                    ta.raisonRejetDemande = raison;
                    await _annulationTransfertRepo.UpdateAsync(ta);

                    trsfert.annulationStatut = STATUT_TRANSFERT.rejete;
                    trsfert.annulationStatutRaison = raison;
                    await _transfertRepo.UpdateAsync(trsfert);

                    /// Changer le statut de toutes les autres demandes d'annulation en cours
                    await _annulationTransfertRepo.UpdateAllAnnulationsEnCours(trsfert.endToEndId, statutAnnulation.rejete);


                    return (new GeneraleRetour { status = 200, detail = "La demande est rejetée avec succès", data = JsonConvert.SerializeObject(trsfert) });

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Service Repondre à une demande de paiement : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }


        public async Task<ReponseTraiteDto> TraiterRetourEvenement(string message,string? tag)
        {
            RetourEventDto retour = JsonConvert.DeserializeObject<RetourEventDto>(message); ;

            _logger.LogInformation($"TraiterRetourEvenement   =======================> retour.type {retour.type}");

            switch (retour.type)
            {
                case type_notification.NOTIFICATION_ECHEC_500_504:
                    RcpEchecMessageIso res_500 = JsonConvert.DeserializeObject<RcpEchecMessageIso>(retour.data);
                    return (new ReponseTraiteDto { status_code = 500, desc_error = res_500.detailEchec });
                case type_notification.NOTIFICATION_ECHEC_TRAITEMENT_AIP:
                    RcpEchecMessageIso res_rejet_trt = JsonConvert.DeserializeObject<RcpEchecMessageIso>(retour.data);
                    return (new ReponseTraiteDto { status_code = 400, desc_error = res_rejet_trt.detailEchec });
                case type_notification.NOTIFICATION_REPONSE_REQUETE:
                    return (new ReponseTraiteDto { status_code = 200, data = retour.data });
                case type_notification.NOTIFICATION_ECHEC_FORMAT_ISO_INVALIDE:
                    RcpMessageIsoFormatInvalide res_rejet_format = JsonConvert.DeserializeObject<RcpMessageIsoFormatInvalide>(retour.data);
                    return (new ReponseTraiteDto { status_code = 400, desc_error = res_rejet_format.detailEchec });
                case type_notification.NOTIFICATION_REPONSE_RETOUR_FONDS:
                    ReponseReceiveDemandeAnnulationDto res_reponse_rf = JsonConvert.DeserializeObject<ReponseReceiveDemandeAnnulationDto>(retour.data);
                    string _lib_error = await _codeErreurRepo.GetLibelleErreurAsync(res_reponse_rf.raison, tag);
                    return (new ReponseTraiteDto { status_code = 403, code_error = res_reponse_rf.raison, desc_error = _lib_error });

                case type_notification.NOTIFICATION_REPONSE_ECHEC:
                    ReponseEchecDto res_reponse_echec = JsonConvert.DeserializeObject<ReponseEchecDto>(retour.data);

                    string _desc = res_reponse_echec.descriptionRaisonRejet;

                    if (string.IsNullOrEmpty(_desc))
                        _desc = await _codeErreurRepo.GetLibelleErreurAsync(res_reponse_echec.codeRaisonRejet, tag);


                    if (string.IsNullOrEmpty(_desc)) _desc = "Traitement echoué";
                    if (!string.IsNullOrEmpty(res_reponse_echec.infoAdditionnelle)) _desc += " " + res_reponse_echec.infoAdditionnelle;

                    return (new ReponseTraiteDto { status_code = 403, code_error = res_reponse_echec.codeRaisonRejet, desc_error = _desc });
                case type_notification.NOTIFICATION_REJET_ECHEC:
                    ReponseNotificationEchec res_rejet_echec = JsonConvert.DeserializeObject<ReponseNotificationEchec>(retour.data);

                    string _description = res_rejet_echec.descriptionRaisonRejet;

                    if (string.IsNullOrEmpty(_description))
                    {
                        _description = await _codeErreurRepo.GetLibelleErreurAsync(res_rejet_echec.codeRaisonRejet, tag_erreur.CODE_RAISON_REJET.ToString());
                    }

                    if (string.IsNullOrEmpty(_description)) _desc = "Traitement echoué";
                    if (!string.IsNullOrEmpty(res_rejet_echec.infoAdditionnelle)) _description += " " + res_rejet_echec.infoAdditionnelle;

                    return (new ReponseTraiteDto { status_code = 403, code_error = res_rejet_echec.codeRaisonRejet, desc_error = _description });

            }

            return (new ReponseTraiteDto { status_code = 400, desc_error = "Une erreur est survenue pendant le traitement" });

        }


        public async Task<GeneraleRetour> DemanderAnnulationTransfert(t_transfert t, QueryAnnulationMobileDto _body)
        {
            int TIMEOUT_SECONDS = _aipdata.timeOutReponse ?? 30;
            const string SCRIPT = "SERVICE D'ANNULATION D'UN TRANSFERT";
            string TAG_REJET = tag_erreur.CODE_REJET_DEMANDE_ANNULATION.ToString();

            try
            {
                // Garde précoce
                if (t is null)
                    return new GeneraleRetour { status = 404, detail = "La transaction est introuvable dans le système" };

                // Validation _body
                var validator = new QueryAnnulationMobileDtoValidator();
                var results = validator.Validate(_body);
                var invalidParams = new List<InvalidParam>();

                if (!results.IsValid)
                {
                    invalidParams.AddRange(results.Errors.Select(e => new InvalidParam { name = e.PropertyName, reason = e.ErrorMessage }));
                }

                // Raison autorisée (jeu utilisé pour demande d’annulation côté mobile)
                var raisonPossible = new[] { "DUPL", "AC03", "AM09", "FRAD", "SVNR" };
                var raison = (_body.raison ?? string.Empty).Trim().ToUpperInvariant();

                if (!raisonPossible.Contains(raison))
                {
                    invalidParams.Add(new InvalidParam
                    {
                        name = "raison",
                        reason = $"La valeur '{_body.raison}' n'est pas autorisée. Raisons possibles : {string.Join(", ", raisonPossible)}"
                    });
                }

                if (invalidParams.Count > 0)
                    return new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes", invalidParams = invalidParams };

                if (t.statut_general != STATUT_TRANSFERT.irrevocable)
                    return new GeneraleRetour { status = 403, detail = "Le statut de la transaction ne permet pas cette action" };

                // Enregistrement de la demande d’annulation
                var ann = new t_annulation_transfert
                {
                    endToEndId = t.endToEndId,
                    codeMembreParticipantPaye = t.codeMembreParticipantPaye,
                    codeMembreParticipantPayeur = t.codeMembreParticipantPayeur,
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    statut = statutAnnulation.initie,
                    raison = raison,
                    sensFlux = t.sensFlux
                };

                if (t.sensFlux == sensFlux.INTERNE)
                {
                    // Notif client + persist
                    var details = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new { montant = t.montant, emetteur = t.nomClientPayeur }));
                    var n = new t_notification
                    {
                        type = type_notif.ANNULATION_DEMANDEE.ToString(),
                        estCliquable = true,
                        compte = t.compteClientPaye,
                        idObject = t.endToEndId,
                        dateAction = DateTime.Now,
                        details = details
                    };

                    await _annulationTransfertRepo.AddAsync(ann);
                    await _notificationRepo.AddAsync(n);
                }
                else // SORTANT
                {
                    var send = new RequeteSendAnnulationDeTransfertDTo
                    {
                        endToEndId = t.endToEndId,
                        raison = raison,
                        codeMembreParticipantPaye = t.codeMembreParticipantPaye,
                        msgId = ann.msgId
                    };

                    var ret = await _envoieController.DemandeAnnulationTransfert(send);
                    if (!ret.operationResult)
                        return new GeneraleRetour { status = (int)ret._statuscode, detail = ret.messageResult };

                    await _annulationTransfertRepo.AddAsync(ann);

                    // Attente évènement PI (msgId ou e2e) avec tag de rejet d'annulation
                    var (ok, rep) = await WaitPiAsync(send.msgId, send.endToEndId, TAG_REJET);
                    if (!ok && rep is not null)
                    {
                        ann.statut = statutAnnulation.rejete;
                        ann.raisonRejetDemande = rep.desc_error;
                        await _annulationTransfertRepo.UpdateAsync(ann);

                        _logger.LogWarning("Annulation rejetée (EndToEndId={E2E}, MsgId={MsgId}) => {@Rep}", t.endToEndId, ann.msgId, rep);
                        return new GeneraleRetour { status = rep.status_code, detail = rep.desc_error };
                    }
                }

                // Mise à jour transfert
                t.annulationRaison = raison;
                t.annulationDate = DateTime.Now;
                t.annulationStatut = STATUT_TRANSFERT.initie;
                await _transfertRepo.UpdateAsync(t);

                return new GeneraleRetour { status = 200, detail = "Demande d'annulation envoyée avec succès" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} : {Message}", SCRIPT, ex.Message);
                return new GeneraleRetour { status = 500, detail = "Erreur interne lors de la demande d'annulation" };
            }
        }

        public async Task<GeneraleRetour> RetournerLesFonds(t_transfert data_transfert, string iddemande)
        {
            const string SCRIPT = "SERVICE DE RETOUR DE FONDS";
            int TIMEOUT_SECONDS = _aipdata.timeOutReponse ?? 30;
            string TAG_REJET = tag_erreur.CODE_RAISON_REJET.ToString();

            try
            {
                // --- Garde-fous statut/état
                if (data_transfert == null)
                    return new GeneraleRetour { status = 400, detail = "Transaction manquante" };

           
                if (data_transfert.statut_general != STATUT_TRANSFERT.irrevocable)
                    return new GeneraleRetour { status = 403, detail = "Le statut de la transaction ne permet pas de faire cette action" };

                bool b = await _retourFondrepo.HasValidReturnByEndToEndIdAsync(data_transfert.endToEndId);
                if (b == true)
                    return new GeneraleRetour { status = 403, detail = "Le retour de fonds pour ce transfert a déjà été effectué." };

                if (data_transfert.retourStatut == STATUT_TRANSFERT.initie )
                    return new GeneraleRetour { status = 403, detail = "Un retour de fonds a déjà été initié" };

                // --- Prépare l'objet retour
                var rf = new t_retour_fonds
                {
                    endToEndId = data_transfert.endToEndId,
                    raisonRetour = "CUST", // à paramétrer au besoin
                    montantRetourne = data_transfert.montant.ToString(),
                    etape = etapeRetourFond.initie,
                    sensFlux = (data_transfert.sensFlux == sensFlux.INTERNE) ? sensFlux.INTERNE : sensFlux.SORTANT,
                    IdOperationSib = Tools.Tools.GenerateAlphaNumeriquevalue(30),
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre)
                };

                // --- Persiste la ligne retour au plus tôt
                await _retourFondrepo.AddAsync(rf);

                // --- Màj du transfert (début de processus de retour)
                data_transfert.retourDate = DateTime.Now;
                data_transfert.retourEtape = etapeRetourFond.initie;
                data_transfert.retourStatut = STATUT_TRANSFERT.initie;
                data_transfert.retourStatutRaison = rf.raisonRetour;
                await _transfertRepo.UpdateAsync(data_transfert);

                // -------------------------- RETOUR INTERNE --------------------------
                if (data_transfert.sensFlux == sensFlux.INTERNE)
                {
                    string motif = string.IsNullOrWhiteSpace(data_transfert.motif)
                        ? "TRANSFERT INTERNE INTEROPERABLE"
                        : data_transfert.motif;
                    motif += " - RETOUR DE FONDS";

                    var data_retourfonds = new TransfertInterneBodyDto
                    {
                        msgId = rf.msgId,
                        endToEndId = rf.endToEndId,
                        identifiantTransaction = rf.IdOperationSib,
                        // Sens inverse:
                        compteClientPaye = data_transfert.compteClientPayeur,
                        nomClientPaye = data_transfert.nomClientPayeur,
                        montant = data_transfert.montant.ToString(),
                        compteClientPayeur = data_transfert.compteClientPaye,
                        nomClientPayeur = data_transfert.nomClientPaye,
                        motif = motif
                    };

                    var res_retour_interne = await _serviceAIF.OrdreDeTransfertInterne(data_retourfonds, iddemande);

                    if (!Tools.Tools.RetourIsSucces(res_retour_interne.status))
                    {
                        rf.etape = etapeRetourFond.rejete;
                        rf.statut = statutRetourFond.rejete;
                        rf.motifRejet = res_retour_interne.detail;
                        rf.codeRejet = res_retour_interne.data; // code raison rejet si fourni
                        await _retourFondrepo.UpdateAsync(rf);

                        data_transfert.retourEtape = etapeRetourFond.rejete;
                        data_transfert.retourStatut = STATUT_TRANSFERT.rejete;
                        data_transfert.retourStatutRaison = string.IsNullOrWhiteSpace(rf.codeRejet)
                                                            ? res_retour_interne.detail
                                                            : $"{rf.codeRejet}:{res_retour_interne.detail}";
                        await _transfertRepo.UpdateAsync(data_transfert);


                      return new GeneraleRetour { status = res_retour_interne.status, detail = rf.motifRejet };
                    }

                    // Succès interne
                    rf.etape = etapeRetourFond.valide;
                    rf.statut = statutRetourFond.irrevocable;
                    rf.dateHeureIrrevocabilite = DateTime.Now;
                    await _retourFondrepo.UpdateAsync(rf);

                    data_transfert.retourStatut = STATUT_TRANSFERT.irrevocable;
                    data_transfert.retourEtape = etapeRetourFond.valide;
                    data_transfert.statut_general = STATUT_TRANSFERT.desactive;
                    data_transfert.etape = ETAPE_TRANSFERT.DESACTVE;
                    data_transfert.annulationStatut = STATUT_TRANSFERT.irrevocable;


                    await _transfertRepo.UpdateAsync(data_transfert);

                    /// Changer le statut de toutes les demandes d'annulation en cours du transfert
                    await _annulationTransfertRepo.UpdateAllAnnulationsEnCours(data_transfert.endToEndId, statutAnnulation.accepte);

                    return new GeneraleRetour { status = 200, detail = "Retour de fonds interne exécuté avec succès" };
                }

                // -------------------------- RETOUR INTEROP --------------------------

                // 1) Réservation des fonds côté "payé" (on reprend l'argent à renvoyer)
                var bodyReservation = new ReservationFondsBodyDto
                {
                    numeroCompte = data_transfert.compteClientPaye,
                    montantReserve = rf.montantRetourne,
                    identifiantTransaction = rf.IdOperationSib
                };

                var res_reservation = await _serviceAIF.FaireUneReservationDeFonds(bodyReservation, iddemande);

                if (res_reservation.data != null)
                {
                    var data_reservation = JsonConvert.DeserializeObject<ReservationDto_AIF>(res_reservation.data);
                    rf.numEvenementReserv = data_reservation?.numeroEvenement;
                    rf.codeOperationReserv = data_reservation?.codeOperation;
                    rf.codeAgenceReserv = data_reservation?.codeAgenceTransaction;
                }

                if (!Tools.Tools.RetourIsSucces(res_reservation.status))
                {
                    rf.etape = etapeRetourFond.rejete;
                    rf.statut = statutRetourFond.rejete;
                    rf.motifRejet = res_reservation.detail;
                    await _retourFondrepo.UpdateAsync(rf);

                    data_transfert.retourEtape = etapeRetourFond.rejete;
                    data_transfert.retourStatut = STATUT_TRANSFERT.rejete;
                    data_transfert.retourStatutRaison = res_reservation.detail;
                    await _transfertRepo.UpdateAsync(data_transfert);

                    // Harmonise le message
                    res_reservation.detail = "Une erreur est survenue pendant la réservation de fonds";
                    return res_reservation;
                }

                await _retourFondrepo.UpdateAsync(rf);

                // 2) Demande de retour PI
                var send = new RequeteRetourDesFondsDto
                {
                    endToEndId = data_transfert.endToEndId,
                    raisonRetour = rf.raisonRetour,
                    montantRetourne = data_transfert.montant.ToString(),
                    msgId = rf.msgId
                };

                var retPI = await _envoieController.RetournerLesFonds(send);
                if (!retPI.operationResult)
                    return new GeneraleRetour { status = (int)retPI._statuscode, detail = retPI.messageResult };

                // 3) Attente d’événement PI (pattern Task.WhenAny conservé via helper)
                var (ok, rep) = await WaitPiAsync(send.msgId, send.endToEndId, TAG_REJET);
                if (!ok && rep is not null)
                {
                    // PI signale un rejet
                    rf.etape = etapeRetourFond.rejete;
                    rf.statut = statutRetourFond.rejete;
                    rf.motifRejet = rep.desc_error; // <- message PI
                    rf.codeRejet = rep.code_error;
                    await _retourFondrepo.UpdateAsync(rf);

                    data_transfert.retourEtape = etapeRetourFond.rejete;
                    data_transfert.retourStatut = STATUT_TRANSFERT.rejete;
                    data_transfert.retourStatutRaison = string.IsNullOrWhiteSpace(rep.code_error)
                                                        ? rep.desc_error
                                                        : $"{rep.code_error}:{rep.desc_error}";
                    await _transfertRepo.UpdateAsync(data_transfert);

                    _logger.LogWarning("Retour de fonds rejeté (EndToEndId={E2E}, MsgId={MsgId}) => {@Rep}",
                                        send.endToEndId, send.msgId, rep);
                    return new GeneraleRetour { status = rep.status_code, detail = rep.desc_error };
                }

                // Succès interop (event ok ou timeout considéré OK)
                rf.etape = etapeRetourFond.valide;
                rf.statut = statutRetourFond.irrevocable;
                await _retourFondrepo.UpdateAsync(rf);

                data_transfert.retourEtape = etapeRetourFond.valide;
                data_transfert.retourStatut = STATUT_TRANSFERT.irrevocable;
                data_transfert.annulationStatut = STATUT_TRANSFERT.irrevocable;

                await _transfertRepo.UpdateAsync(data_transfert);


                /// Changer le statut de toutes les demandes d'annulation en cours du transfert
                await _annulationTransfertRepo.UpdateAllAnnulationsEnCours(data_transfert.endToEndId, statutAnnulation.accepte);


                return new GeneraleRetour { status = 200, detail = "Retour de fonds effectué avec succès" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{SCRIPT} : erreur inattendue", SCRIPT);
                return new GeneraleRetour { status = 500 };
            }
        }

        public async Task<TransactionDto> CreateSouscriptionMobileDto(t_scheduled s, bool isClientPayeur)
        {

            var clientPays = isClientPayeur ? s.paysClientPaye : s.paysClientPayeur;
            var clientCodeParticipant = isClientPayeur ? s.codeMembreParticipantPaye : s.codeMembreParticipantPayeur;

            // Récupérations async (safe null)
            var participantTrouve = await _participantRepo.searchParticipant(clientCodeParticipant);
            var nomPays = await _datarepo.getItemDescriptionByCodeAndKey(code_datas.PAYS.ToString(), clientPays);

            return Tools.BuildAndMap.BuildSouscriptionDto(s, isClientPayeur, nomPays, participantTrouve?.nomOfficiel);

        }

        public async Task<GeneraleRetour> RecupererStatutDunTransfert(string endToend)
        {
            string _script = "SERVICE DE RECUPERATION DU STATUT D'UN TRANSFERT";

            try
            {


                var resDemandeTransfert = await _envoieController.RecupererStatutTransfert(endToend);

                if (!resDemandeTransfert.operationResult) // Si Envoi a PI Echouée
                    return (new GeneraleRetour { status = (int)resDemandeTransfert._statuscode, detail = resDemandeTransfert.messageResult });

                var requestId = _eventService.RegisterRequest(resDemandeTransfert.idoperation);
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto reponseTraiteDto = await TraiterRetourEvenement(message, tag_erreur.CODE_RAISON_REJET.ToString());

                if (!Tools.Tools.RetourIsSucces(reponseTraiteDto.status_code))
                    return (new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error });


                var ret = JsonConvert.DeserializeObject<ReponseRecuDemandeDeTransfert>(reponseTraiteDto.data);



                TransactionStatutDto d = new TransactionStatutDto
                {
                    codeRejet = ret.codeRaison,
                    statut = (ret.statutTransaction == "RJCT") ? STATUT_TRANSFERT.rejete.ToString() : STATUT_TRANSFERT.irrevocable.ToString(),
                    dateIrrevocabilite = (DateTime)ret.dateHeureIrrevocabilite

                };


                return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(d) });

            }
            catch (Exception ex)
            {
                _logger.LogError($"{_script} de transfert : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }



        public async Task<GeneraleRetour> ScheduledProcessus(string idSouscrption, string? iddemande)
        {

            const string SCRIPT = "ScheduledProcessus";


            // Charger la souscription
            var data_scheduled = await _scheduledrepo.searchById(idSouscrption);
            if (data_scheduled is null)
                return new GeneraleRetour { status = 404, detail = "La souscription n'existe pas dans le système" };

            // Vérifier si il y'a une prochaine execution.
            if (!data_scheduled.nextExecution.HasValue)
            {
                const string msg = "Aucune prochaine exécution planifiée.";
                return new GeneraleRetour { status = 403, detail = msg };
            }

            DateTime dateExecution = data_scheduled.nextExecution.Value;

            _logger.LogInformation(
                "dateExecution={DateExecution} frequence={Frequence} periodicite={Periodicite}",
                dateExecution,
                data_scheduled.frequence,
                data_scheduled.periodicite
            );

            // Calcule la prochaine exécution seulement si une fréquence est définie
            DateTime? dateNextExecution = null;
            if (data_scheduled.frequence != null)
            {
                // Si periodicite est nullable, prévoir une valeur par défaut (ex: 0 ou 1 selon votre règle métier)
                var periodicite = data_scheduled.periodicite ?? 0;

                dateNextExecution = Tools.Tools.CalculateNextExecutionDate(
                    dateExecution,
                    data_scheduled.frequence,
                    periodicite,
                    data_scheduled.dateFin
                );
            }


            var now = DateTime.Now;
            _logger.LogInformation("{Script} Next Execution {Id} => {Next}", SCRIPT, idSouscrption, dateExecution);


            // Autoriser seulement si la fenêtre est atteinte (>=) — pas juste la même date.
            if (now < dateExecution)
            {
                var msg = $"Traitement indisponible. Prochaine exécution le {dateExecution:dd/MM/yyyy à HH:mm}.";
                return new GeneraleRetour { status = 403, detail = msg };
            }


            if (Tools.Tools.canalEstCanalDemandePaiement(data_scheduled.canal))
                return await ScheduledProcessusDemandePaiement(data_scheduled, now, dateExecution, iddemande);
            else
                return await ScheduledProcessusTransfert(data_scheduled, now, dateExecution, iddemande);

        }


        public async Task<GeneraleRetour> ScheduledProcessusTransfert(t_scheduled data_scheduled, DateTime now, DateTime? dateNextExecution, string? iddemande)
        {
            const string SCRIPT = "ScheduledProcessusTransfert";

            try
            {
              
                

                // 3) Construire le transfert (factorisé)
                var t = MapFromScheduled(data_scheduled);

                // Correctifs de doublons/affectations
                // (évite les répétitions et erreurs comme dateNaissanceClientPayeur = typeCompteClientPayeur)
                t.dateHeureAcceptation = now; // unique set
                t.endToEndId = Tools.Tools.GenerateEndToEndCode(_aipdata); // sera gardé sauf identité réussie qui fournit un E2E

                await _transfertRepo.AddAsync(t);

                // 4) Vérifications préalables
                // 4.1 Alias payeur
                var data_payeur = await _aliasRepo.SearchAliasByAlias(data_scheduled.aliasClientPayeur);
                if (data_payeur is null)
                    return await RejectAndFinalizeAsync(t, data_scheduled, 404, "L'alias du payeur est introuvable dans notre système");

                // 4.2 Plafond TRAL (si applicable)
                if (t.typeCompteClientPayeur == "TRAL")
                {
                    var plafonds = await _datarepo.getDataInListByCode<ItemData>(code_datas.PLAFOND_TRAL.ToString());
                    var plafondTral = (double?)plafonds?.FirstOrDefault()?.Montant ?? 0d;

                    if (plafondTral > 0 && t.montant > plafondTral)
                    {
                        var lib = $"Un client TRAL (non identifié) ne peut pas dépasser {plafondTral:N0} CFA.";
                        return await RejectAndFinalizeAsync(t, data_scheduled, 403, lib);
                    }
                }

                // 5) Déterminer le type d’initiation (alias / iban / other)
                var typeInit = Tools.Tools.DeterminerTypeInitiation(t.aliasClientPaye, t.ibanClientPaye, t.otherClientPaye);

                // 5.1 Vérification d'identité (iban/other)
                DataPayeDto dataPayeFromLookup = new();
                if (typeInit is Type_initie.iban or Type_initie.other)
                {
                    var resId = await VerificationIdentite(t.codeMembreParticipantPaye, t.ibanClientPaye, t.otherClientPaye, iddemande);
                    if (!Tools.Tools.RetourIsSucces(resId.status))
                        return await RejectAndFinalizeAsync(t, data_scheduled, resId.status, resId.detail);

                    if (!TryDeserialize<DataPayeDto>(resId.data, out dataPayeFromLookup))
                        return await RejectAndFinalizeAsync(t, data_scheduled, 500, "Réponse d'identité invalide");
                }

                // 5.2 Recherche alias
                if (typeInit == Type_initie.alias)
                {
                    var resAlias = await RechercheAlias(t.aliasClientPaye, _aipdata.enabledInternalTransfer);

                    _logger.LogInformation("status recherche alias" + resAlias.status);

                    if (!Tools.Tools.RetourIsSucces(resAlias.status))
                        return await RejectAndFinalizeAsync(t, data_scheduled, resAlias.status, resAlias.detail);

                    if (!TryDeserialize<DataPayeDto>(resAlias.data, out dataPayeFromLookup))
                        return await RejectAndFinalizeAsync(t, data_scheduled, 500, "Réponse d'alias invalide");
                }

                _logger.LogInformation("Identité du payé est OK");


                // 6) Vérifier l’institution du bénéficiaire
                var participant = await _participantRepo.searchParticipant(dataPayeFromLookup.participant);
                if (participant is null || participant.statut == "DLTD")
                    return await RejectAndFinalizeAsync(t, data_scheduled, 404, "Institution du bénéficiaire inconnue dans PI");

                if (participant.statut == "DSBL")
                    return await RejectAndFinalizeAsync(t, data_scheduled, 403, "Institution du bénéficiaire momentanément indisponible");

                // 7) Hydrater les données PAYE depuis la lookup
                ApplyPayeData(t, dataPayeFromLookup, typeInit);

                // 8) Autorisation canal
                var autorise = await _transfertAutorepo.AvoirAutorisations(t.typeClientPayeur, dataPayeFromLookup.type, t.canalCommunication);
                if (!autorise)
                    return await RejectAndFinalizeAsync(t, data_scheduled, 403,
                        $"Transaction non autorisée (de {t.typeClientPayeur} vers {dataPayeFromLookup.type} via {t.canalCommunication}).");

                // 9) Plafond général
                var plafondOk = await _transfertPlafondrepo.VerifiePlafond(t.typeClientPayeur, dataPayeFromLookup.type, t.montant);
                if (!plafondOk)
                    return await RejectAndFinalizeAsync(t, data_scheduled, 403, "Le montant dépasse le plafond autorisé.");

                // 10) Comparer avec la donnée paye prévue dans la souscription

                string? jsonData = data_scheduled.data_paye?.RootElement.GetRawText();
                DataPayeDto? dataPayeInScheduled = !string.IsNullOrWhiteSpace(jsonData)
                    ? JsonConvert.DeserializeObject<DataPayeDto>(jsonData)
                    : null;



                _logger.LogInformation("Comparaison DataPaye: dataPayeFromLookup {@dataPayeFromLookup} VS  dataPayeInScheduled {@dataPayeInScheduled}", dataPayeFromLookup, dataPayeInScheduled);


                if (!DataPayeEquals(dataPayeFromLookup, dataPayeInScheduled))
                {

                    _logger.LogInformation("Attente de confirmation");

                    t.etape = ETAPE_TRANSFERT.ATTENTE_REPONSE_CLIENT;
                    await _transfertRepo.UpdateAsync(t);

                    data_scheduled.lastExecution = now;
                    data_scheduled.nextExecution = dateNextExecution;

                    JsonDocument jsonDocdatapaye = JsonDocument.Parse(JsonConvert.SerializeObject(dataPayeFromLookup));
                    data_scheduled.data_paye = jsonDocdatapaye;

                    await _scheduledrepo.UpdateAsync(data_scheduled);


                    var data = new
                    {
                        montant = t.montant,
                        emetteur = t.nomClientPaye,
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    JsonDocument details = JsonDocument.Parse(json);

                    t_notification n = new t_notification
                    {

                        type = type_notif.CONFIRME_TRANSFERT.ToString(),
                        estCliquable = true,
                        compte = t.compteClientPayeur,
                        idObject = t.endToEndId,
                        dateAction = DateTime.Now,
                        details = details
                    };

                    await _notificationRepo.AddAsync(n);


                    return new GeneraleRetour { status = 201, detail = "Opération en attente de confirmation du client" };
                }

                // 11) Confirmer
                t.etape = ETAPE_TRANSFERT.CONFIRME;
                await _transfertRepo.UpdateAsync(t);

                data_scheduled.lastExecution = now;
                JsonDocument jsonDoc_data_paye = JsonDocument.Parse(JsonConvert.SerializeObject(dataPayeFromLookup));
                data_scheduled.data_paye = jsonDoc_data_paye;
                await _scheduledrepo.UpdateAsync(data_scheduled);

                // NB: si la lookup a fourni un endToEndId “officiel”, gardez-le (sinon on conserve celui généré)
                if (!string.IsNullOrWhiteSpace(dataPayeFromLookup.endToEndId))
                    t.endToEndId = dataPayeFromLookup.endToEndId;

                // Générer msgId au plus tard (une seule fois)
                t.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                await _transfertRepo.UpdateAsync(t);

                // 12) Exécuter la confirmation
                _logger.LogInformation("Confirmation du transfert programmé");
                GeneraleRetour r = await ConfirmerTransfert(t, "");
                if (Tools.Tools.RetourIsSucces(r.status))
                {
                    data_scheduled.nextExecution = dateNextExecution;
                    await _scheduledrepo.UpdateAsync(data_scheduled);
                    return new GeneraleRetour { status = 200, detail = "Opération effectuée avec succès" };
                }
                else
                    return r;
        

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} : {Message}", SCRIPT, ex.Message);
                return new GeneraleRetour { status = 500, detail = "Erreur interne durant le traitement planifié" };
            }
        }


        public async Task<GeneraleRetour> ScheduledProcessusDemandePaiement(t_scheduled data_scheduled, DateTime now , DateTime? dateNextExecution, string? iddemande)
        {
            const string SCRIPT = "ScheduledProcessusDemandePaiement";

            try
            {
               
      

                // 3) Construire le transfert (factorisé)
                var t = MapFromScheduled(data_scheduled);

                // Correctifs de doublons/affectations
                // (évite les répétitions et erreurs comme dateNaissanceClientPayeur = typeCompteClientPayeur)
                t.dateHeureAcceptation = now; // unique set
                t.endToEndId = Tools.Tools.GenerateEndToEndCode(_aipdata); // sera gardé sauf identité réussie qui fournit un E2E

                await _transfertRepo.AddAsync(t);

                // 4) Vérifications préalables
                // 4.1 Alias payeur
                var data_demandeur = await _aliasRepo.SearchAliasByAlias(data_scheduled.aliasClientPaye);
                if (data_demandeur is null)
                    return await RejectAndFinalizeAsync(t, data_scheduled, 404, "L'alias du demandeur est introuvable dans notre système");

                // 4.2 Plafond TRAL (si applicable)
                if (t.typeCompteClientPayeur == "TRAL")
                {
                    var plafonds = await _datarepo.getDataInListByCode<ItemData>(code_datas.PLAFOND_TRAL.ToString());
                    var plafondTral = (double?)plafonds?.FirstOrDefault()?.Montant ?? 0d;

                    if (plafondTral > 0 && t.montant > plafondTral)
                    {
                        var lib = $"Un client TRAL (non identifié) ne peut pas dépasser {plafondTral:N0} CFA.";
                        return await RejectAndFinalizeAsync(t, data_scheduled, 403, lib);
                    }
                }

                // 5) Déterminer le type d’initiation (alias / iban / other)
                var typeInit = Tools.Tools.DeterminerTypeInitiation(t.aliasClientPayeur, t.ibanClientPayeur, t.otherClientPayeur);

                // 5.1 Vérification d'identité (iban/other)
                DataPayeDto dataPayeurFromLookup = new();
                if (typeInit is Type_initie.iban or Type_initie.other)
                {
                    var resId = await VerificationIdentite(t.codeMembreParticipantPayeur, t.ibanClientPayeur, t.otherClientPayeur, iddemande);
                    if (!Tools.Tools.RetourIsSucces(resId.status))
                        return await RejectAndFinalizeAsync(t, data_scheduled, resId.status, resId.detail);

                    if (!TryDeserialize<DataPayeDto>(resId.data, out dataPayeurFromLookup))
                        return await RejectAndFinalizeAsync(t, data_scheduled, 500, "Réponse d'identité invalide");
                }

                // 5.2 Recherche alias
                if (typeInit == Type_initie.alias)
                {
                    var resAlias = await RechercheAlias(t.aliasClientPayeur, _aipdata.enabledInternalTransfer);

                    _logger.LogInformation("status recherche alias" + resAlias.status);

                    if (!Tools.Tools.RetourIsSucces(resAlias.status))
                        return await RejectAndFinalizeAsync(t, data_scheduled, resAlias.status, resAlias.detail);

                    if (!TryDeserialize<DataPayeDto>(resAlias.data, out dataPayeurFromLookup))
                        return await RejectAndFinalizeAsync(t, data_scheduled, 500, "Réponse d'alias invalide");
                }

                _logger.LogInformation("Identité du payeur est OK");


                // 6) Vérifier l’institution du bénéficiaire
                var participant = await _participantRepo.searchParticipant(dataPayeurFromLookup.participant);
                if (participant is null || participant.statut == "DLTD")
                    return await RejectAndFinalizeAsync(t, data_scheduled, 404, "Institution du bénéficiaire inconnue dans PI");

                if (participant.statut == "DSBL")
                    return await RejectAndFinalizeAsync(t, data_scheduled, 403, "Institution du bénéficiaire momentanément indisponible");

                // 7) Hydrater les données PAYE depuis la lookup
                ApplyPayeData(t, dataPayeurFromLookup, typeInit);

                // 8) Autorisation canal
                var autorise = await _transfertAutorepo.AvoirAutorisations(dataPayeurFromLookup.type, t.typeClientPaye, t.canalCommunication);
                if (!autorise)
                    return await RejectAndFinalizeAsync(t, data_scheduled, 403,
                        $"Transaction non autorisée (de {dataPayeurFromLookup.type} vers {t.typeClientPaye} via {t.canalCommunication}).");

                // 9) Plafond général
                var plafondOk = await _transfertPlafondrepo.VerifiePlafond(dataPayeurFromLookup.type,t.typeClientPaye, t.montant);
                if (!plafondOk)
                    return await RejectAndFinalizeAsync(t, data_scheduled, 403, "Le montant dépasse le plafond autorisé.");

                // 10) Comparer avec la donnée paye prévue dans la souscription

                string? jsonData = data_scheduled.data_payeur?.RootElement.GetRawText();
                DataPayeDto? dataPayeurInScheduled = !string.IsNullOrWhiteSpace(jsonData)
                    ? JsonConvert.DeserializeObject<DataPayeDto>(jsonData)
                    : null;

            
                // 11) Confirmer
                t.etape = ETAPE_TRANSFERT.CONFIRME;
                await _transfertRepo.UpdateAsync(t);

                data_scheduled.lastExecution = now;
                JsonDocument jsonDoc_data_payeur = JsonDocument.Parse(JsonConvert.SerializeObject(dataPayeurInScheduled));
                data_scheduled.data_payeur = jsonDoc_data_payeur;
                await _scheduledrepo.UpdateAsync(data_scheduled);

                // NB: si la lookup a fourni un endToEndId “officiel”, gardez-le (sinon on conserve celui généré)
                if (!string.IsNullOrWhiteSpace(dataPayeurInScheduled.endToEndId))
                    t.endToEndId = dataPayeurInScheduled.endToEndId;

                // Générer msgId au plus tard (une seule fois)
                t.msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre);
                await _transfertRepo.UpdateAsync(t);

                // 12) Exécuter la confirmation
                _logger.LogInformation("Confirmation de la demande de paiement programmé");
                return await ConfirmerEtEnvoyerDemandePaiement(t, iddemande);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Script} : {Message}", SCRIPT, ex.Message);
                return new GeneraleRetour { status = 500, detail = "Erreur interne durant le traitement planifié" };
            }
        }



        /* --------------------- Helpers --------------------- */
        private async Task<(bool ok, ReponseTraiteDto? rep)> WaitPiAsync(string msgId, string endToEndId , string tagErreur)
        {


            int timeoutSeconds = _aipdata.timeOutReponse ?? 30;
            var reqMsg = _eventService.RegisterRequest(msgId);
            var reqE2E = _eventService.RegisterRequest(endToEndId);

            var tMsg = _eventService.GetTasks(reqMsg);
            var tE2E = _eventService.GetTasks(reqE2E);
            var tout = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            var winner = await Task.WhenAny(tMsg, tE2E, tout);
            if (winner == tout) return (true, null); // Considéré OK (timeout)

            var message = winner == tMsg ? await tMsg : await tE2E;
            var rep = await TraiterRetourEvenement(message, tagErreur);

            return (rep.status_code == 200, rep);
        }
        private t_transfert MapFromScheduled(t_scheduled s) => new t_transfert
        {
            // Métier
            statut_general = STATUT_TRANSFERT.initie,
            etape = ETAPE_TRANSFERT.INITIEE,
            sensFlux = sensFlux.SORTANT,
            typeTransaction = "PRMG",
            msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),

            // Références
            identifiantTransaction = s.txId,
            numeroDocumentReference = s.numeroDocumentReference,
            typeDocumentReference = s.typeDocumentReference,
            canalCommunication = s.canal,
            r_scheduled_id_fk = s.Id,

            // Montant / motif
            montant = s.montant,
            motif = s.motif,

            // Payeur
            aliasClientPayeur = s.aliasClientPayeur,
            nomClientPayeur = s.nomClientPayeur,
            dateNaissanceClientPayeur = s.dateNaissanceClientPayeur,
            typeCompteClientPayeur = s.typeCompteClientPayeur,
            villeClientPayeur = s.villeClientPayeur,
            villeNaissanceClientPayeur = s.villeNaissanceClientPayeur,
            adresseClientPayeur = s.adresseClientPayeur,
            ibanClientPayeur = s.ibanClientPayeur,
            otherClientPayeur = s.otherClientPayeur,
            paysClientPayeur = s.paysClientPayeur,
            paysNaissanceClientPayeur = s.paysNaissanceClientPayeur,
            deviseCompteClientPayeur = s.deviseCompteClientPayeur,
            typeClientPayeur = s.typeClientPayeur,
            compteClientPayeur = s.compteClientPayeur,
            codeMembreParticipantPayeur = s.codeMembreParticipantPayeur,
            systemeIdentificationClientPayeur = s.systemeIdentificationClientPayeur,
            numeroIdentificationClientPayeur = s.numeroIdentificationClientPayeur,
            numeroRCCMClientPayeur = s.numeroRCCMClientPayeur,
            latitudeClientPayeur = s.latitudeClientPayeur,
            longitudeClientPayeur = s.longitudeClientPayeur,

            // Payé (pré-rempli si fourni dans la souscription, sinon complété après lookup)
            aliasClientPaye = s.aliasClientPaye,
            typeCompteClientPaye = s.typeCompteClientPaye,
            ibanClientPaye = s.ibanClientPaye,
            otherClientPaye = s.otherClientPaye,
            codeMembreParticipantPaye = s.codeMembreParticipantPaye,
            nomClientPaye = s.nomClientPaye,
            dateNaissanceClientPaye = s.dateNaissanceClientPaye,
            villeClientPaye = s.villeClientPaye,
            villeNaissanceClientPaye = s.villeNaissanceClientPaye,
            adresseClientPaye = s.adresseClientPaye,
            paysClientPaye = s.paysClientPaye,
            paysNaissanceClientPaye = s.paysNaissanceClientPaye,
            deviseCompteClientPaye = s.deviseCompteClientPaye,
            typeClientPaye = s.typeClientPaye,
            compteClientPaye = s.compteClientPaye,
            systemeIdentificationClientPaye = s.systemeIdentificationClientPaye,
            numeroIdentificationClientPaye = s.numeroIdentificationClientPaye,
            numeroRCCMClientPaye = s.numeroRCCMClientPaye,
            latitudeClientPaye = s.latitudeClientPaye,
            longitudeClientPaye = s.longitudeClientPaye,
        };

        private async Task<GeneraleRetour> RejectAndFinalizeAsync(t_transfert t, t_scheduled sched, int status, string detail)
        {
            t.statut_general = STATUT_TRANSFERT.rejete;
            t.etape = ETAPE_TRANSFERT.REJETE;
            t.motifRejet = detail;
            await _transfertRepo.UpdateAsync(t);

            sched.lastExecution = DateTime.Now;
            await _scheduledrepo.UpdateAsync(sched);

            return new GeneraleRetour { status = status, detail = detail };
        }

        private static void ApplyPayeData(t_transfert t, DataPayeDto src, Type_initie? typeInit)
        {
            // Identifiants du payé
            t.compteClientPaye = !string.IsNullOrWhiteSpace(src.iban) ? src.iban : src.other;
            t.nomClientPaye = src.nom;
            t.dateNaissanceClientPaye = src.dateNaissance;
            t.typeCompteClientPaye = src.typeCompte;
            t.villeClientPaye = src.ville;
            t.villeNaissanceClientPaye = src.villeNaissance;
            t.adresseClientPaye = src.adresseComplete;
            t.ibanClientPaye = src.iban;
            t.otherClientPaye = src.other;
            t.paysClientPaye = src.paysResidence;
            t.paysNaissanceClientPaye = src.paysNaissance;
            t.codeMembreParticipantPaye = src.participant;
            t.typeClientPaye = src.type;

            t.systemeIdentificationClientPaye = null;
            t.numeroIdentificationClientPaye = null;
            if (t.typeCompteClientPaye != "TRAL")
            {
                t.systemeIdentificationClientPaye = src.systemeIdentification;
                t.numeroIdentificationClientPaye = src.numeroIdentification;
            }

            t.numeroRCCMClientPaye = t.typeClientPaye == "C" ? src.numeroRCCM : null;
            t.aliasClientPaye = (typeInit == Type_initie.alias) ? src.alias : null;

            // endToEndId de la lookup (si fourni) : gardé plus tard au besoin
        }

        private static bool TryDeserialize<T>(string json, out T result)
        {
            result = default!;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try { result = JsonConvert.DeserializeObject<T>(json)!; return result is not null; }
            catch { return false; }
        }

        private static bool DataPayeEquals(DataPayeDto a, DataPayeDto b)
        {





            if (a is null && b is null) return true;
            if (a is null || b is null) return false;

            // Choisissez les champs “contrats” pertinents pour la confirmation
            return string.Equals(a.participant, b.participant, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.type, b.type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.typeCompte, b.typeCompte, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.iban ?? a.other, b.iban ?? b.other, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.alias, b.alias, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.ville, b.ville, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.numeroIdentification, b.numeroIdentification, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.systemeIdentification, b.systemeIdentification, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.other, b.other, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.devise, b.devise, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.numeroRCCM, b.numeroRCCM, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.paysResidence, b.paysResidence, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.paysNaissance, b.paysNaissance, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.villeNaissance, b.villeNaissance, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.dateNaissance, b.dateNaissance, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.adresseComplete, b.adresseComplete, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.nom, b.nom, StringComparison.OrdinalIgnoreCase);



    }



        public async Task<double> CalculerLesFrais(DeterminerFraisDto _data)
        {
             return 0;
        }
    }
}
