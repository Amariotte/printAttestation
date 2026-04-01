using InteroperabiliteProject.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Controllers;
using InteroperabiliteProject.DtoAppMobile;
using InteroperabiliteProject.Event;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Text.Json;
using InteroperabiliteProject.Dtos.RevendicationAlias;
using InteroperabiliteProject.DtoAIP;
using ask.Dtos.RequestToSendDto;
using ask.Dtos.RequestToReceiveDto;


namespace InteroperabiliteProject.ServicceAIP
{
    public class ServiceAlias
    {

        private readonly ILogger<ServiceAlias> _logger;
        private readonly ServiceAIF _serviceAIF;
        private readonly ServiceMessagerie _serviceMessagerie;
        private readonly EnvoieController _envoieController;
        private readonly EventService _eventService;
        private readonly ServiceSecurity _securityService;
        private readonly AIPDATA _aipdata;
        private readonly PARAM_MESSAGE _paramdata;
        private readonly IemployeRepo _aliasRepo;
        private readonly IrevendicationRepo _revendicationRepo;
        private readonly IclientRepo _clientRepo;
        private readonly IcreationAliasRepo _creationAliasRepo;
        private readonly IotpRepo _otpRepo;
        private readonly IcompteRepo _compteRepo;
        private readonly IMapper _imapper;
        private readonly JsonSerializerSettings jsonSerializerSettings;
        private readonly SecurityConfig _securityconfig;


        public ServiceAlias(ILogger<ServiceAlias> logger, ServiceSecurity ServiceSecurity, IOptions<SecurityConfig> securityconfig, ServiceAIF serviceAIF, ServiceMessagerie serviceMessagerie, IOptions<AIPDATA> aipdata, IOptions<PARAM_MESSAGE> paramdata, EnvoieController envoieController, EventService eventService, IemployeRepo aliasRepo, IotpRepo otpRepo, IclientRepo clientRepo, IMapper imapper, IcompteRepo compteRepo, IcreationAliasRepo creationAliasRepo, IrevendicationRepo revendicationRepo)
        {
            _logger = logger;
            _serviceAIF = serviceAIF;
            _serviceMessagerie = serviceMessagerie;
            _envoieController = envoieController;
            _securityService = ServiceSecurity;
            _eventService = eventService;
            _aliasRepo = aliasRepo;
            _securityconfig = securityconfig.Value;
            _creationAliasRepo = creationAliasRepo;
            _revendicationRepo = revendicationRepo;
            _otpRepo = otpRepo;
            _clientRepo = clientRepo;
            _compteRepo = compteRepo;
            _aipdata = aipdata.Value;
            _paramdata = paramdata.Value;
            _imapper = imapper;

            jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

        }


           
        public async Task<GeneraleRetour> CreerUnAlias(string Plateforme, Model.t_client dataclient,string NumCompte, string TypeAlias, string? ValeurAlias, string? IdDemande)

