using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Model;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ask.Controllers
{
    [Route("api/[controller]")]

    public class askController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        private readonly askContext _dbContext;
     
        private readonly IConfiguration _configuration;
        private readonly ParamMessage _paramdata;
        private readonly ILogger<askController> _logger;
        private readonly IDbContextFactory<askContext> _dbFactory;


        //private readonly ILogger _logger;
        public askController(IDbContextFactory<askContext> dbFactory,askContext askContext, IOptions<ParamMessage> paramdata, IConfiguration configuration, IWebHostEnvironment env, ILogger<askController> logger)
        {

            _configuration = configuration;
            _dbFactory = dbFactory;
            _env = env;
            _paramdata = paramdata.Value;
            _logger = logger;
            _serviceMessagerie = serviceMessagerie;
            _serviceEtat = serviceEtat;
            _dbContext = askContext;
            _otpRepo = otpRepo;
           
        }

        [NonAction]
        public string GetPublicUrl(string otherPath, string fileName)
        {

            var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.ToString();
            var basePath = _configuration["AppSettings:BasePath"];


            var baseUrl = $"{scheme}://{host}";
            if (!string.IsNullOrEmpty(basePath))
                baseUrl += $"/{basePath}";

            var publicUrl = $"{baseUrl}/{otherPath}/{fileName}";

            return publicUrl.TrimEnd('/');
        }


        [NonAction]
        public Model.t_user GetInfoUser()
        {
            if (HttpContext.Items.ContainsKey("User"))
            {
                return (Model.t_user)HttpContext.Items["User"];
            }
            else
            {
                return null;
            }
        }

        [NonAction]
        public string RecupererToken()
        {
            if (Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {

                string token = authorizationHeader.ToString();

                if (token.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.ToString().Substring("Bearer ".Length).Trim();

                return token;
            }

            return "";
        }

        [NonAction]
        public string RecupererIdDemandeEnCours()
        {
            return Request.Headers["id-dmd-header"].ToString();
        }


      
        [Authorize]
        [HttpGet("comptes/{id}/soldes")]
        public async Task<IActionResult> GetSoldeCompte(string id)
        {
            string _desc_route = "Solde de compte";
            string IdDemande = RecupererIdDemandeEnCours();


            try
            {

                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro du compte est requis", instance: HttpContext.Request.Path));


                Model.t_client resp_client = GetInfoClient();

                t_compte _rech_compte = await _compteRepo.SearchCompteByIbanOrOther(id, resp_client.Id);
                if (_rech_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                var result = await _ServiceAIF.GetClientCompte(id, IdDemande);

                if (!result.operationStatus)
                {
                    return StatusCode(result.status,
                        GeneraleRetour.BuildProblemResponse(new GeneraleRetour { status = result.status, detail = result.erreur }, instance: HttpContext.Request.Path));
                }

                var res_data = JsonConvert.DeserializeObject<Message>(result.data);

                return Ok(new { solde = res_data.compte.soldeDisponibleCompte });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [HttpGet("comptes/{id}/infos")]
        public async Task<IActionResult> GetDetailsInfos(string id)
        {
            string _desc_route = "Informations de compte";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(GeneraleRetour.BuildNotFound(detail: "Le numéro du compte est requis", instance: HttpContext.Request.Path));


                var result = await _ServiceAIF.GetClientCompte(id, IdDemande);

                if (!result.operationStatus)
                {
                    return StatusCode(result.status,
                        GeneraleRetour.BuildProblemResponse(new GeneraleRetour { status = result.status, detail = result.erreur }, instance: HttpContext.Request.Path));
                }


                var res_data = JsonConvert.DeserializeObject<Message>(result.data);
                return Ok(res_data);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));

            }
        }


        [Authorize]
        [HttpGet("comptes/{id}/details")]
        public async Task<IActionResult> GetDetailsCompteNew(string id)
        {
            string _desc_route = "Détails de compte";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {
                Model.t_client resp_client = GetInfoClient();

                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(GeneraleRetour.BuildNotFound(detail: "Le numéro du compte est requis", instance: HttpContext.Request.Path));


                t_compte _rech_compte = await _compteRepo.SearchCompteByIbanOrOther(id, resp_client.Id);
                if (_rech_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                string myIbanOrIban = _rech_compte.ibanOrOther;

                t_alias _rech_alias = await _aliasRepo.SearchAliasByIban(myIbanOrIban);
                if (_rech_alias == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte n'as pas d'alias dans le système", instance: HttpContext.Request.Path));


                if (_rech_alias.categorie != "P" && _rech_alias.categorie != "C")
                    Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Vous n'avez pas les autorisations", instance: HttpContext.Request.Path));

                DetailsCompteDto e = new DetailsCompteDto
                {
                    dateCreation = Tools.Tools.ConvertirDateTimeEnFormatJson(_rech_alias.dateCreationAlias),
                    dateModification = Tools.Tools.ConvertirDateTimeEnFormatJson(_rech_alias.dateModificationAlias),
                    alias = new aliasDto
                    {
                        cle = _rech_alias.valeurAlias,
                        type = _rech_alias.typeAlias,
                        shid = _rech_alias.shid,
                        codeQr = _rech_alias.codeQr,
                    },
                    compte = new compteDto
                    {
                        participant = _rech_alias.participant,
                        type = _rech_alias.typeCompte,
                        numero = _rech_compte.numeroCompte,
                        agence = _rech_compte.codeAgence,
                        dateOuverture = _rech_alias.dateOuvertureCompte
                    },

                    client = new clientDto
                    {
                        categorie = _rech_alias.categorie,
                        nom = _rech_alias.nomClient,
                        nationalite = _rech_alias.nationalite,
                        paysResidence = _rech_alias.paysResidence,
                        telephone = _rech_alias.telephone,
                        photo = _rech_alias.photo,
                        email = _rech_alias.email,
                        adresse = _rech_alias.adresse,
                        codePostale = _rech_alias.codePostale,
                        raisonSociale = _rech_alias.raisonSociale,
                        denominationSociale = _rech_alias.denominationSociale,
                        categorieEntreprise = _rech_alias.categorieEntreprise,
                        identificationRCCM = _rech_alias.identificationRccm,
                        identificationFiscale = _rech_alias.identificationFiscale,
                    }

                };

                return Ok(e);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));

            }
        }


    
        [Authorize]
        [HttpPost("alias")]
        public async Task<IActionResult> CreerUnAlias([FromBody] QueryCreationAlias _body)
        {

            string _desc_route = "Creation d'alias";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new QueryCreationAliasValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );

                    return BadRequest(problem);
                }


                Model.t_client data_client = GetInfoClient();


                GeneraleRetour r = await _serviceAlias.CreerUnAlias(DtoAIP.PlateformeAPI.MOBILE.ToString(), data_client, _body.compte, _body.type, _body.cle, IdDemande);

                if (Tools.Tools.RetourIsSucces(r.status))
                {


                    switch (r.status)
                    {
                        case 200:
                            t_alias repAlias = JsonConvert.DeserializeObject<t_alias>(r.data);
                            RepCreateAliasDto newalias = new RepCreateAliasDto
                            {
                                cle = repAlias.valeurAlias,
                                shid = repAlias.shid,
                                type = repAlias.typeAlias,
                                compte = _body.compte,
                                pays = repAlias.paysResidence,
                            };
                            return StatusCode(200, newalias);
                        case 202:

                            RepCreateAliasDto aliasAConfirmer = JsonConvert.DeserializeObject<RepCreateAliasDto>(r.data);

                            return StatusCode(200, aliasAConfirmer);
                    }

                }
                else
                {

                    var problem = GeneraleRetour.BuildProblemResponse(
                       r, instance: HttpContext.Request.Path);
                    return StatusCode(r.status, problem);
                }

                var p = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, p);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [NonAction]
        [HttpPut("alias")]
        public async Task<IActionResult> ModifierUnAlias([FromBody] QueryModificationAliasClientDto rq)
        {

            string _desc_route = "Modification d'alias";

            //*****************************Verification du corps envoyé ******************************************

            if (string.IsNullOrEmpty(rq.alias))
                return StatusCode(400, new { description = _desc_route, message = "Les données envoyées ne sont pas conformes" });

            try
            {
                GeneraleRetour e = await _serviceAlias.UpdateAlias(rq);

                if (Tools.Tools.RetourIsSucces(e.status))
                {
                    var ret = JsonConvert.DeserializeObject<t_alias>(e.data);
                    return StatusCode(200, ret);
                }
                else
                {
                    var problem = GeneraleRetour.BuildProblemResponse(
                    e, instance: HttpContext.Request.Path);
                    return StatusCode(e.status, problem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);

            }

        }

        [Authorize]
        [HttpPut("alias/{cle}")]
        public async Task<IActionResult> ConfirmationCreationAlias(string cle, [FromBody] QueryConfirmationOtpDto _body)
        {

            string _desc_route = "Confirmation de création d'alias";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new QueryConfirmationOtpDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );

                    return BadRequest(problem);
                }


                Model.t_client data_client = GetInfoClient();

                GeneraleRetour r = await _serviceAlias.ConfirmerLaCreationDunAlias(data_client, cle, _body, IdDemande);

                if (Tools.Tools.RetourIsSucces(r.status))
                {
                    var alias = JsonConvert.DeserializeObject<t_alias>(r.data);

                    RepCreateAliasDto newalias = new RepCreateAliasDto
                    {
                        cle = alias.valeurAlias,
                        shid = alias.shid,
                        type = alias.typeAlias,
                        compte = alias.iban,
                        pays = alias.paysResidence,
                    };

                    return StatusCode(200, newalias);
                }
                else
                {
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, HttpContext.Request.Path));
                }

            }
            catch (Exception ex)
            {
                _logger.LogInformation($"[EndPoint {_desc_route}] ===============================>" + ex.Message);

                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }


        }

        [Authorize]
        [HttpGet("alias/{cle}")]
        public async Task<IActionResult> RecupererUnAlias(string cle)
        {

            string _desc_route = "Recuperer un alias";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrWhiteSpace(cle))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'alias fourni est vide ou invalide.",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }


                Model.t_client resp_client = GetInfoClient();

                t_compte _rech_compte = await _compteRepo.SearchCompteByIbanOrOther(cle, resp_client.Id);
                if (_rech_compte == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "Le compte est inconnu",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }


                /// Recherche de l'alias 
                t_alias alias = await _aliasRepo.SearchAliasByIban(_rech_compte.ibanOrOther);

                if (alias == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                     detail: "Le client n'as pas d'alias dans le système",
                     instance: HttpContext.Request.Path
                     );

                    return NotFound(problem);
                }

                RepAliasDto info_alias = new RepAliasDto
                {
                    cle = alias.valeurAlias,
                    shid = alias.shid,
                    type = alias.typeAlias,
                    compte = alias.iban,
                    pays = alias.paysResidence,
                    client = alias.nomClient
                };

                return StatusCode(200, info_alias);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("alias/{cle}")]
        public async Task<IActionResult> DeleteAlias(string cle)
        {
            string _desc_route = "Suppression d'alias";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrWhiteSpace(cle))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'alias fourni est vide ou invalide.",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }

                Model.t_client resp_client = GetInfoClient();


                GeneraleRetour e = await _serviceAlias.DeleteAlias(resp_client.Id, cle, RAISON_DELETE.DEMANDE_CLIENT.ToString(), IdDemande);
                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

                return StatusCode(204, "L'alias a été supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

     
      
        [Authorize]
        [HttpGet("qr/{qrcode}")]
        public async Task<IActionResult> DecodeCodeQr(string qrcode)
        {

            string _desc_route = "Decoder un QR Code";
            try
            {


                if (string.IsNullOrEmpty(qrcode))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le QR code à décoder est manquant", instance: HttpContext.Request.Path));


                var o = Tools.Tools.DecodeCodeQr(qrcode);
                if (o.Item1 == true)
                    return Ok(o.Item3);
                else
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: o.Item2, instance: HttpContext.Request.Path));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }


        [Authorize]
        [HttpGet("comptes/{id}/qrcode")]
        public async Task<IActionResult> GenererQrCodeSatic(string id)
        {
            string _desc_route = "Génerer un Qr code statique";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de compte est requis", instance: HttpContext.Request.Path));


                Model.t_client resp_client = GetInfoClient();


                t_compte _rech_compte = await _compteRepo.SearchCompteByIbanOrOther(id, resp_client.Id);
                if (_rech_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));

                string myIbanOrOther = _rech_compte.ibanOrOther;

                t_alias _rech_alias = await _aliasRepo.SearchAliasByIban(myIbanOrOther);
                if (_rech_alias == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte n'as pas d'alias dans le système", instance: HttpContext.Request.Path));

                if (_rech_alias.categorie != "P" && _rech_alias.categorie != "C")
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Vous n'avez pas les autorisations", instance: HttpContext.Request.Path));


                string fileName = $"qrcode_{_rech_alias.Id}.png";
                byte[] fileBytes = Tools.Tools.GenerateQrCodeWithLogo(_rech_alias.codeQr, fileName, _aipdata.colorQr);

                if (fileBytes == null)
                    return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));

                return File(fileBytes, "image/png", fileName);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPost("qr")]
        public async Task<IActionResult> GenererCodeQr([FromBody] QrCodeDto _body)
        {

            string _desc_route = "Générer un QR Code";
            try
            {

                // 731 - Transfert par QR code
                // 000 - Paiement par QR code Statique
                //400 - Paiement par QR code Dynamique

                var validator = new QrCodeDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );

                    return BadRequest(problem);
                }


                string codeQr = Tools.Tools.GenerationQR(_body.alias, _body.pays, _body.canal, _body.montant, _body.txId);
                return Ok(new { Qrcode = codeQr });
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        

       
        [Authorize]
        [HttpGet("participants/{filter?}")]
        public async Task<IActionResult> GetParticipant(string? filter)
        {
            const string RouteDesc = "Liste des Participants";

            try
            {
                var query = _interopContext.t_participant.AsNoTracking().Select(p => new{
                        p.codeMembreParticipant,
                        p.statut,
                        p.codeBanque,
                        p.nomOfficiel
                    });

                // On n’applique le filtre que si fourni et différent de ALLL
                if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("ALLL", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => EF.Functions.Like(p.codeMembreParticipant, filter + "%"));
              
                var items = await query
                    .OrderBy(p => p.codeMembreParticipant)
                    .ToListAsync();

                return Ok(new
                {
                    data = items,
                    meta = new MetaDto { total = items.Count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EndPoint {Route}] échec", RouteDesc);
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


  

        [Authorize]
        [HttpPost("transferts")]
        public async Task<IActionResult> CreerUnTransfert([FromBody] QueryBodyTransactionDto _body)
        {

            string _desc_route = "Initiation d'un transfert";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {


                var validator = new QueryBodyTransactionDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );

                    return BadRequest(problem);
                }

                Model.t_client data_client = GetInfoClient();

                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(_body.compte, data_client.Id);

                if (data_compte == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "Le compte est inconnu",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }


                string myIban = data_compte.ibanOrOther;

                t_alias alias_client_connecte = await _aliasRepo.SearchAliasByIban(myIban);

                if (alias_client_connecte == null)

                {
                    var problem = GeneraleRetour.BuildNotFound(
                      detail: "Le compte n'as pas d'alias dans le système",
                      instance: HttpContext.Request.Path
                  );

                    return NotFound(problem);
                }

                if (_body.action == "receive_now") // par 500 si son client est un Commerçant Type C
                    if (alias_client_connecte.categorie == "C")
                        _body.canal = "500";

                if (Tools.Tools.canal_BesoinIDTrans(_body.canal))
                    if (string.IsNullOrEmpty(_body.txId))
                        _body.txId = Tools.Tools.GenerateAlphaNumeriquevalue(30);

                if (string.IsNullOrEmpty(_body.txId))
                    _body.txId = null;

                GeneraleRetour res_transaction = new GeneraleRetour();

                switch (_body.action)
                {
                    case "send_now": // Transfert immédiat

                        if (string.IsNullOrWhiteSpace(_body.motif))
                            _body.motif = "Transfert immédiat";
                        res_transaction = await _serviceTransfert.InitierTransfert(alias_client_connecte.valeurAlias, _body, IdDemande);
                       
                        break;
                    case "receive_now": // Demande de paiement

                        if (string.IsNullOrWhiteSpace(_body.motif))
                            _body.motif = "Paiement";

                        GeneraleRetour intie_rtp = await _serviceTransfert.InitierDemandePaiement(alias_client_connecte.valeurAlias, _body.alias, _body, IdDemande);

                        if (!Tools.Tools.RetourIsSucces(intie_rtp.status))
                            return StatusCode(intie_rtp.status, GeneraleRetour.BuildProblemResponse(intie_rtp));

                        var rtp = JsonConvert.DeserializeObject<t_transfert>(intie_rtp.data);
                        res_transaction = await _serviceTransfert.ConfirmerEtEnvoyerDemandePaiement(rtp, IdDemande);

                        break;
                    case "send_schedule": // Transfert programmé et transaction abonnement

                    default:
                        return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
                }


                if (Tools.Tools.RetourIsSucces(res_transaction.status))
                {

                    var transaction = JsonConvert.DeserializeObject<t_transfert>(res_transaction.data);
                    TransactionDto t = await _serviceTransfert.ConvertirTransfertEnTransfertDto(transaction, data_compte.ibanOrOther);

                    return StatusCode(200, t);
                }
                else
                {

                    _logger.LogError($"[EndPoint {_desc_route}] ===============================> {res_transaction.status} ---  {res_transaction.detail}");
                    return StatusCode(res_transaction.status, GeneraleRetour.BuildProblemResponse(res_transaction, instance: HttpContext.Request.Path));

                }

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }


        }


    
        [Authorize]
        [HttpPut("transferts/{id}")]
        [HttpPut("transferts/test/{id}")]
        public async Task<IActionResult> ConfirmerUnTransfert(string id, [FromBody] QueryConfirmTransactionDto _body)
        {

            string _desc_route = "Confirmation d'un transfert";
            string IdDemande = RecupererIdDemandeEnCours();

            bool isTest = HttpContext.Request.Path.Value.Contains("/test/");

            try
            {

                var json = System.Text.Json.JsonSerializer.Serialize(_body,new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                _logger.LogInformation("Confirmation transfert - Body reçu: {Body}", json);


                Model.t_client data_client = GetInfoClient();

                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant de la transaction est requis",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }


                var validator = new QueryConfirmTransactionDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );

                    return BadRequest(problem);
                }

                var _data = await (
                    from t in _interopContext.t_transfert
                    join c in _interopContext.t_compte
                    on t.compteClientPayeur equals c.ibanOrOther
                    where t.endToEndId == id
                    && t.is_delete != true
                    && c.is_delete != true
                    && c.r_client_id == data_client.Id
                    && ( t.etape == ETAPE_TRANSFERT.INITIEE || t.etape == ETAPE_TRANSFERT.ATTENTE_REPONSE_CLIENT)
                    select new
                    {
                        Transfert = t,
                        data_compte = c
                    }
                    ).FirstOrDefaultAsync();


                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction ne figure pas parmi celles à confirmer.", instance: HttpContext.Request.Path));


                t_transfert transfert = _data.Transfert;


                if ((_body.montant > 0 && _body.montant != transfert.montant) || (!string.IsNullOrEmpty(_body.motif) && transfert.motif != transfert.motif))
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Les données de confirmation ne sont pas conformes", instance: HttpContext.Request.Path));


                if (Tools.Tools.canalEstCanalDemandePaiement(_data.Transfert.canalCommunication))
                {

                    List<InvalidParam> invalidParams = new List<InvalidParam>();

                    if (!_body.latitude.HasValue)
                        invalidParams.Add(new InvalidParam { name = "latitude", reason = "La latitude de l'utilisateur est requise" });

                    if (!_body.longitude.HasValue)
                        invalidParams.Add(new InvalidParam { name = "longitude", reason = "La longitude de l'utilisateur est requise" });

                    var problem = GeneraleRetour.BuildBadRequest(detail: "Les coordonnées du payeur sont obligatoires", instance: HttpContext.Request.Path, invalidParams: invalidParams);

                    if (invalidParams.Count > 0) return BadRequest(problem);

                    transfert.longitudeClientPayeur = _body.longitude.ToString();
                    transfert.latitudeClientPayeur = _body.latitude.ToString();
                }


                transfert.dateConfirmation = _body.confirmationDate;
                transfert.methodeConfirmation = _body.confirmationMethode;

                GeneraleRetour e = new GeneraleRetour();

                if (isTest)
                    e = await _serviceTransfert.ConfirmerTransfertTest(transfert, IdDemande);
                else
                    e = await _serviceTransfert.ConfirmerTransfert(transfert, IdDemande);


                if (Tools.Tools.RetourIsSucces(e.status))
                {
                    TransactionDto t = await _serviceTransfert.ConvertirTransfertEnTransfertDto(transfert, _data.data_compte.ibanOrOther);
                    return StatusCode(200, t);
                }
                else
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }


        }



    
     
        [Authorize]
        [HttpGet("transferts/{id}/reponses")]
        [Produces("application/json", "text/event-stream")]
        public async Task<IActionResult> ObtenirStatutTransfert(string id)
        {
            const string _desc_route = "Obtenir le statut d'un transfert";

            if (string.IsNullOrWhiteSpace(id))
            {
                var problem = GeneraleRetour.BuildBadRequest(
                    detail: "L'identifiant de la transaction est requis",
                    instance: HttpContext.Request.Path
                );
                return BadRequest(problem);
            }

            // Si le client demande SSE
            var accept = Request.Headers["Accept"].ToString();
            var wantsSse = accept?.IndexOf("text/event-stream", StringComparison.OrdinalIgnoreCase) >= 0;
            if (wantsSse)
                return await StreamStatutTransfertSse(id, HttpContext);

            // ---- JSON one-shot (comportement classique) ----
            try
            {
                var data_client = GetInfoClient(); // ta méthode existante
                if (data_client is null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail : "Autorisations insuffisantes", instance: HttpContext.Request.Path));

                await using var db = await _dbFactory.CreateDbContextAsync(HttpContext.RequestAborted);

                var t = await db.t_transfert
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.endToEndId == id && x.is_delete != true, HttpContext.RequestAborted);

                if (t is null)
                    return NotFound(GeneraleRetour.BuildNotFound("La transaction est introuvable dans le système", HttpContext.Request.Path));

             
                var comptesClient = await db.t_compte
                    .AsNoTracking()
                    .Where(c => c.is_delete != true && c.r_client_id == data_client.Id)
                    .Select(c => c.ibanOrOther)
                    .ToListAsync(HttpContext.RequestAborted);

                var isPayeur = comptesClient.Contains(t.compteClientPayeur);
                var isPaye = comptesClient.Contains(t.compteClientPaye);
                var isOwner = isPayeur || isPaye;

                if (!isOwner)
                    return NotFound(GeneraleRetour.BuildNotFound("La transaction est introuvable dans le système", HttpContext.Request.Path));


                var finalStatut =
             t.etape == ETAPE_TRANSFERT.REJETE
                ? STATUT_TRANSFERT.rejete.ToString()
                  : (t.statut_general == STATUT_TRANSFERT.irrevocable && (t.etape == ETAPE_TRANSFERT.VALIDE || t.etape == ETAPE_TRANSFERT.DEBIT_ECHOUE || t.etape == ETAPE_TRANSFERT.REJETE)
                      ? STATUT_TRANSFERT.irrevocable.ToString()
                      : t.statut_general?.ToString());

                var dto = new TransactionStatutDto
                {
                    statut = finalStatut,
                    codeRejet = (finalStatut == STATUT_TRANSFERT.irrevocable.ToString()? null: t.codeRejet),
                    dateIrrevocabilite = (finalStatut == STATUT_TRANSFERT.irrevocable.ToString() ? t.dateHeureIrrevocabilite : null)
                };

                // Completer avec la description de l'erreur si Transfert Rejete ou désactivé
                if (dto.statut == STATUT_TRANSFERT.rejete.ToString() || dto.statut == STATUT_TRANSFERT.desactive.ToString())
                {
                    string? libelle = null;
                    const string DEFAULT_MSG = "Transfert echoué";

                    if (dto.statut == STATUT_TRANSFERT.rejete.ToString())
                    {

                        libelle = null;
                        if (!string.IsNullOrWhiteSpace(dto.codeRejet))
                        {
                            libelle = await _codeErreurRepo.GetLibelleErreurAsync(
                                dto.codeRejet,
                                tag_erreur.CODE_RAISON_REJET.ToString()
                            );
                        }
                    }

                    if (dto.statut == STATUT_TRANSFERT.desactive.ToString())
                    {
                        dto.statut = STATUT_TRANSFERT.rejete.ToString();
                        libelle = "Transfert échoué – fonds retournés";
                    }


                    // Si pas de code, ou si le repo ne renvoie rien de probant => message par défaut
                    dto.detailRejet = !string.IsNullOrWhiteSpace(libelle) ? libelle : DEFAULT_MSG;

                }


                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Route}] Erreur JSON one-shot pour id {Id}", _desc_route, id);
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        // ===== SSE =====
        [NonAction]
        private async Task<IActionResult> StreamStatutTransfertSse(string id, HttpContext ctx)
        {
            const int POLL_SECONDS = 2;

            try
            {
                var data_client = GetInfoClient();
                await using var db = await _dbFactory.CreateDbContextAsync(ctx.RequestAborted);

                var ct = ctx.RequestAborted;

                // Pré-check existence (scopé au client si présent)
                var baseQuery = db.t_transfert
                    .AsNoTracking()
                    .Where(t => t.endToEndId == id && t.is_delete != true);



                if (data_client is not null)
                {
                    baseQuery = baseQuery.Where(t =>
                        db.t_compte.AsNoTracking().Any(c =>
                            c.is_delete != true &&
                            c.r_client_id == data_client.Id &&
                            (c.ibanOrOther == t.compteClientPayeur || c.ibanOrOther == t.compteClientPaye)
                        )
                    );
                }


                var count = await baseQuery.CountAsync(ct);
                if (count == 0)
                {
                    return new NotFoundObjectResult(
                        GeneraleRetour.BuildNotFound("La transaction est introuvable dans le système", ctx.Request.Path)
                    );
                }
                if (count > 1)
                {
                    _logger.LogWarning("SSE transfert {Id}: {Count} lignes trouvées. On prendra la plus récente.", id, count);
                }

                var res = ctx.Response;
                res.StatusCode = StatusCodes.Status200OK;
                res.Headers["Content-Type"] = "text/event-stream";
                res.Headers["Cache-Control"] = "no-cache";
                res.Headers["Connection"] = "keep-alive";
                res.Headers["X-Accel-Buffering"] = "no";
                await res.Body.FlushAsync(ct);

                STATUT_TRANSFERT? lastStatut = null;
                string? lastCode = null;
                DateTimeOffset? lastIrrev = null;

                while (!ct.IsCancellationRequested)
                {
                    // Snapshot minimal
                    var snap = await baseQuery
                        .OrderByDescending(t => t.dateHeureIrrevocabilite) 
                        .ThenByDescending(t => t.r_createdon)              
                        .Select(t => new
                        {
                            statut = t.statut_general,
                            etape = t.etape, // interne uniquement
                            t.codeRejet,
                            dateIrrevocabilite = t.dateHeureIrrevocabilite
                        })
                        .FirstOrDefaultAsync(ct);

                    if (snap is null)
                    {
                        await SendEventAsync(res, "deleted",
                            Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Transfert supprimé" }),
                            ctx);
                        break;
                    }

                    // ----- Statut effectif (sans exposer l'étape) -----
                    var effectiveStatut =
                        (snap.etape == ETAPE_TRANSFERT.REJETE)
                            ? STATUT_TRANSFERT.rejete
                            : (snap.statut == STATUT_TRANSFERT.irrevocable
                                ? ((snap.etape == ETAPE_TRANSFERT.VALIDE || snap.etape == ETAPE_TRANSFERT.REJETE || snap.etape == ETAPE_TRANSFERT.DEBIT_ECHOUE)
                                    ? STATUT_TRANSFERT.irrevocable
                                    : STATUT_TRANSFERT.initie)
                                : snap.statut);

                    var changed = effectiveStatut != lastStatut
                                  || snap.codeRejet != lastCode
                                  || snap.dateIrrevocabilite != lastIrrev;

                    if (changed)
                    {
                        lastStatut = effectiveStatut;
                        lastCode = snap.codeRejet;
                        lastIrrev = snap.dateIrrevocabilite;

                        string? detailRejet = null;

                        // DEMANDE: si statut = DESACTIVE ➜ transformer en REJETE et sortir immédiatement
                        if (lastStatut == STATUT_TRANSFERT.desactive)
                        {
                            lastStatut = STATUT_TRANSFERT.rejete; // forcer le rejet
                            detailRejet = "Transfert échoué – retour de fonds";

                            var payloadDesactive = new
                            {
                                statut = lastStatut.ToString(),    // "REJETE"
                                codeRejet = lastCode,             
                                detailRejet,                     
                                dateIrrevocabilite = (DateTimeOffset?)null 
                            };

                            var jsonDesactive = Newtonsoft.Json.JsonConvert.SerializeObject(payloadDesactive);
                            await SendEventAsync(res, "statut-update", jsonDesactive, ctx);
                            await SendEventAsync(res, "finalized", jsonDesactive, ctx); // finaliser tout de suite
                            break; //  sortir immédiatement
                        }

                        // Cas rejet: chercher un libellé sinon défaut
                        if (lastStatut == STATUT_TRANSFERT.rejete)
                        {
                            const string DEFAULT_MSG = "Transfert échoué";
                            string? libelle = null;

                            if (!string.IsNullOrWhiteSpace(lastCode))
                            {
                                libelle = await _codeErreurRepo.GetLibelleErreurAsync(
                                    lastCode.Trim(),
                                    tag_erreur.CODE_RAISON_REJET.ToString()
                                );
                            }

                            detailRejet = !string.IsNullOrWhiteSpace(libelle) ? libelle : DEFAULT_MSG;
                        }

                        var payload = new
                        {
                            statut = lastStatut?.ToString(),
                            codeRejet = (lastStatut == STATUT_TRANSFERT.irrevocable) ? null : lastCode,
                            detailRejet,
                            dateIrrevocabilite = (lastStatut == STATUT_TRANSFERT.irrevocable) ? lastIrrev : (DateTimeOffset?)null
                        };

                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                        await SendEventAsync(res, "statut-update", json, ctx);

                        // Terminer si final
                        if (lastStatut == STATUT_TRANSFERT.irrevocable || lastStatut == STATUT_TRANSFERT.rejete)
                        {
                            await SendEventAsync(res, "finalized", json, ctx);
                            break;
                        }
                    }
                    else
                    {
                        await res.WriteAsync($": keep-alive {DateTimeOffset.UtcNow:o}\n\n", ct);
                        await res.Body.FlushAsync(ct);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(POLL_SECONDS), ct);
                }
            }
            catch (OperationCanceledException)
            {
                // client parti
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur SSE pour transfert {Id}", id);
            }

            return new EmptyResult();
        }

        [NonAction]
        private static async Task SendEventAsync(HttpResponse res, string eventName, string json, HttpContext ctx)
        {
            await res.WriteAsync($"event: {eventName}\n", ctx.RequestAborted);
            await res.WriteAsync($"id: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}\n", ctx.RequestAborted);
            await res.WriteAsync($"data: {json}\n\n", ctx.RequestAborted);
            await res.Body.FlushAsync(ctx.RequestAborted);
        }

        [Authorize]
        [HttpGet("transferts/{id}")]
        public async Task<IActionResult> DetailsTransfert(string id)
        {

            string _desc_route = "Détails d'un transfert";

            try
            {


                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "La réference de la transaction est requise", instance: HttpContext.Request.Path));

                Model.t_client data_client = GetInfoClient();

                var _data = await (
               from e in _interopContext.t_transfert
               from c in _interopContext.t_compte
               where e.endToEndId == id
               && e.is_delete != true
               && c.is_delete != true
               && c.r_client_id == data_client.Id
               && (c.ibanOrOther == e.compteClientPayeur || c.ibanOrOther == e.compteClientPaye)
               select new
               {
                   Transfert = e,
                   data_compte = c
               }).FirstOrDefaultAsync();


                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction est introuvable dans le système", instance: HttpContext.Request.Path));


                TransactionDto t = await _serviceTransfert.ConvertirTransfertEnTransfertDto(_data.Transfert, _data.data_compte.ibanOrOther);

                return StatusCode(200, t);

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }


        }



        [Authorize]
        [HttpGet("transferts/{id}/tickets")]
        public async Task<IActionResult> ImprimerTransfert(string id)
        {
            string _desc_route = "Imprimer un reçu de transfert";

            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "La réference de la transaction est requise", instance: HttpContext.Request.Path));

                Model.t_client data_client = GetInfoClient();

                var _data = await (
                    from t in _interopContext.t_transfert
                    from c in _interopContext.t_compte
                    where t.endToEndId == id
                    && t.is_delete != true
                    && c.is_delete != true
                    && c.r_client_id == data_client.Id
                    && (c.ibanOrOther == t.compteClientPayeur || c.ibanOrOther == t.compteClientPaye)
                    select new
                    {
                        Transfert = t,
                        data_compte = c
                    }).FirstOrDefaultAsync();



                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction est introuvable dans le système", instance: HttpContext.Request.Path));


                t_transfert data_transfert = _data.Transfert;

                bool isClientPayeur = (_data.data_compte.ibanOrOther == data_transfert.compteClientPayeur);

                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "facture");
                string fileName = $"Facture_{data_transfert.endToEndId}.pdf";
                string nomDuParticipant = "";

                if (isClientPayeur)
                {
                    t_participant p = await _participantrepo.searchParticipant(data_transfert.codeMembreParticipantPaye);
                    if (p != null)
                    {
                        nomDuParticipant = p.nomOfficiel;
                    }
                }

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Générer le PDF
                string cheminComplet = await _serviceEtat.GenererRecuPaiementPdf(data_transfert, isClientPayeur, folderPath, fileName, nomDuParticipant);

                if (!System.IO.File.Exists(cheminComplet))
                {
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le fichier PDF n'a pas pu être généré", instance: HttpContext.Request.Path)); ;
                }

                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(cheminComplet);
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpPut("transferts/{id}/annulations")]
        public async Task<IActionResult> DemanderAnnulationTransfert(string id, [FromBody] QueryAnnulationMobileDto _body)
        {

            string _desc_route = "Annulation d'operation";
            try
            {


                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la transaction est requis", instance: HttpContext.Request.Path));

                var raisonPossible = new List<string> { "DUPL", "AC03", "AM09", "FRAD", "SVNR" };

                var validator = new QueryAnnulationMobileDtoValidator();
                var results = validator.Validate(_body);

                List<InvalidParam> invalidParams = new();

                if (!results.IsValid)
                {
                    invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                }


                if (!raisonPossible.Contains(_body.raison))
                {
                    invalidParams.Add(new InvalidParam
                    {
                        name = "raison",
                        reason = $"La valeur '{_body.raison}' n'est pas autorisée. Les raisons possibles sont : {string.Join(", ", raisonPossible)}"
                    });
                }

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", invalidParams: invalidParams, instance: HttpContext.Request.Path));

                Model.t_client data_client = GetInfoClient();


                var _data = await (
               from e in _interopContext.t_transfert
               from c in _interopContext.t_compte
               where e.endToEndId == id
               && e.is_delete != true
               && c.is_delete != true
               && c.r_client_id == data_client.Id
               && (c.ibanOrOther == e.compteClientPayeur)
               select new
               {
                   Transfert = e,
                   data_compte = c
               }).FirstOrDefaultAsync();



                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction est introuvable dans le système", instance: HttpContext.Request.Path));

                if (_data.Transfert.statut_general != STATUT_TRANSFERT.irrevocable)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Le statut de la transaction ne permet pas de faire cette action", instance: HttpContext.Request.Path));


                GeneraleRetour r = await _serviceTransfert.DemanderAnnulationTransfert(_data.Transfert, _body);

                if (Tools.Tools.RetourIsSucces(r.status))
                    return StatusCode(200, "Demande d'annulation envoyée");
                else
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, HttpContext.Request.Path));

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

   
   


        [HttpPost("otp/{challengeId}")]
        public async Task<IActionResult> GenererOtp(string challengeId)
        {
            string _desc_route = "Régenerer un OTP";

            try
            {

                if (string.IsNullOrEmpty(challengeId))
                {
                    var problem = GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour { status = 400, detail = "Le challenge est requis pour la génération", },
                       instance: HttpContext.Request.Path
                   );

                    return BadRequest(problem);
                }

                t_otp otp = _interopContext.t_otp.Where(c => c.is_delete != true && c.challengeId == challengeId).FirstOrDefault();

                if (otp == null)
                {
                    var problem = GeneraleRetour.BuildForbid(
                     detail: "Session invalide ou expirée",
                     instance: HttpContext.Request.Path
                 );

                    return StatusCode(403, problem);

                }

                /// Recherche les infos du client
                t_client data_client = await _interopContext.t_client
               .Where(p => p.Id == otp.r_client_id_fk && p.is_delete != true)
               .FirstOrDefaultAsync();

                if (data_client == null)
                {
                    var problem = GeneraleRetour.BuildForbid(detail: "Session invalide ou expirée", instance: HttpContext.Request.Path);
                    return StatusCode(403, problem);
                }


                string tel = "";
                // Si c'est confirmation MBNO
                if (otp.type == type_otp.CREATION_ALIAS)
                {
                    t_creation_alias tca = await _interopContext.t_creation_alias.Where(p => p.Id.ToString() == otp.idOperationParent && p.is_delete != true).FirstOrDefaultAsync();
                    if (tca != null)
                    {
                        tel = tca.telephoneClient;
                    }

                }


                type_modele t = (type_modele)Tools.Tools.EquivalenceOtpEnModele(otp.type);

                await _serviceMessagerie.sendMessageAuClient(t, tel, data_client, otp);

                return Ok(new { message = "OTP envoyé avec succès pour la réinitialisation du mot de passe.", challengeId = otp.challengeId, contactMasked = Tools.Tools.MaskPhone(data_client.telephone), emailMasked = Tools.Tools.MaskEmail(data_client.email) });
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }






         }
}