using ask.Dtos.RequestToReceiveDto;
using ask.Dtos.RequestToSendDto;
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.DtoAppMobile.Securite;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Dtos.EnvoieController;
using InteroperabiliteProject.Dtos.Reglements;
using InteroperabiliteProject.Dtos.RevendicationAlias;
using InteroperabiliteProject.Event;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.RequestToReceiveDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;


namespace InteroperabiliteProject.Controllers
{
    [Route("apinaomi")]
    [ApiController]
    public class ReceptionController : ControllerBase
    {

        private readonly EventService _eventService;
        //private readonly AliasEvent _myEventAlias;
        private TaskCompletionSource<string> _tcs = new TaskCompletionSource<string>();
        private readonly ReceptionAIPController _receptionAipController;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReceptionController> _logger;

        private readonly IDbContextFactory<InteropContext> _contextFactory;

        public ReceptionController(EventService eventService , IDbContextFactory<InteropContext> contextFactory, IConfiguration configuration, ILogger<ReceptionController> logger, ReceptionAIPController receptionAipController)
        {
            _eventService = eventService;
            _configuration = configuration;
            _logger = logger;
            _receptionAipController = receptionAipController;
            _contextFactory = contextFactory;
        }

        [NonAction]
        public string RecupererIdDemandeEnCours()
        {
            return Request.Headers["id-dmd-header"].ToString();
        }