        {

            try
            {



                _logger.LogInformation("Creation alias initier ===============================> SERVICE");


                List<InvalidParam> invalidParams = new List<InvalidParam>();


                string[] PlateformeAutorisees = { PlateformeAPI.MOBILE.ToString(), PlateformeAPI.BUSINESS.ToString(), PlateformeAPI.BACKOFFICE.ToString() };

                if (!Enum.TryParse<PlateformeAPI>(Plateforme, true, out var p))
                {
                    invalidParams.Add(new InvalidParam
                    {
                        name = "plateforme",
                        reason = $"La plateforme est invalide. Valeurs autorisées : {Tools.Tools.FormatListeOu(PlateformeAutorisees)}."
                    });
                }
              

                var (PossibleValue, possiblePersonnes) = p switch
                {
                    PlateformeAPI.MOBILE => (new[] { "SHID", "MBNO" }, new[] { "P", "C" }),
                    PlateformeAPI.BUSINESS => (new[] { "SHID", "MCOD" }, new[] { "B", "G", "C" }),
                    PlateformeAPI.BACKOFFICE => (new[] { "SHID", "MBNO", "MCOD" }, new[] { "P", "B", "G", "C" }),
                    _ => (Array.Empty<string>(), Array.Empty<string>())
                };

                if (!PossibleValue.Contains(TypeAlias))
                {
                    var liste = Tools.Tools.FormatListeOu(PossibleValue);
                    invalidParams.Add(new InvalidParam
                    {
                        name = "type",
                        reason = $"Le type d'alias doit être l’un de : {liste}."
                    });
                }
             
                if (TypeAlias == "MBNO")
                {

                    if (string.IsNullOrEmpty(ValeurAlias))
                        invalidParams.Add(new InvalidParam { name = "cle", reason = "Le numéro de téléphone est réquis" });
                    else if (!Tools.Tools.EstUnNumeroTelephone(ValeurAlias))
                        invalidParams.Add(new InvalidParam { name = "cle", reason = "Le numéro de téléphone saisi n'est pas valide" });
                }

                if (TypeAlias == "MCOD" && string.IsNullOrEmpty(ValeurAlias))
                    invalidParams.Add(new InvalidParam { name = "cle", reason = "Le numéro MCOD est obligatoire" });

                if (invalidParams.Count > 0)
                    return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes", invalidParams = invalidParams });

                
                
                
                var retReqAIF = await _serviceAIF.GetClientCompte(NumCompte, IdDemande);

                if (!retReqAIF.operationStatus)
                    return (new GeneraleRetour { status = retReqAIF.status, detail = retReqAIF.erreur });

                var ret_AIF = JsonConvert.DeserializeObject<Message>(retReqAIF.data);

                var retReqAIP = await _serviceAIF.GetEquivalenceClientCompte(ret_AIF);

                if (!retReqAIP.operationStatus)
                    return (new GeneraleRetour { status = retReqAIP.status, detail = retReqAIP.erreur });

                var ret_AIP = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationAIF>(retReqAIP.data);

                // Equivalence sur AIP


                if (!possiblePersonnes.Contains(ret_AIP.typeClient))
                    return (new GeneraleRetour { status = 403, detail = $"Impossible de créer un alias pour un client de type {ret_AIP.typeClient}" });

             
               if (ret_AIP.typeClient == "P" || ret_AIP.typeClient == "C") // Personnes Physique , un seul alias
                 {
                   t_alias _rech_alias = await _aliasRepo.SearchAliasByIdClient(dataclient.Id);
                     if (_rech_alias != null)
                      {
                            return (new GeneraleRetour { status = 403, detail = "La limite de création d'alias autorisé est atteint pour ce client" });
                     }
                 }

                CreationAlias creationAlias = new CreationAlias();

                creationAlias.participant = _aipdata.codemembre;
                creationAlias.typeCompte = ret_AIP.typeCompte;
                creationAlias.typeAlias = TypeAlias;

                creationAlias.idCreationAlias = Tools.Tools.GenerateIdAlias();

                creationAlias.categorieClient = ret_AIP.typeClient;
                creationAlias.nationaliteClient = ret_AIP.nationaliteClient;
                creationAlias.paysResidenceClient = ret_AIP.paysResidence;
                creationAlias.telephoneClient = ret_AIP.telephoneClient;
                creationAlias.dateOuvertureCompte = ret_AIP.dateOuvertureCompte;
                creationAlias.villeClient = ret_AIP.villeClient;
                creationAlias.iban = ret_AIP.ibanClient;
                creationAlias.adresseClient = ret_AIP.adresseComplete;
                creationAlias.photoClient = ret_AIP.photo;
                if (creationAlias.categorieClient == "P" || creationAlias.categorieClient == "C")
                {
                    
                    creationAlias.dateNaissanceClient = ret_AIP.dateNaissance;
                    creationAlias.genreClient = ret_AIP.genreClient;
                    creationAlias.nomMere = ret_AIP.nomMere;
                    creationAlias.paysNaissanceClient = ret_AIP.paysNaissance;
                    creationAlias.villeNaissanceClient = ret_AIP.villeNaissance;
                    creationAlias.nomClient = ret_AIP.nomClient;
                }

                if (TypeAlias != "SHID")
                    creationAlias.valeurAlias = ValeurAlias;

                if (creationAlias.categorieClient == "B" || creationAlias.categorieClient == "G")
                {
                    creationAlias.raisonSociale = ret_AIP.nomClient;
                    creationAlias.denominationSociale = ret_AIP.sigleClient; ;
                    creationAlias.nomClient = ret_AIP.sigleClient;
                }

                if (creationAlias.categorieClient == "C" && !string.IsNullOrEmpty(ret_AIP.numeroRCCMClient))
                    creationAlias.identificationRccm = ret_AIP.numeroRCCMClient;

                // if (creationAlias.categorieClient == "B")
                //     creationAlias.preConfirmation = preConfirmation;

                switch (ret_AIP.systemeIdentification)
                {
                    case "CCPT": //Passeport
                        creationAlias.numeroPasseport = ret_AIP.numeroIdentification;
                        break;
                    case "NIDN":  //Carte Nationale
                        creationAlias.identificationNationaleClient = ret_AIP.numeroIdentification;
                        break;
                    case "TXID":  // Identification fiscale
                        creationAlias.identificationFiscale = ret_AIP.numeroIdentification;
                        break;
                }


                // Besoin de confirmation du numéro du telephone
                if ((TypeAlias == "MBNO") && (dataclient.telephone != ValeurAlias))
                {

                    JsonDocument data_client_bq = JsonDocument.Parse(JsonConvert.SerializeObject(ret_AIF));
                    t_creation_alias new_creation = _imapper.Map<t_creation_alias>(creationAlias);

                    new_creation.data_client_bq = data_client_bq;
                    new_creation.r_client_id = dataclient.Id;

                    // Marquer les anciens comme SUPPRIME
                    await _creationAliasRepo.DeleteAllByIdClientAndTelCreation(dataclient.Id,new_creation.valeurAlias);

                    await _creationAliasRepo.AddAsync(new_creation);

                    t_otp o = await _otpRepo.genererOtp(dataclient.Id,new_creation.idCreationAlias, type_otp.CREATION_ALIAS, _paramdata.sms.validite_otp ?? 6);
                 
                    await _serviceMessagerie.sendMessageAuClient(type_modele.CONFIRMATION_ALIAS_MBNO, new_creation.valeurAlias, dataclient, o);


                    RepCreateAliasDto aliasAConfirmer = new RepCreateAliasDto
                    {
                        cle = new_creation.valeurAlias,
                        shid = "",
                        type = new_creation.typeAlias,
                        compte = Tools.Tools.TransformeIbanEnNumCompte(new_creation.iban),
                        pays = new_creation.paysResidenceClient,
                    };


                    return (new GeneraleRetour { status = 202, data = JsonConvert.SerializeObject(aliasAConfirmer) });
                }

                // Appel au service pour créer l'alias
                var retReq = await _envoieController.DemandeDeCreationAlias(creationAlias);

                if (!retReq.operationResult)
                    return (new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.messageResult });


                // Enregistrer la requête et gérer l'événement
                var requestId = _eventService.RegisterRequest(retReq.idoperation);
              
                var message = await _eventService.GetTasks(requestId);


                ReponseTraiteDto reponseTraiteDto = await TraiterRetourEvenement(message);

                if (!(reponseTraiteDto.status_code == 200))
                {
                    return (new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error });
                }


