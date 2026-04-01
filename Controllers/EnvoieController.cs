using ask.Dtos.RequestToSendDto;
using AutoMapper;
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.DtoAppMobile;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Dtos.EnvoieController;
using InteroperabiliteProject.Dtos.Notification;
using InteroperabiliteProject.Dtos.Reglements;
using InteroperabiliteProject.Dtos.RevendicationAlias;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text.Json;

namespace InteroperabiliteProject.Controllers
{

    public class EnvoieController
    {
        private readonly IConfiguration _configuration;
        private readonly AIPDATA _aipdata;
        private readonly IMapper _mapper;
        private readonly ILogger<EnvoieController> _logger;
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly JsonSerializerSettings jsonSerializerSettings;
        private readonly IDbContextFactory<InteropContext> _contextFactory;

        //private readonly DemandeligneRepo _demandeLigneRepo;

        private readonly string msg_error_systeme = "Une erreur inattendue s’est produite sur le serveur [PI] lors du traitement de la demande";

        public EnvoieController(IConfiguration configuration, IOptions<AIPDATA> aipdata, IMapper mapper, ILogger<EnvoieController> logger, IDbContextFactory<InteropContext> contextFactory)
        {
            _configuration = configuration;
            _aipdata = aipdata.Value;
            _mapper = mapper;
            _logger = logger;
            _contextFactory = contextFactory;

            //_demandeLigneRepo = demandeLigneRepo;
            jsonSerializerOptions = new JsonSerializerOptions
            {
                //   DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,

                //  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

        }

        //**************************************************PARTICIPANT MANAGEMENT **************************************************
        public async Task<RetourClassAip> DemandeListeParticipant()
        {
            string _script = "DemandeListeParticipant";
            try
            {
                string fullpath = $"{_aipdata.BaseUriClientApi}/participants/listes";

                RequeteListeParticipant RqLp = new RequeteListeParticipant
                {
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre)
                };

                string Body = JsonConvert.SerializeObject(RqLp);
                await saveMessage(typeMessage.DEMANDE_LISTE_PARTICIPANT, Body);
                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, RqLp.msgId);

            }
            catch (Exception ex)
            {
                _logger.LogInformation($"[{_script}] Exception Capturée pendant l'envoi : {ex.Message}");
                return ErreurSysteme(ex);
            }
        }
        //**************************************************IDENTITE MANAGEMENT******************************************************
        public async Task<RetourClassAip> DemandeVerificationIdentite(RequeteDemandeVerificationIdentiteAip cre)
        {
            try
            {
                string fullpath = $"{_aipdata.BaseUriClientApi}/verifications-identites";

                string Body = JsonConvert.SerializeObject(cre, jsonSerializerSettings);
                await saveMessage(typeMessage.DEMANDE_VERIFICATION_IDENTITE, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.msgId);


            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> ReponseaDemandeVerificationIdentite(RequeteReponseaDemandeVerificationIdentite cre)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/verifications-identites/reponses";

                string Body = JsonConvert.SerializeObject(cre);
                await saveMessage(typeMessage.REPONSE_VERIFICATION_IDENTITE, Body);
                _logger.LogInformation($"====>>>>>>>>>>>>>>>avant envoi Repondre à une demande verification d'identite {cre.ToString()}");

                //  ********************************************* Envoie de la requette au serveur**************************************************
                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);
                _logger.LogInformation($"====>>>>>>>>>>>>>>>Apres envoi Repondre à une demande verification d'identite {cre.ToString()}");

                return await TraiterReponseHttp(response, "");
            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        //**************************************************ALIAS MANAGEMENT ********************************************************
        public async Task<RetourClassAip> DemandeDeRechercheAlias(RequeteDemandeDeRechercheAliasClient cre)
        {
            try
            {
                string fullpath = $"{_aipdata.BaseUriClientApi}/alias/recherche";

                RequeteDemandeDeRechercheAlias bodayaip = _mapper.Map<RequeteDemandeDeRechercheAlias>(cre);
                bodayaip.endToEndId = Tools.Tools.GenerateEndToEndCode(_aipdata);

                string Body = JsonConvert.SerializeObject(bodayaip);
                await saveMessage(typeMessage.RECHERCHE_ALIAS, Body);

                _logger.LogInformation($"DEMANDE RECHERCHE ALIAS ===============>URI :{fullpath} Body : {Body}");
                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, bodayaip.endToEndId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> DemandeDeCreationAlias(CreationAlias cre)
        {
            try
            {
                //**********************************************Constitution URI * *****************************************************************
                string fullpath = $"{_aipdata.BaseUriClientApi}/alias/creation";
                string Body = JsonConvert.SerializeObject(cre, jsonSerializerSettings);
                await saveMessage(typeMessage.DEMANDE_CREATION_ALIAS, Body);


                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.idCreationAlias);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> DemandeDeSuppressionAlias(RequeteDemandeDeSuppressionAlias cre)
        {
            try
            {


                string fullpath = $"{_aipdata.BaseUriClientApi}/alias/suppression";
                string Body = JsonConvert.SerializeObject(cre);
                await saveMessage(typeMessage.DEMANDE_SUPPRESSION_ALIAS, Body);

                //**********************************************Creation du corps de la requette**************************************************
                _logger.LogInformation($"Element transmis a l'AIP delete Alias =============>{Body}");
                //  ********************************************* Envoie de la requette au serveur**************************************************
                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);
                // *********************************************Envoie de la requette au serveur**************************************************
                _logger.LogInformation($"Element Retour Code status a l'AIP delete Alias =============>{response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                //  ******************************************* Gestion de la reponse***************************************************************

                return await TraiterReponseHttp(response, cre.alias);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> DemandeDemodificationAlias(QueryModificationAliasClientDto cre)
        {
            try
            {
                string fullpath = $"{_aipdata.BaseUriClientApi}/alias/modification";

                string Body = System.Text.Json.JsonSerializer.Serialize(cre, jsonSerializerOptions);
                await saveMessage(typeMessage.DEMANDE_MODIFICATION_ALIAS, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.alias);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        //**************************************************TRANSFERT MANAGEMENT********************************************************
        public async Task<RetourClassAip> DemandeDeTransfert(TransfertDto cre)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/transferts";
                string Body = JsonConvert.SerializeObject(cre, jsonSerializerSettings);
                await saveMessage(typeMessage.DEMANDE_TRANSFERT, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.msgId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> ReponseAUneDemandeDeTransfert(ReponseAUneDemandeDeTransfert _body)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/transferts/reponses";
                string Body = JsonConvert.SerializeObject(_body, jsonSerializerSettings);
                //  await saveMessage(typeMessage.REPONSE_DEMANDE_TRANSFERT, Body);


                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);
                _logger.LogInformation("Traitement de reponse du transfert");

                return await TraiterReponseHttp(response, "");

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        public async Task<RetourClassAip> RecupererStatutTransfert(string endToEndId)
        {
            try
            {

                RequeteDemandeTransfertStatut _data = new RequeteDemandeTransfertStatut
                {
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    endToEndId = endToEndId
                };

                string fullpath = $"{_aipdata.BaseUriClientApi}/transferts/statut";
                string Body = JsonConvert.SerializeObject(_data);

                await saveMessage(typeMessage.DEMANDE_STATUT_TRANSFERT, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, _data.msgId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }

        }

        public async Task<RetourClassAip> DemandeAnnulationTransfert(RequeteSendAnnulationDeTransfertDTo cre)
        {
            try
            {

                //**********************************************Constitution URI * *****************************************************************
                string fullpath = $"{_aipdata.BaseUriClientApi}/retour-fonds/demande";
                string Body = JsonConvert.SerializeObject(cre);

                await saveMessage(typeMessage.DEMANDE_RETOUR_FONDS, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.endToEndId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> RepondreAUneDemandeAnnulation(ReponseSendDemandeAnnulationDto cre)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/retour-fonds/reponses";
                string Body = JsonConvert.SerializeObject(cre);

                await saveMessage(typeMessage.REPONSE_DEMANDE_RETOUR_FONDS, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.endToEndId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        public async Task<RetourClassAip> RetournerLesFonds(RequeteRetourDesFondsDto cre)
        {
            try
            {

                //**********************************************Constitution URI * *****************************************************************
                string fullpath = $"{_aipdata.BaseUriClientApi}/retour-fonds";
                string Body = JsonConvert.SerializeObject(cre);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cre.endToEndId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        //*************************************************Test de connectivité****************************************
        public async Task<RetourClassAip> DemandeTestDeConnectivite(string type)
        {
            string[] arrayValidate = ["PING", "MAIN"];
            if (arrayValidate.Contains(type))
            {
                return new RetourClassAip { _statuscode = 400, messageResult = "Les données envoyées ne sont pas conformes", operationResult = false };
            }

            try
            {
                //**********************************************Constitution URI * *****************************************************************
                string fullpath = $"{_aipdata.BaseUriClientApi}/notifications/test-connectivite";

                ConnectiviteDTO cto = new ConnectiviteDTO
                {
                    evenement = type,
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    evenementDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    evenementDescription = (type == "PING") ? "Test de connectivité" : "Système en maintenance",
                };

                string Body = JsonConvert.SerializeObject(cto);

                await saveMessage(typeMessage.TEST_CONNECTIVITE, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, cto.msgId);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        //*************************************************Test de connectivité****************************************


        //*************************************************Revendication d'alias****************************************
        public async Task<RetourClassAip> RevendiquerAlias(string numero)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/revendications/creation";

                var contentObjetc = new { alias = numero };
                string Body = JsonConvert.SerializeObject(contentObjetc);
                await saveMessage(typeMessage.REVENDICATION_ALIAS, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);
                return await TraiterReponseHttp(response, contentObjetc.alias);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }


        public async Task<RetourClassAip> RecupererRevendication(string identifiantRevendication)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/revendications/recuperation";

                var contentObjetc = new { identifiantRevendication = identifiantRevendication };
                string Body = JsonConvert.SerializeObject(contentObjetc);
                await saveMessage(typeMessage.REPONSE_RECUPERATION_REVENDICATION_ALIAS, Body);


                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, contentObjetc.identifiantRevendication);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }


        public async Task<RetourClassAip> AcceptationRevendication(AcceptationRevendicationDTO acteur)
        {
            try
            {
                string[] arrayValidate = ["CLIENT", "PARTICIPANT"];
                if (!arrayValidate.Contains(acteur.auteurAction))
                {
                    return new RetourClassAip { erreur = "Err823", _statuscode = 400, messageResult = "Les données envoyées ne sont pas conformes", operationResult = false };
                }

                string fullpath = $"{_aipdata.BaseUriClientApi}/revendications/acceptation";

                AcceptationRevendicationDTO act = new AcceptationRevendicationDTO
                {
                    auteurAction = acteur.auteurAction,
                    identifiantRevendication = acteur.identifiantRevendication,
                    dateAction = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                string Body = JsonConvert.SerializeObject(act);
                await saveMessage(typeMessage.REPONSE_ACCEPTATION_REVENDICATION_ALIAS, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, acteur.identifiantRevendication);
            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }


        public async Task<RetourClassAip> RejeterRevendication(RejeterRevendicationDTO acteur)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/revendications/rejet";

                RejeterRevendicationDTO act = new RejeterRevendicationDTO
                {
                    identifiantRevendication = acteur.identifiantRevendication,
                    dateAction = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
                string Body = JsonConvert.SerializeObject(act);
                await saveMessage(typeMessage.REPONSE_REJET_REVENDICATION_ALIAS, Body);


                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, acteur.identifiantRevendication);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        //*************************************************Revendication d'alias****************************************


        //************************************************Gestion des Paiements************************************************
        public async Task<RetourClassAip> DemandeDePaiement(DemandeDePaiementDTO DmdPmnt)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/demandes-paiements";
                string Body = JsonConvert.SerializeObject(DmdPmnt, jsonSerializerSettings);
                await saveMessage(typeMessage.DEMANDE_PAIEMENT, Body);

                _logger.LogInformation($"Données envoyées à PI [Demande de paiement] ===============>URI :{fullpath} Body : {Body}");
                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, DmdPmnt.msgId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        public async Task<RetourClassAip> ReponseAuneDemandeDePaiement(ReponseaDemandeDePaiementDTO DmdPmnt)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/demandes-paiements/reponses";
                string Body = JsonConvert.SerializeObject(DmdPmnt);
                await saveMessage(typeMessage.REPONSE_DEMANDE_PAIEMENT, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);
                return await TraiterReponseHttp(response, DmdPmnt.msgId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        //************************************************Gestion des Paiements************************************************


        //*************************************************rapport de demande*******************************************
        public async Task<RetourClassAip> DemandeRapportReglement(string typeRapport, string datedebut, string heuredebut)
        {
            try
            {
                string[] arrayValidate = ["COMP", "FACT", "TRANS"];
                if (arrayValidate.Contains(typeRapport))
                {
                    return new RetourClassAip { erreur = "Err863", _statuscode = 400, messageResult = "Les données envoyées ne sont pas conformes", operationResult = false };
                }
                string fullpath = $"{_aipdata.BaseUriClientApi}/rapports/demandes";

                DemandeRapportReglementDTO act = new DemandeRapportReglementDTO
                {
                    msgId = Tools.Tools.GenererMessageId(_aipdata.codemembre),
                    typeRapport = typeRapport,
                    dateDebutPeriode = datedebut,
                    heureDebutPeriode = heuredebut,

                };
                string Body = JsonConvert.SerializeObject(act);
                await saveMessage(typeMessage.RAPPORT_COMPENSATION, Body);

                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);
                // *********************************************Envoie de la requette au serveur**************************************************

                return await TraiterReponseHttp(response, act.msgId);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        public async Task<RetourClassAip> TelechargerRapportTransaction(string id)
        {
            try
            {

                string fullpath = $"{_aipdata.BaseUriClientApi}/rapports/telechargements";

                var content = new { id = id };
                string Body = JsonConvert.SerializeObject(content);
                await saveMessage(typeMessage.RAPPORT_TRANSACTION, Body);


                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, Body, _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, id);

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }

        //*************************************************rapport de demande*******************************************



        //***********************************************Rechercher l'état d'un message*********************************
        public async Task<RetourClassAip> RechercheEtatMessage(string msgId)
        {
            try
            {
                if (string.IsNullOrEmpty(msgId))
                {
                    return new RetourClassAip { _statuscode = 400, messageResult = "Les données envoyées ne sont pas conformes", operationResult = false, erreur = "Err843" };
                }

                string fullpath = $"{_aipdata.BaseUriClientApi}/messages/statut/{msgId}";
                var response = await RequettePI.ExecuteHttpsPostRequestAsync(fullpath, "", _aipdata.liencertificat, _aipdata.cleprive);

                return await TraiterReponseHttp(response, "");

            }
            catch (Exception ex)
            {
                return ErreurSysteme(ex);
            }
        }
        //***********************************************Rechercher l'état d'un message*********************************



        public int GetIdDemande(HttpRequest request)
        {
            return Convert.ToInt32(request.Headers["id-dmd-header"].ToString());
        }



        private async Task<RetourClassAip> TraiterReponseHttp(HttpResponseMessage response, string msgId = null)
        {

            var result = new RetourClassAip
            {
                _statuscode = (int)response.StatusCode
            };

            result.messageResult = await response.Content.ReadAsStringAsync();


            switch (response.StatusCode)
            {
                case HttpStatusCode.Accepted:
                    result.operationResult = true;
                    result.erreur = "Scs700";
                    if (!string.IsNullOrWhiteSpace(msgId))
                        result.idoperation = msgId;
                    break;

                case HttpStatusCode.OK:
                    result.operationResult = false;
                    result.erreur = "Err704";
                    break;

                case HttpStatusCode.BadRequest:
                    result.operationResult = false;
                    result.erreur = "Err703";
                    break;

                case HttpStatusCode.NotFound:
                    result.operationResult = false;
                    result.erreur = "Err705";
                    break;

                default:
                    result.operationResult = false;
                    result.erreur = "Err706";
                    result.messageResult = msg_error_systeme;
                    break;
            }

            return result;
        }

        private RetourClassAip ErreurSysteme(Exception ex)
        {

            _logger.LogError("Message d'erreur " + ex.Message);
            return new RetourClassAip { _statuscode = 500, messageResult = msg_error_systeme, operationResult = false, erreur = "Err001" };
        }

        public async Task saveMessage(typeMessage type, string body, bool bmsgIdEstmsgDemande = false)
        {
            try
            {

                await MessageService.SaveMessageAsyncAvecDbFactory(_contextFactory, _logger, type, body, bmsgIdEstmsgDemande);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[saveMessage]  {ex.Message}  [Inner exception ] {ex?.InnerException?.Message}");
                throw;
            }
        }




    }
}