        //********************************************Route reel mis en place qui respectes les specification attendu
        [HttpPost("verifications-identites")]
        public async Task<IActionResult> RecevoirUneDemandeDeVerificationIdentite(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception d'un message de Verification d'identité AIP {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.DEMANDE_VERIFICATION_IDENTITE, jsonElement.ToString());
                var dataRecue = JsonConvert.DeserializeObject<VerificationIdentiteRequestFromAIP>(jsonElement.ToString());
               
                dataRecue.Validate();
                
                await _receptionAipController.RecevoirDemandeVerificationIdentite(dataRecue);
              
                return Accepted(new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }


        [HttpPost("verifications-identites/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAUneVerificationIdentite(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception du message de Recherche d'identité {jsonElement.ToString()}");

                await _receptionAipController.saveMessage(typeMessage.REPONSE_VERIFICATION_IDENTITE, jsonElement.ToString());

                var data = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationIdentite>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(data.msgIdDemande, JsonConvert.SerializeObject(retour_data).ToString());

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }


        [HttpPost("verifications-identites/echecs")]
        public async Task<IActionResult> RecevoirUnEchecAUneVerificationIdentite(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception d'echec du message de Recherche d'identité {jsonElement.ToString()}");

                await _receptionAipController.saveMessage(typeMessage.REPONSE_VERIFICATION_IDENTITE, jsonElement.ToString());

                var data = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationIdentite>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(data.msgIdDemande, JsonConvert.SerializeObject(retour_data).ToString());

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }


        [HttpPost("alias/recherche/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAUneDemandeDeRechercheAlias(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception du message de Recherche d'alias depuis AIP {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.REPONSE_RECHERCHE_ALIAS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseADemandeRechercheAlias>(jsonElement.ToString());

                _logger.LogError($"[Apres la deserialisation  =================> valeur retreq : {jsonElement.ToString()}");


                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };


                _eventService.TriggerEvent(retreq.endToEndId, JsonConvert.SerializeObject(retour_data).ToString());
                _logger.LogInformation($"Reception du message de Recherche d'alias depuis AIP =======================>{retreq.endToEndId} corps {JsonConvert.SerializeObject(retour_data)}");
                _logger.LogInformation($"Reception du message de Recherche d'alias depuis AIP =======================> ON RETOURNE UN STATUS 202 car non exception");

                return StatusCode(202, new { message = "Réponse de recherche d'alias reçue avec succés" });
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Reception du message de Recherche d'alias depuis AIP =======================> ON RETOURNE UN STATUS 400 Car Exception suivant {ex.Message}");
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("alias/creation/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAUneDemandeDeCreationAlias(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception du message de creation d'alias depuis AIP {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.REPONSE_CREATION_ALIAS, jsonElement.ToString());

                var data = JsonConvert.DeserializeObject<ReponseDemandeCreationAlias>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(data.idCreationAlias, JsonConvert.SerializeObject(retour_data).ToString());
            
                return StatusCode(202, new { message = "Réponse de création d'alias reçue avec succés" });


            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("alias/modification/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAUneDemandeDeModificationAlias(JsonElement jsonElement)
        {

            try
            {
                await _receptionAipController.saveMessage(typeMessage.REPONSE_MODIFICATION_ALIAS, jsonElement.ToString());

                var data = JsonConvert.DeserializeObject<ReponseADemandeDeModificationAlias>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };


                _eventService.TriggerEvent(data.alias, JsonConvert.SerializeObject(retour_data).ToString());
                if (data.statut == "SUCCES")
                    return StatusCode(202, new { message = "Réponse de modification d'alias reçue avec succés" });
                else
                    return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("alias/suppression/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAUneDemandeDeSuppressionAlias(JsonElement jsonElement)
        {

            try
            {

                await _receptionAipController.saveMessage(typeMessage.REPONSE_SUPPRESSION_ALIAS, jsonElement.ToString());

                _logger.LogInformation($"Reception de la reponse de suppression d'alias depuis AIP {jsonElement.ToString()}");

                var retreq = JsonConvert.DeserializeObject<ReponseADemandeDeSuppressionAlias>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.alias, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Réponse de suppression d'alias reçue avec succés" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("verifications-identites/rejets")]
        public async Task<IActionResult> RecevoirUneDemandeDeVerificationIdentiteRejet(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception de la reponse  depuis AIP {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.REPONSE_REJET_VERIFICATION_IDENTITE, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseEchecDto>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REJET_ECHEC,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.reference, JsonConvert.SerializeObject(retour_data).ToString());

                return StatusCode(202, new { message = "Demande en cours de traitement" });


            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("transferts")]
        public async Task<IActionResult> RecevoirUneDemandeDetransfert(JsonElement jsonElement)
        {
            string IdDemande = RecupererIdDemandeEnCours();
            try
            {
                await _receptionAipController.saveMessage(typeMessage.DEMANDE_TRANSFERT, jsonElement.ToString());

                _logger.LogInformation($"Reception de demande de transfert depuis AIP {jsonElement.ToString()}");
                var retreq = JsonConvert.DeserializeObject<TransfertDto>(jsonElement.ToString());

               await _receptionAipController.RecevoirDemandeDetransfertEtRepondre(retreq, IdDemande);

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("transferts/reponses")]
        public async Task<IActionResult> RecevoirUneReponseDemandeDetransfert(JsonElement jsonElement)
        {
            try
            {
                await _receptionAipController.saveMessage(typeMessage.REPONSE_DEMANDE_TRANSFERT, jsonElement.ToString());

                string idDemande = RecupererIdDemandeEnCours();
                _logger.LogInformation($"Reception de reponse d'un transfert depuis AIP {jsonElement.ToString()}");
                var retreq = JsonConvert.DeserializeObject<ReponseRecuDemandeDeTransfert>(jsonElement.ToString());

              //  await _receptionAipController.RecevoirIrrevocabiliteDeDisponibilite(retreq, idDemande);
                await _receptionAipController.RecevoirIrrevocabilite(retreq, idDemande);

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.msgIdDemande, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {
                _logger.LogError("transferts/reponses =======> "+ex.Message);
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }
        }

        [HttpPost("transferts/echecs")]
        [HttpPost("transferts/rejets")]
        public async Task<IActionResult> RecevoirUnEchecDemandeDetransfert(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception du message d'echec ou rejet de transfert depuis AIP {jsonElement.ToString()}");
                string idDemande = RecupererIdDemandeEnCours();
                await _receptionAipController.saveMessage(typeMessage.ECHEC_DEMANDE_TRANSFERT, jsonElement.ToString());

                var data = JsonConvert.DeserializeObject<ReponseEchecDto>(jsonElement.ToString());
               await _receptionAipController.TraitementEchecTransfert(data.reference, data.codeRaisonRejet, data.descriptionRaisonRejet, idDemande);

               RetourEventDto retour_data = new RetourEventDto
                  {
                   type = type_notification.NOTIFICATION_REJET_ECHEC,
                  data = jsonElement.ToString()
               };

                 _eventService.TriggerEvent(data.reference, JsonConvert.SerializeObject(retour_data).ToString());

                if (data.reference != null)
                    return StatusCode(202, new { message = "Demande en cours de traitement" });
                else
                    return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
          
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }
        
        [HttpPost("messages-iso/echec-envoi")]
        public async Task<IActionResult> RecevoirUnmessageisoRejeteFormatInvalide(JsonElement jsonElement)
        {

            try
            {

                _logger.LogInformation($"Reception de notification d'échec d'envoi de message ISO pour format invalide {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.ECHEC_ENVOI_FORMAT_ISO, jsonElement.ToString(),true);

                var retreq = JsonConvert.DeserializeObject<RcpMessageIsoFormatInvalide>(jsonElement.ToString());

                await _receptionAipController.TraitementEchecTransfert(retreq.msgId, retreq.raison, retreq.detailEchec,"");


                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_ECHEC_FORMAT_ISO_INVALIDE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.msgId, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("/message-traitement/echec")]
        public async Task<IActionResult> RecevoirUnMessageEchecDeTraitement(JsonElement jsonElement)
        {

            try
            {

                _logger.LogInformation($"Reception d'une notificationd'échec de traitement du message par l'AIP. {jsonElement.ToString()}");
               await _receptionAipController.saveMessage(typeMessage.ECHEC_TRAITEMENT_AIP, jsonElement.ToString(), true);

                var data = JsonConvert.DeserializeObject<RcpEchecMessageIso>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_ECHEC_TRAITEMENT_AIP,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(data.msgId, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("messages-iso/echec-envoi/echec-http")]
        public async Task<IActionResult> RecevoirUnmessageEchecEnvoiHttp(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception d'une notification d'échec d'envoi de message pour cause d'erreur 500 ou 504 {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.ECHEC_HTTP_ENVOI, jsonElement.ToString(), true);


                var retreq = JsonConvert.DeserializeObject<RcpEchecMessageIso>(jsonElement.ToString());
                 await _receptionAipController.TraitementEchecTransfert(retreq.msgId, "", retreq.detailEchec, "");

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_ECHEC_500_504,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.msgId, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("message-envoi/echec-http")]
        public async Task<IActionResult> RecevoirUnmessageEnvoiEchecHttp(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception d'une notification d'échec d'envoi de message pour cause d'erreur echec-http{jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.ECHEC_HTTP_ENVOI, jsonElement.ToString(), true);


                var retreq = JsonConvert.DeserializeObject<RcpEchecMessageIso>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_ECHEC_500_504,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.msgId, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

       

        [HttpPost("notifications/echecs")]
        public async Task<IActionResult> RecevoirNotificationEchecs(JsonElement jsonElement)
        {

            try
            {
                _logger.LogInformation($"Reception d'une notification d'échec {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.NOTIFICATION_ECHEC, jsonElement.ToString(), true);


                var retreq = JsonConvert.DeserializeObject<ReponseNotificationEchec>(jsonElement.ToString());
               await _receptionAipController.TraitementEchecTransfert(retreq.msgId, retreq.codeRaisonRejet, retreq.descriptionRaisonRejet, "");

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REJET_ECHEC,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.msgId, JsonConvert.SerializeObject(retour_data).ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("participants/liste/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAuneDemandeDelisteParticipant(JsonElement jsonElement)
        {
            //_logger.LogInformation($"Reponses de l'AIP pour les participant avec les données suivante: {jsonElement}");
            try
            {
                _logger.LogInformation("Donnees recues ================================" + jsonElement.ToString());
                await _receptionAipController.saveMessage(typeMessage.LISTE_PARTICIPANT, jsonElement.ToString());


                var resbody = JsonConvert.DeserializeObject<ReponseDemandeListeParticipant>(jsonElement.ToString());

                await _receptionAipController.RecevoirListeDesParticipants(resbody);

                return StatusCode(202, new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

       

        [HttpPost("demandes-paiements")]
        public async Task<IActionResult> RecevoirDemandeDePaiement(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Reception du message de demande de paiement depuis AIP {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.DEMANDE_PAIEMENT, jsonElement.ToString());


                var retreq = JsonConvert.DeserializeObject<DemandeDePaiementDTO>(jsonElement.ToString());

               // retreq.Validate();

                await _receptionAipController.RecevoirDemandePaiement(retreq, IdDemande);
                return StatusCode(202, new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }



        [HttpPost("demandes-paiements/reponses")]
        public async Task<IActionResult> RecevoirUneReponseAUneDemandeDePaiement(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Reception de la reponse du message de paiement depuis AIP {jsonElement.ToString()}");
               await _receptionAipController.saveMessage(typeMessage.REPONSE_DEMANDE_PAIEMENT, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseaDemandeDePaiementDTO>(jsonElement.ToString());


                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };

                _eventService.TriggerEvent(retreq.msgIdDemande, JsonConvert.SerializeObject(retour_data).ToString());
               
                await _receptionAipController.RecevoirRejetDemandePaiement(retreq, IdDemande);
                
                return StatusCode(202, new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }
        }


        [HttpPost("notifications/relation")]
        public async Task<IActionResult> RecevoirNotificationDeRelation(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception de notification de relation depuis AIP {jsonElement.ToString()}");
               await _receptionAipController.saveMessage(typeMessage.NOTIFICATION_RELATION, jsonElement.ToString());


                var retreq = JsonConvert.DeserializeObject<RcpNotificationRelationDto>(jsonElement.ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("notifications/accuse-reception")]
        public async Task<IActionResult> RecevoirNotificationAccuseReception(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception de notification d'accusé de reception {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.NOTIFICATION_ACCUSE_RECEPTION, jsonElement.ToString());


                var retreq = JsonConvert.DeserializeObject<RcpAccuseReceptionDto>(jsonElement.ToString());

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }


        }

        [HttpPost("notifications/info-warn")]
        public async Task<IActionResult> RecevoirNotificationInfoWarn(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Reception de notification d'info-warn {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.NOTIFICATION_INFO_WARN, jsonElement.ToString());


                var retreq = JsonConvert.DeserializeObject<RcpNotificationInfoWarnDto>(jsonElement.ToString());
                await _receptionAipController.RecevoirNotificationInfoWarn(retreq, IdDemande);

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("notifications/garantie")]
        [HttpPost("notifications/garanties")]
        public async Task<IActionResult> RecevoirNotificationModificationGarantie(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception de notification de modification de garantie {jsonElement.ToString()}");

                await _receptionAipController.saveMessage(typeMessage.NOTIFICATION_MODIFICATION_GARANTIE, jsonElement.ToString());
                var retreq = JsonConvert.DeserializeObject<RcpNotificationModificatonGarantie>(jsonElement.ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("reglements/soldes")]
        [HttpPost("reglements/solde")]
        public async Task<IActionResult> RecevoirReglementsSoldes(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception des rapports de compensations {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.RAPPORT_COMPENSATION, jsonElement.ToString());
                var retreq = JsonConvert.DeserializeObject<RcpNotificationRegelementSoldeDto>(jsonElement.ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }



        [HttpPost("reglements/factures")]
        [HttpPost("reglements/facture")]
        public async Task<IActionResult> RecevoirRapportdeFacturation(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Recevoir les rapports de facturation==========> {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.RAPPORT_FACTURATION, jsonElement.ToString());

                var data = JsonConvert.DeserializeObject<RecevoirLesRapportsDeFacturationDTO>(jsonElement.ToString());
                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }


        [HttpPost("rapports/telechargements/reponse")]
        [HttpPost("rapports/telechargements/reponses")]
        public async Task<IActionResult> RecevoirRapportdetransaction(JsonElement jsonElement)
        {
            try
            {
                await _receptionAipController.saveMessage(typeMessage.RAPPORT_TRANSACTION, jsonElement.ToString());

                _logger.LogInformation($"Recevoir les rapports de transactions =======> {jsonElement.ToString()}");
                var retreq = JsonConvert.DeserializeObject<RecevoirLesRapportsDeTransactionsDTO>(jsonElement.ToString());

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("retour-fonds/reponses")]
        public async Task<IActionResult> RecevoirReponseDemandeAnnulation(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Retour d'une reponse de retour de fonds ==============> {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.REPONSE_DEMANDE_RETOUR_FONDS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseReceiveDemandeAnnulationDto>(jsonElement.ToString());

                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_RETOUR_FONDS,
                    data = jsonElement.ToString()
                };


                _eventService.TriggerEvent(retreq.endToEndId, JsonConvert.SerializeObject(retour_data).ToString());

                await _receptionAipController.RecevoirRejetRetourDeFonds(retreq, IdDemande);



                return StatusCode(202, new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("retour-fonds")]
        public async Task<IActionResult> RecevoirRetourDeFond(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Reception d'un retour de Fonds ==============> {jsonElement.ToString()}");
               await _receptionAipController.saveMessage(typeMessage.RETOUR_FONDS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<RequeteRetourDesFondsDto>(jsonElement.ToString());
                await _receptionAipController.RecevoirRetourFondsEtRepondre(retreq,"");

                return StatusCode(202, new { message = "Demande en cours de traitement" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("retour-fonds/demande")]
        public async Task<IActionResult> RecevoirUnedemandeDeRetourDeFonds(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Reception de demande de retour de fond ==============> {jsonElement.ToString()}");
               await _receptionAipController.saveMessage(typeMessage.DEMANDE_RETOUR_FONDS, jsonElement.ToString());

                var _data = JsonConvert.DeserializeObject<ReceiveDemandeAnnulationDto>(jsonElement.ToString());
                await _receptionAipController.RecevoirDemandeDeRetourDeFonds(_data, IdDemande);
                 return StatusCode(202, new { message = "Demande en cours de traitement" });
               
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }


        //**************************************************Revendication d'alias*******************************************************
        [HttpPost("revendications/reponses")]
        public async Task<IActionResult> RecevoirUneReponseDeRevendicationAlias(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Retour de revendications d'alias ==============> {jsonElement.ToString()}");

                await _receptionAipController.saveMessage(typeMessage.REPONSE_REVENDICATION_ALIAS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseDemandeRevendicationDTO>(jsonElement.ToString());


                RetourEventDto retour_data = new RetourEventDto
                {
                    type = type_notification.NOTIFICATION_REPONSE_REQUETE,
                    data = jsonElement.ToString()
                };


                _eventService.TriggerEvent(retreq.alias, JsonConvert.SerializeObject(retour_data).ToString());

                return StatusCode(202, new { message = "Réponse de revendication d'alias reçue avec succés" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }


        [HttpPost("revendications/recuperation/reponses")]
        public async Task<IActionResult> RecevoirUneReponseaRevendicationAlias(JsonElement jsonElement)
        {
            try
            {
                _logger.LogInformation($"Revendication d'alias - Récuperation de reponse ==============> {jsonElement.ToString()}");
                await _receptionAipController.saveMessage(typeMessage.REPONSE_RECUPERATION_REVENDICATION_ALIAS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseARecuperationdeRevendication>(jsonElement.ToString());

                return StatusCode(202, new { message = "Réponse de recupération de revendication reçue avec succés" });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }

        [HttpPost("revendications/acceptation/reponses")]
        public async Task<IActionResult> RecevoirUneReponseaAcceptationRevendicationAlias(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Reponse d'acception de revendication d'alias==============> {jsonElement.ToString()}");

                await _receptionAipController.saveMessage(typeMessage.REPONSE_ACCEPTATION_REVENDICATION_ALIAS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseRevendicationDecisionReponseDTO>(jsonElement.ToString());
                await _receptionAipController.RecevoirReponseRevendicationAlias(retreq, IdDemande);
                return StatusCode(202, new { message = "Réponse d'acceptation de revendication reçue avec succés" });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }


        [HttpPost("revendications/rejet/reponses")]
        public async Task<IActionResult> RecevoirUneReponseaRejetRevendicationAlias(JsonElement jsonElement)
        {
            try
            {
                string IdDemande = RecupererIdDemandeEnCours();

                _logger.LogInformation($"Reponse de rejet d'une revendication ==============> {jsonElement.ToString()}");
               await _receptionAipController.saveMessage(typeMessage.REPONSE_REJET_REVENDICATION_ALIAS, jsonElement.ToString());

                var retreq = JsonConvert.DeserializeObject<ReponseRevendicationDecisionReponseDTO>(jsonElement.ToString());
                await _receptionAipController.RecevoirReponseRevendicationAlias(retreq, IdDemande);

                return StatusCode(202, new { message = "Demande en cours de traitement" });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = "Les données envoyées ne sont pas conformes" });
            }

        }


        //**************************************************Revendication d'alias*******************************************************



        //********************************************Route reel mis en place qui respectes les specification attendu************************************************

    }
}