                var ret1 = JsonConvert.DeserializeObject<ReponseDemandeCreationAlias>(reponseTraiteDto.data);

                if (ret1 != null && ret1.statut == "SUCCES")
                {
                    // Enregistrer l'alias en base de données


                    string aliasQr = ret1.shid;
                    if (string.IsNullOrEmpty(aliasQr))
                        aliasQr = ret1.alias;

                    if (string.IsNullOrEmpty(aliasQr))
                        aliasQr = creationAlias.valeurAlias;


                    // Création du compte du client si il n'existe pas dans le système
                    t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(NumCompte, dataclient.Id);
                    if (data_compte == null)
                    {
                        data_compte = _imapper.Map<t_compte>(ret_AIF.compte);
                        data_compte.r_client_id = dataclient.Id;
                        data_compte.type = type.Iban;
                        await _compteRepo.AddAsync(data_compte);
                    }
                    else
                    {
                        data_compte.type = type.Iban;
                        data_compte.ibanOrOther = ret_AIF.compte.iban;
                        data_compte.codeAgence = ret_AIF.compte.codeAgence;
                        data_compte.nomAgence = ret_AIF.compte.nomAgence;
                        data_compte.numeroCompte = ret_AIF.compte.numeroCompte;
                        data_compte.typeCompte = ret_AIF.compte.typeCompte;
                        data_compte.cleRib = ret_AIF.compte.cleRib;
                        data_compte.intituleCompte = ret_AIF.compte.intituleCompte;
                        data_compte.racineCompte = ret_AIF.compte.racineCompte;
                        data_compte.titulaireCompte = ret_AIF.compte.titulaireCompte;
                        data_compte.codeDeviseCompte = ret_AIF.compte.codeDeviseCompte;
                        data_compte.deviseCompte = ret_AIF.compte.deviseCompte;
                        data_compte.sensCompte = ret_AIF.compte.sensCompte;
                        data_compte.taxeCompte = ret_AIF.compte.taxeCompte;
                        data_compte.instanceFermetureCompte = ret_AIF.compte.instanceFermetureCompte;
                        data_compte.FermetureCompte = ret_AIF.compte.FermetureCompte;
                        data_compte.dateOuverture = ret_AIF.compte.dateOuverture;
                        data_compte.dateFermeture = ret_AIF.compte.dateFermeture;
                        data_compte.dateInstanceFermetureCompte = ret_AIF.compte.dateInstanceFermetureCompte;
                        await _compteRepo.UpdateAsync(data_compte);
                    }

                    string codecanal = "731";
                    if(creationAlias.categorieClient != null && creationAlias.categorieClient == "C" )
                    {
                        codecanal = "000";
                    }

                    t_alias _new_alias = new t_alias

                    {
                        dateNaissance = creationAlias.dateNaissanceClient,
                        villeNaissance = creationAlias.villeNaissanceClient,
                        other = creationAlias.other,
                        identificationFiscale = creationAlias.identificationFiscale,
                        nomClient = creationAlias.nomClient,
                        idCreationAlias = creationAlias.idCreationAlias,
                        dateOuvertureCompte = creationAlias.dateOuvertureCompte,
                        participant = _aipdata.codemembre,
                        iban = creationAlias.iban,
                        identificationNationaleClient = creationAlias.identificationNationaleClient,
                        typeCompte = creationAlias.typeCompte,
                        categorie = creationAlias.categorieClient,
                        paysResidence = creationAlias.paysResidenceClient,
                        genre = creationAlias.genreClient,
                        nationalite = creationAlias.nationaliteClient,
                        telephone = creationAlias.telephoneClient,
                        ville = creationAlias.villeClient,
                        PaysNaissance = creationAlias.paysNaissanceClient,
                        denominationSociale = creationAlias.denominationSociale,
                        raisonSociale = creationAlias.raisonSociale,
                        numeroPasseport = creationAlias.numeroPasseport,
                        typeAlias = creationAlias.typeAlias,
                        adresse = creationAlias.adresseClient,
                        photo = creationAlias.photoClient,
                        nomMere = creationAlias.nomMere,
                        shid = ret1.shid,
                        dateCreationAlias = ret1.dateCreation,
                        r_client_id_fk = dataclient.Id,
                        ibanOrOther = data_compte.ibanOrOther,
                        valeurAlias = (string.IsNullOrEmpty(ret1.alias)) ? creationAlias.valeurAlias : ret1.alias,
                        codeQr = Tools.Tools.GenerationQR(aliasQr, _aipdata.codepays, codecanal, "", "")
                    };


                    await _aliasRepo.AddAsync(_new_alias);
                    return (new GeneraleRetour { status = 200, detail = "Alias créé avec succès", data = JsonConvert.SerializeObject(_new_alias) });

                }
                else
                {
                    string _msg = ret1.raisonRejet;
                    if (!string.IsNullOrEmpty(ret1.informationsAdditionnelles))
                        if (ret1.informationsAdditionnelles.Contains("Vous ne pouvez pas créer plus d'un alias dans le système"))
                            _msg = ret1.informationsAdditionnelles;
                        else
                            _msg += ' ' + ret1.informationsAdditionnelles;


                    return (new GeneraleRetour { status = 400, detail = _msg });
                }

            }

            catch (Exception e)
            {
                _logger.LogError("CreateAlias", e.Message);
                return (new GeneraleRetour { status = 500 });
            }

        }


        public async Task<GeneraleRetour> ConfirmerLaCreationDunAlias(Model.t_client dataclient, string cle, QueryConfirmationOtpDto dt,string? IdDemande)

        {

            string _script = "Service confirmation de creation d'alias";

            try
            {

                List<InvalidParam> invalidParams = new List<InvalidParam>();

                if (string.IsNullOrEmpty(cle))
                {
                    invalidParams.Add(new InvalidParam { name = "cle", reason = $"le numéro du téléphone est requis" });
                    return (new GeneraleRetour { status = 400, detail = "le numéro du téléphone est requis", invalidParams = invalidParams });
                }

                if (string.IsNullOrEmpty(dt.otp))
                {
                    invalidParams.Add(new InvalidParam { name = "otp", reason = "OTP de confirmation requis" });
                    return (new GeneraleRetour { status = 400, detail = "L'OTP de confirmation est requis", invalidParams = invalidParams });
                }


                t_creation_alias t = await _creationAliasRepo.SearchByIdClientAndTelCreation(dataclient.Id,cle);

                if (t == null)
                    return (new GeneraleRetour { status = 404, detail = "La demande de création introuvable dans le système" });


                if (t.bConfirme == true)
                    return (new GeneraleRetour { status = 403, detail = "La demande de création est déjà confirmée" });




                int res_otp = await _otpRepo.verifieOtp(dataclient.Id,dt.otp, type_otp.CREATION_ALIAS, t.idCreationAlias);
                switch (res_otp)
                {
                    case 0:
                        return (new GeneraleRetour { status = 403, detail = "OTP Expiré" });
                    case -1:
                        return (new GeneraleRetour { status = 403, detail = "OTP Invalide" });
                    case 1:
                        break;
                };

                Message ret_AIF = JsonConvert.DeserializeObject<Message>(t.data_client_bq.RootElement.ToString());

                


                    if (t.categorieClient == "P" || t.categorieClient == "C") // Personnes Physique , un seul alias
                    {
                        t_alias _rech_alias = await _aliasRepo.SearchAliasByIdClient(dataclient.Id);
                        if (_rech_alias != null)
                        {
                            return (new GeneraleRetour { status = 403, detail = "La limite de création d'alias autorisé est atteint pour ce client" });
                        }
                    }
               

                CreationAlias creationAlias = new CreationAlias();

                creationAlias.participant = _aipdata.codemembre;
                creationAlias.typeCompte = t.typeCompte;
                creationAlias.typeAlias = t.typeAlias;

                creationAlias.idCreationAlias = t.idCreationAlias;
                creationAlias.categorieClient = t.categorieClient;
                creationAlias.nationaliteClient = t.nationaliteClient;
                creationAlias.paysResidenceClient = t.paysResidenceClient;
                creationAlias.telephoneClient = t.telephoneClient;
                creationAlias.dateOuvertureCompte = t.dateOuvertureCompte;
                creationAlias.villeClient = t.villeClient;
                creationAlias.iban = t.iban;
                creationAlias.adresseClient = t.adresseClient;
                creationAlias.photoClient = t.photoClient;

                if (creationAlias.categorieClient == "P" || creationAlias.categorieClient == "C")
                {
                    creationAlias.dateNaissanceClient = t.dateNaissanceClient;
                    creationAlias.genreClient = t.genreClient;
                    creationAlias.nomMere = t.nomMere;
                    creationAlias.paysNaissanceClient = t.paysNaissanceClient;
                    creationAlias.villeNaissanceClient = t.villeNaissanceClient;
                    creationAlias.nomClient = t.nomClient;
                }

                if (t.typeAlias != "SHID")
                    creationAlias.valeurAlias = t.valeurAlias;

                if (creationAlias.categorieClient == "B" || creationAlias.categorieClient == "G")
                {
                    creationAlias.raisonSociale = t.raisonSociale;
                    creationAlias.denominationSociale = t.denominationSociale;
                    creationAlias.nomClient = t.nomClient;

                }

                if (creationAlias.categorieClient == "C")
                    creationAlias.identificationRccm = t.identificationRccm;

                // if (creationAlias.categorieClient == "B")
                //     creationAlias.preConfirmation = preConfirmation;


                //Passeport
                creationAlias.numeroPasseport = t.numeroPasseport;
                //Carte Nationale
                creationAlias.identificationNationaleClient = t.identificationNationaleClient;
                // Identification fiscale
                creationAlias.identificationFiscale = t.identificationFiscale;

                t.bConfirme = true;
                await _creationAliasRepo.UpdateAsync(t);


                // Appel au service pour créer l'alias
                var retReq = await _envoieController.DemandeDeCreationAlias(creationAlias);

                if (retReq == null)
                    return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes" });


                if (!retReq.operationResult)
                    return (new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.messageResult });


                // Enregistrer la requête et gérer l'événement
                var requestId = _eventService.RegisterRequest(retReq.idoperation);
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto resEvent = await TraiterRetourEvenement(message);

                if (!(resEvent.status_code == 200))
                    return (new GeneraleRetour { status = resEvent.status_code, detail = resEvent.desc_error });


                var resDemande = JsonConvert.DeserializeObject<ReponseDemandeCreationAlias>(resEvent.data);

                if (resDemande == null)
                    return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes" });


                if (resDemande.statut != "SUCCES")
                {
                    string _msg = resDemande.raisonRejet;
                    if (!string.IsNullOrEmpty(resDemande.informationsAdditionnelles))
                        if (resDemande.informationsAdditionnelles.Contains("Vous ne pouvez pas créer plus d'un alias dans le système"))
                            _msg = resDemande.informationsAdditionnelles;
                        else
                            _msg += ' ' + resDemande.informationsAdditionnelles;

                    return (new GeneraleRetour { status = 403, detail = _msg });
                }


                string aliasQr = resDemande.shid;
                if (string.IsNullOrEmpty(aliasQr))
                    aliasQr = resDemande.alias;

                if (string.IsNullOrEmpty(aliasQr))
                    aliasQr = creationAlias.valeurAlias;


              
                // Création du compte du client si il n'existe pas dans le système
                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(ret_AIF.compte.iban, dataclient.Id);
                if (data_compte == null)
                {
                    data_compte = _imapper.Map<t_compte>(ret_AIF.compte);
                    data_compte.r_client_id = dataclient.Id;
                    data_compte.type = type.Iban;
                    await _compteRepo.AddAsync(data_compte);
                }

                string codecanal = "731";
                if (creationAlias.categorieClient != null && creationAlias.categorieClient == "C")
                {
                    codecanal = "000";
                }
                // Enregistrer l'alias en base de données
                t_alias _new_alias = new t_alias

                {
                    dateNaissance = creationAlias.dateNaissanceClient,
                    villeNaissance = creationAlias.villeNaissanceClient,
                    other = creationAlias.other,
                    identificationFiscale = creationAlias.identificationFiscale,
                    nomClient = creationAlias.nomClient,
                    idCreationAlias = creationAlias.idCreationAlias,
                    dateOuvertureCompte = creationAlias.dateOuvertureCompte,
                    participant = _aipdata.codemembre,
                    iban = creationAlias.iban,
                    identificationNationaleClient = creationAlias.identificationNationaleClient,
                    typeCompte = creationAlias.typeCompte,
                    categorie = creationAlias.categorieClient,
                    paysResidence = creationAlias.paysResidenceClient,
                    genre = creationAlias.genreClient,
                    nationalite = creationAlias.nationaliteClient,
                    telephone = creationAlias.telephoneClient,
                    ville = creationAlias.villeClient,
                    PaysNaissance = creationAlias.paysNaissanceClient,
                    denominationSociale = creationAlias.denominationSociale,
                    raisonSociale = creationAlias.raisonSociale,
                    numeroPasseport = creationAlias.numeroPasseport,
                    typeAlias = creationAlias.typeAlias,
                    adresse = creationAlias.adresseClient,
                    photo = creationAlias.photoClient,
                    nomMere = creationAlias.nomMere,
                    shid = resDemande.shid,
                    dateCreationAlias = resDemande.dateCreation,
                    r_client_id_fk = dataclient.Id,
                    ibanOrOther = data_compte.ibanOrOther,
                    valeurAlias = (string.IsNullOrEmpty(resDemande.alias)) ? creationAlias.valeurAlias : resDemande.alias,
                    codeQr = Tools.Tools.GenerationQR(aliasQr, _aipdata.codepays, codecanal, "", "")
                };


                await _aliasRepo.AddAsync(_new_alias);
                return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(_new_alias) });

            }

            catch (Exception e)
            {
                _logger.LogError(_script, e.Message);
                return (new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." });
            }

        }

        public async Task<GeneraleRetour> DeleteAlias( int ClientId,string alias, string raison,string? IdDemande)
        {

            /*
             rq.raison 1-DEMANDE_CLIENT , 2-FERMETURE_COMPTE_CLIENT
             */
            try
            {
                if (string.IsNullOrEmpty(alias))
                    return (new GeneraleRetour { status = 400, detail = "Format du message invalide" });

                RequeteDemandeDeSuppressionAlias Rqalias = new RequeteDemandeDeSuppressionAlias
                {
                    alias = alias,
                    raisonSuppression = raison
                };

                _logger.LogInformation($"[Idclient de l'alias a supprimer] IDCLIENT =================> {ClientId}");
                _logger.LogInformation($"[Alias a supprimer] ALIAS =================> {Rqalias.alias}");

                t_alias aliasSearch = await _aliasRepo.SearchAliasByIdClientAndAlias(ClientId,Rqalias.alias);


                if (aliasSearch == null)
                    return (new GeneraleRetour { status = 404, detail = "L'alias est introuvable dans le système" });


                if (aliasSearch.typeAlias == "MBNO" && !string.IsNullOrEmpty(aliasSearch.shid))
                {
                    RequeteDemandeDeSuppressionAlias RqDeleteAliasMbno = new RequeteDemandeDeSuppressionAlias
                    {
                        alias = aliasSearch.valeurAlias,
                        raisonSuppression = Rqalias.raisonSuppression
                    };

                    // Si Type Alias est MBNO ===> Supprimer le MBNO avant le SHID

                    Rqalias.alias = aliasSearch.shid;

                    var resDemande = await _envoieController.DemandeDeSuppressionAlias(RqDeleteAliasMbno);

                    if (resDemande == null)
                        return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes" });

                   if (!resDemande.operationResult)
                        return (new GeneraleRetour { status = (int)resDemande._statuscode, detail = resDemande.messageResult });

                    var requestMbnoId = _eventService.RegisterRequest(resDemande.idoperation);
                    var messageMbno = await _eventService.GetTasks(requestMbnoId);

                    ReponseTraiteDto resMbnoTraiteDto = await TraiterRetourEvenement(messageMbno);

                    if (!Tools.Tools.RetourIsSucces(resMbnoTraiteDto.status_code))
                        return (new GeneraleRetour { status = resMbnoTraiteDto.status_code, detail = resMbnoTraiteDto.desc_error });

                    var rep = JsonConvert.DeserializeObject<ReponseADemandeDeSuppressionAlias>(resMbnoTraiteDto.data);

                    if (rep == null)
                        return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes" });

                    if (rep.statut != "SUCCES")
                    {

                        string _msg = rep.raisonRejet;
                        if (!string.IsNullOrEmpty(rep.informationsAdditionnelles))
                            _msg += ' ' + rep.informationsAdditionnelles;
                        return (new GeneraleRetour { status = 403, detail = _msg });
                    }
                   //*****************************************SI REPONSE EST OK ALORS LANCE L'EVENEMENT***********************************
                }


                // Suppression du SHID
                var retReq = await _envoieController.DemandeDeSuppressionAlias(Rqalias);

                if (!retReq.operationResult)
                    return (new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.messageResult });


                var requestId = _eventService.RegisterRequest(retReq.idoperation);
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto reponseTraiteDto = await TraiterRetourEvenement(message);

                if (!(reponseTraiteDto.status_code == 200))
                    return (new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error });

                var ret = JsonConvert.DeserializeObject<ReponseADemandeDeSuppressionAlias>(reponseTraiteDto.data);
                if (ret == null || ret.statut != "SUCCES")
                {
                    string _msg = ret.raisonRejet;
                    if (!string.IsNullOrEmpty(ret.informationsAdditionnelles))
                        _msg += ' ' + ret.informationsAdditionnelles;
                    return (new GeneraleRetour { status = 400, detail = _msg });

                }

                await _aliasRepo.DeleteAlias(Rqalias.alias);



                return (new GeneraleRetour { status = 204, detail = "Alias supprimé avec succès" });
            }

            catch (Exception ex)
            {
                
                return (new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." });
            }
        }


        public async Task<GeneraleRetour> UpdateAlias(QueryModificationAliasClientDto rq)
        {

            //*****************************Verification du corps envoyé ******************************************
            if (string.IsNullOrEmpty(rq.alias))
                return (new GeneraleRetour { status = 400, detail = "L'alias est requis" });

            try
            {

                var retReq = await _envoieController.DemandeDemodificationAlias(rq);
                if (!retReq.operationResult)
                    return (new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.messageResult });


                var requestId = _eventService.RegisterRequest(retReq.idoperation);
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto reponseTraiteDto = await TraiterRetourEvenement(message);

                if (!(reponseTraiteDto.status_code == 200))
                {
                    return (new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error });
                }

                var resDemande = JsonConvert.DeserializeObject<ReponseADemandeDeModificationAlias>(reponseTraiteDto.data);

                if (resDemande == null)
                    return (new GeneraleRetour { status = 400, detail = "Les données ne sont pas conformes" });

                if (resDemande.statut != "SUCCES")
                {
                    string _msg = resDemande.raisonRejet;
                    if (!string.IsNullOrEmpty(resDemande.informationsAdditionnelles))
                        _msg += ' ' + resDemande.informationsAdditionnelles;

                    return (new GeneraleRetour { status = 403, detail = _msg });

                }

                t_alias _rech_alias_upd = await _aliasRepo.SearchAliasByAlias(rq.alias);

                if (_rech_alias_upd != null)
                {
                    if (!string.IsNullOrEmpty(rq.paysResidenceClient)) _rech_alias_upd.paysResidence = rq.paysResidenceClient;
                    if (!string.IsNullOrEmpty(rq.telephoneClient)) _rech_alias_upd.telephone = rq.telephoneClient;
                    if (!string.IsNullOrEmpty(rq.emailClient)) _rech_alias_upd.email = rq.emailClient;
                    if (!string.IsNullOrEmpty(rq.villeClient)) _rech_alias_upd.ville = rq.villeClient;
                    if (!string.IsNullOrEmpty(rq.codePostalClient)) _rech_alias_upd.codePostale = rq.codePostalClient;
                    if (!string.IsNullOrEmpty(rq.numeroPasseport)) _rech_alias_upd.numeroPasseport = rq.numeroPasseport;
                    if (!string.IsNullOrEmpty(rq.adresseClient)) _rech_alias_upd.adresse = rq.adresseClient;
                    if (!string.IsNullOrEmpty(rq.denominationSociale)) _rech_alias_upd.denominationSociale = rq.denominationSociale;
                    if (!string.IsNullOrEmpty(rq.photoClient)) _rech_alias_upd.photo = rq.photoClient;
                    if (!rq.preConfirmation != null) _rech_alias_upd.preConfirmation = rq.preConfirmation;

                    _rech_alias_upd.dateModificationAlias = resDemande.dateModification;
                    await _aliasRepo.UpdateAsync(_rech_alias_upd);
                }

                return (new GeneraleRetour { status = 200, detail = "Alias modifié avec succès", data = JsonConvert.SerializeObject(_rech_alias_upd) });

            }
            catch (Exception ex)
            {
                return (new GeneraleRetour { status = 500, detail = "Une erreur inattendue a empêché le serveur de traiter la requête." });
            }


        }


        public async Task<GeneraleRetour> RevendiquerUnAlias(string numero, string compte, string? IdDemande)

        {

            try
            {

                if (string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(compte))
                    return (new GeneraleRetour { status = 400, detail = "Les données envoyées ne sont pas conformes" });



                var retReqAIF = await _serviceAIF.GetClientCompte(compte, IdDemande);

                if (!retReqAIF.operationStatus)
                    return (new GeneraleRetour { status = retReqAIF.status, detail = retReqAIF.erreur });


                var retReq = await _envoieController.RevendiquerAlias(numero);

                if (!retReq.operationResult)
                    return (new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.erreur });

                var ret_AIF = JsonConvert.DeserializeObject<Message>(retReqAIF.data);


                // Enregistrer la requête et gérer l'événement
                var requestId = _eventService.RegisterRequest(retReq.idoperation);
                var message = await _eventService.GetTasks(requestId);

                ReponseTraiteDto reponseTraiteDto = await TraiterRetourEvenement(message);

                if (!(reponseTraiteDto.status_code == 200))
                {
                    return (new GeneraleRetour { status = reponseTraiteDto.status_code, detail = reponseTraiteDto.desc_error });
                }

                var retRev = JsonConvert.DeserializeObject<ReponseDemandeRevendicationDTO>(reponseTraiteDto.data);

                if (retRev == null)
                    return (new GeneraleRetour { status = 400, detail = "Les données envoyées ne sont pas conformes" });


                t_revendication rv = new t_revendication
                {
                    alias = numero,
                    statut = retRev.statut == "INITIEE" ? statut_revendication.INITIE : statut_revendication.REJETEE,
                    sensFlux = sensFlux.SORTANT,
                    dateDemande = DateTime.Now,
                    pspDetenteur = retRev.detenteur,
                    pspRevendicateur = retRev.revendicateur,
                    idRevendicationPi = retRev.identifiantRevendication,
                    dateCreation = retRev.dateCreation,
                    dateModification = retRev.dateModification,
                    raisonRejet = retRev.raisonRejet,
                    informationsAdditionnelles = retRev.informationsAdditionnelles,
                    compte = ret_AIF.compte.iban
                };

                await _revendicationRepo.AddAsync(rv);


                if (retRev.statut == "ECHEC")
                {
                    string _msg = retRev.raisonRejet;
                    if (!string.IsNullOrEmpty(retRev.informationsAdditionnelles))
                        _msg += ' ' + retRev.informationsAdditionnelles;

                    return (new GeneraleRetour { status = 400, detail = _msg });
                }
                else
                {
                    return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(rv) });
                }

            }

            catch (Exception e)
            {
                _logger.LogError("RevendiquerUnAlias", e.Message);
                return (new GeneraleRetour { status = 500 });
            }

        }

        public async Task<GeneraleRetour> RepondreAUneRevendication(int clientID,string idRevendicationPI, bool decision,t_client dataclient, string auteur = "CLIENT")

        {

            try
            {

                if (string.IsNullOrEmpty(idRevendicationPI))
                    return (new GeneraleRetour { status = 400, detail = "L'identifiant PI de la revendication est obligatoire" });


                t_revendication t_rev = await _revendicationRepo.SearchRevendicationByIdPI(idRevendicationPI);

                if (t_rev == null)
                    return (new GeneraleRetour { status = 404, detail = "La revendication est introuvable dans le système" });


                if (t_rev.statut != statut_revendication.INITIE)
                    return (new GeneraleRetour { status = 403, detail = "La revendication est terminée ou cloturée" });


                if (decision == true)
                {
                    AcceptationRevendicationDTO _body = new AcceptationRevendicationDTO
                    {
                        identifiantRevendication = idRevendicationPI,
                        dateAction = Tools.Tools.ConvertirDateTimeEnFormatJson(DateTime.Now),
                        auteurAction = auteur,
                    };

                    var retReq = await _envoieController.AcceptationRevendication(_body);
                    if (!retReq.operationResult)
                        return (new GeneraleRetour { status = 400, detail = retReq.erreur });
                    else
                    {
                        t_rev.statut = statut_revendication.ACCEPTEE;
                        await _revendicationRepo.UpdateAsync(t_rev);
                        return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(t_rev) });
                    }
                }
                else
                {
                    // Envoi d'otp
                  
                    t_otp o = await _otpRepo.genererOtp(clientID,t_rev.idRevendicationPi, type_otp.REJET_REVENDICATION, _paramdata.sms.validite_otp ?? 6);
                    await _serviceMessagerie.sendMessageAuClient(type_modele.REJET_REVENDICATION, t_rev.alias, dataclient, o);
                    return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(t_rev) });

                }

            }

            catch (Exception e)
            {
                _logger.LogError("RevendiquerUnAlias", e.Message);
                return (new GeneraleRetour { status = 500 });
            }

        }


        public async Task<GeneraleRetour> ConfirmerLeRejetDuneRevendication(int ClientID,string idRevendicationPI, QueryConfirmationOtpDto dt, string? IdDemande)

        {

            try
            {

                if (string.IsNullOrEmpty(idRevendicationPI))
                    return (new GeneraleRetour { status = 400, detail = "L'identifiant PI de la revendication est obligatoire" });

                List<InvalidParam> invalidParams = new List<InvalidParam>();

                if (string.IsNullOrEmpty(dt.otp))
                {
                    invalidParams.Add(new InvalidParam { name = "otp", reason = "OTP de confirmation requis" });
                    return (new GeneraleRetour { status = 400, detail = "L'OTP de confirmation est requis", invalidParams = invalidParams });
                }

                t_revendication t_rev = await _revendicationRepo.SearchRevendicationByIdPI(idRevendicationPI);

                if (t_rev == null)
                    return (new GeneraleRetour { status = 404, detail = "La revendication est introuvable dans le système" });


                if (t_rev.statut != statut_revendication.INITIE)
                    return (new GeneraleRetour { status = 403, detail = "La revendication est terminée ou cloturée" });



                int res_otp = await _otpRepo.verifieOtp(ClientID,dt.otp, type_otp.REJET_REVENDICATION, t_rev.idRevendicationPi);
                switch (res_otp)
                {
                    case 0:
                        return (new GeneraleRetour { status = 403, detail = "OTP Expiré" });
                    case -1:
                        return (new GeneraleRetour { status = 403, detail = "OTP Invalide" });
                    case 1:
                        break;
                };

                RejeterRevendicationDTO _body = new RejeterRevendicationDTO
                {
                    identifiantRevendication = idRevendicationPI,
                    dateAction = Tools.Tools.ConvertirDateTimeEnFormatJson(DateTime.Now),
                };

                var retReq = await _envoieController.RejeterRevendication(_body);

                if (!retReq.operationResult)
                    return (new GeneraleRetour { status = (int)retReq._statuscode, detail = retReq.erreur });
                else
                {
                    t_rev.statut = statut_revendication.REJETEE;
                    await _revendicationRepo.UpdateAsync(t_rev);
                    return (new GeneraleRetour { status = 200 });
                }

            }

            catch (Exception e)
            {
                _logger.LogError("ConfirmerLeRejetDuneRevendication", e.Message);
                return (new GeneraleRetour { status = 500 });
            }

        }


        public async Task<ReponseTraiteDto> TraiterRetourEvenement(string message)
        {
            RetourEventDto retour = JsonConvert.DeserializeObject<RetourEventDto>(message); ;

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
            }

            return (new ReponseTraiteDto { status_code = 400, desc_error = "Une erreur est survenue pendant le traitement" });

        }
    }


}


