using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ask.ContextDb;
using ask.DtoAppMobile;
using ask.DtoAppMobile.Alias;
using ask.DtoAppMobile.Securite;
using ask.Dtos;
using ask.Dtos.Request.Auth;
using ask.Event;
using ask.Interface;
using ask.Model;
using ask.RequestToSendDto;
using ask.ServicceAIP;
using ask.ServicesSecure.Dtos;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly IdatasRepo _datarepo;
        private readonly IParticipantsRepo _participantrepo;
        private readonly ServiceAIF _ServiceAIF;
        private readonly IcompteRepo _compteRepo;
        private readonly IaliasRepo _aliasRepo;
        private readonly IotpRepo _otpRepo;
        private readonly ServiceAlias _serviceAlias;
        private readonly ServiceTransfert _serviceTransfert;
        private readonly ServiceMessagerie _serviceMessagerie;
        private readonly ServiceEtat _serviceEtat;
        private readonly ServiceSecurity _serviceSecurity;
        private readonly IrevendicationRepo _revendicationRepo;
        private readonly ICodeErreurRepo _codeErreurRepo;

       
        private readonly IConfiguration _configuration;
        private readonly AIPDATA _aipdata;
        private readonly PARAM_MESSAGE _paramdata;
        private readonly ILogger<MobileController> _logger;
        private readonly SecurityConfig _securityconfig;
        private readonly IDbContextFactory<askContext> _dbFactory;


        //private readonly ILogger _logger;
        public askController(IrevendicationRepo revendicationrepo, ICodeErreurRepo codeErreurRepo, IDbContextFactory<InteropContext> dbFactory, IDemandeRepo demandeRepo, ItransfertAutoriseRepo transfertAutorepo, ItransfertRepo transfertRepo, Iannulation_transfert AnnulationTransfertRepo, EventService eventService, ServiceSecurity securityService, IOptions<SecurityConfig> securityconfig, ServiceAlias serviceAlias, ServiceTransfert serviceTransfert, ServiceMessagerie serviceMessagerie, ServiceEtat serviceEtat, IdatasRepo datarepo, IotpRepo otpRepo, InteropContext interopContext, IOptions<AIPDATA> aipdata, IOptions<PARAM_MESSAGE> paramdata, IConfiguration configuration, IWebHostEnvironment env, ILogger<MobileController> logger, ServiceAIF serviceAIF, IaliasRepo aliasRepo, IcompteRepo compteRepo, IParticipantsRepo participantrepo)
        {

            _configuration = configuration;
            _dbFactory = dbFactory;
            _env = env;
            _aipdata = aipdata.Value;
            _paramdata = paramdata.Value;
            _logger = logger;
            _securityconfig = securityconfig.Value;
            _ServiceAIF = serviceAIF;
            _serviceMessagerie = serviceMessagerie;
            _serviceEtat = serviceEtat;
            _askContext = askContext;
            _revendicationRepo = revendicationrepo;
            _codeErreurRepo = codeErreurRepo;
            _serviceSecurity = securityService;
            _datarepo = datarepo;
            _compteRepo = compteRepo;
            _otpRepo = otpRepo;
            _aliasRepo = aliasRepo;
            _serviceAlias = serviceAlias;
            _serviceTransfert = serviceTransfert;     
            _participantrepo = participantrepo;
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
        public Model.t_client GetInfoClient()
        {
            if (HttpContext.Items.ContainsKey("Client"))
            {
                return (Model.t_client)HttpContext.Items["Client"];
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



        [HttpPost("register")]
        public async Task<IActionResult> Inscription([FromBody] InscriptionDto _body)
        {
            string _desc_route = "Inscription";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new InscriptionDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }

                /// Enregistrement dans la table des demandes d'inscriptions

                t_register t = new t_register
                {
                    numerocompte = _body.numerocompte,
                    nom = _body.nom,
                    email = _body.email,
                    telephone = _body.telephone,
                    password = _body.password
                };


                await _interopContext.t_register.AddAsync(t);
                await _interopContext.SaveChangesAsync();

                GeneraleRetour b = await _serviceSecurity.Register(t, IdDemande);

                if (!Tools.Tools.RetourIsSucces(b.status))
                {
                    t.motif_rejet = b.detail;
                    t.statut = statut_register.REJETE;
                    _interopContext.t_register.Update(t);
                    await _interopContext.SaveChangesAsync();

                    return StatusCode(b.status, GeneraleRetour.BuildProblemResponse(b, instance: HttpContext.Request.Path));
                }

                t.statut = statut_register.ACCEPTE;
                _interopContext.t_register.Update(t);
                await _interopContext.SaveChangesAsync();
                return Ok("Inscription effectuée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message.ToString()}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [HttpPost("auth/register")]
        public async Task<IActionResult> InscriptionAvecConfirmation([FromBody] InscriptionDto _body)
        {
            string _desc_route = "Inscription";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new InscriptionDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }

                /// Enregistrement dans la table des demandes d'inscriptions

                t_register rg = new t_register
                {
                    numerocompte = _body.numerocompte,
                    nom = _body.nom,
                    email = _body.email,
                    telephone = _body.telephone,
                    password = _body.password,
                    statut = statut_register.WAIT_CONFIRM
                };

                await _interopContext.t_register.AddAsync(rg);
                await _interopContext.SaveChangesAsync();

                t_otp o = await _otpRepo.genererOtp(0, rg.Id.ToString(), type_otp.CONFIRMATION_REGISTER, _paramdata.sms.validite_otp ?? 6);

                await _serviceMessagerie.sendMessageAuRegister(rg, o);

                return Ok(new { message = "OTP envoyé avec succès pour la confirmation de l'inscription.", challengeId = o.challengeId, contactMasked = Tools.Tools.MaskPhone(rg.telephone), emailMasked = Tools.Tools.MaskEmail(rg.email) }
);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message.ToString()}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [HttpPost("auth/register/confirm")]
        public async Task<IActionResult> ConfirmerInscription([FromBody] QueryConfirmationOtpRegisterDto _body)
        {
            string _desc_route = "Confirmation de l'inscription";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new QueryConfirmationOtpRegisterDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }


                (int res_otp, t_otp o) = await _otpRepo.verifieOtpAndChallenge(_body.otp, type_otp.CONFIRMATION_REGISTER, _body.challenge);

                switch (res_otp)
                {
                    case 0:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Expiré", instance: HttpContext.Request.Path));
                    case -1:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));
                    case 1:
                        break;
                }
                ;

                Model.t_register rg = await _interopContext.t_register.Where(p => p.Id.ToString() == o.idOperationParent && p.is_delete != true).FirstOrDefaultAsync();

                if (rg == null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));


                rg.statut = statut_register.CONFIRMER;

                _interopContext.t_register.Update(rg);
                await _interopContext.SaveChangesAsync();

                GeneraleRetour b = await _serviceSecurity.Register(rg, IdDemande);

                if (!Tools.Tools.RetourIsSucces(b.status))
                {
                    rg.motif_rejet = b.detail;
                    rg.statut = statut_register.REJETE;
                    _interopContext.t_register.Update(rg);
                    await _interopContext.SaveChangesAsync();

                    return StatusCode(b.status, GeneraleRetour.BuildProblemResponse(b, instance: HttpContext.Request.Path));
                }

                rg.statut = statut_register.ACCEPTE;
                _interopContext.t_register.Update(rg);
                await _interopContext.SaveChangesAsync();
                return Ok("Inscription effectuée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message.ToString()}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


      

        [HttpPost("auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Authentification([FromBody] ConnexionDto _body)
        {
            string _desc_route = "Authentification";

            try
            {

                string IdDemande = RecupererIdDemandeEnCours();

                var validator = new ConnexionDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }


                /// Recherche du client dans la base
                var resp_query_client = await _interopContext.t_client
                  .Where(c => c.security_username == _body.identifiant && c.is_delete != true)
                  .FirstOrDefaultAsync();

                if (resp_query_client == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Authentification echouée", instance: HttpContext.Request.Path));

                GeneraleRetour data_res_auth = await _serviceSecurity.AuthentificationUserClient(_body.identifiant, _body.password, IdDemande);


                var resp_query_alias = await _interopContext.t_alias
                .Where(c => c.r_client_id_fk == resp_query_client.Id && c.is_delete != true)
                .Select(o => new { alias = o.valeurAlias, iban = o.iban }) // Ajoute le champ du compte ici
                .ToListAsync();


                var resp_query_compte = await _interopContext.t_compte
                    .Where(c => c.r_client_id == resp_query_client.Id && c.is_delete != true)
                    .Select(o => o.codeAgence + o.numeroCompte)
                    .ToListAsync();


                if (!Tools.Tools.RetourIsSucces(data_res_auth.status))
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: data_res_auth.detail, instance: HttpContext.Request.Path));

                AuthSecurityRetourDto res_auth = JsonConvert.DeserializeObject<AuthSecurityRetourDto>(data_res_auth.data);

                if (string.IsNullOrEmpty(res_auth.access_token))
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Authentification echouée", instance: HttpContext.Request.Path));


                /// Retourne la session , les comptes et les alias
                return Ok(new { session = res_auth, comptes = resp_query_compte, alias = resp_query_alias });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message.ToString()}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RafraichirLeToken([FromBody] QueryRefreshToken _body)
        {
            string _desc_route = "Rafraichir le token";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {


                var validator = new QueryRefreshTokenValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    return BadRequest(GeneraleRetour.BuildBadRequest(instance: HttpContext.Request.Path, detail: "Les données ne sont pas conformes", invalidParams: invalidParams));
                }

                Model.t_client data_client = GetInfoClient();

                if (data_client == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le client n'existe pas dans notre système", instance: HttpContext.Request.Path));

                GeneraleRetour res = await _serviceSecurity.RefreshToken(_body.refresh_token, RecupererToken(), IdDemande);

                if (!Tools.Tools.RetourIsSucces(res.status))
                    return StatusCode(res.status, GeneraleRetour.BuildProblemResponse(res, instance: HttpContext.Request.Path));


                AuthSecurityRetourDto res_auth = JsonConvert.DeserializeObject<AuthSecurityRetourDto>(res.data);

                return Ok(res_auth);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [NonAction]
        [HttpPost("test")]
        public async Task<IActionResult> TesterEnvoiMessage()
        {
            string _desc_route = "Tester";
            string IdDemande = RecupererIdDemandeEnCours();
            try
            {

                Model.t_client data_client = GetInfoClient();

                t_otp o = await _otpRepo.genererOtp(data_client.Id, data_client.Id.ToString(), type_otp.RESET_CODE_PIN, _paramdata.sms.validite_otp ?? 6);

               await _serviceMessagerie.sendMessageAuClient(type_modele.INITIALISATION_CODE_PIN, "", data_client, o);

                return Ok("ok test");

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("code-pin")]
        public async Task<IActionResult> DefinirLeCodePin([FromBody] CodePinClientBodyDto _body)
        {
            string _desc_route = "Définition de code PIN";
            string IdDemande = RecupererIdDemandeEnCours();
            try
            {

                Model.t_client data_client = GetInfoClient();


                int taille_code_pin = _securityconfig.length_pin > 0 ? _securityconfig.length_pin : 4;

                var validator = new CodePinClientBodyDtoValidator(taille_code_pin);
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );

                    return BadRequest(problems);
                }

                GeneraleRetour data_res = await _serviceSecurity.DefinirCodePIN(_body, RecupererToken(), IdDemande);

                if (Tools.Tools.RetourIsSucces(data_res.status))
                    return Ok("Enregistrement du code PIN effectué avec succès");
                else
                    return StatusCode(data_res.status, GeneraleRetour.BuildProblemResponse(data_res, instance: HttpContext.Request.Path));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPut("code-pin")]
        public async Task<IActionResult> ModifierLeCodePin([FromBody] UpdateCodePinClientDto _body)
        {
            string _desc_route = "Modification du code PIN";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                Model.t_client data_client = GetInfoClient();

                int taille_code_pin = _securityconfig.length_pin;
                if (taille_code_pin == 0)
                    taille_code_pin = 4;

                var validator = new UpdateCodePinClientDtoValidator(taille_code_pin);
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }


                GeneraleRetour e = await _serviceSecurity.ModificationCodePin(data_client, _body, RecupererToken(), IdDemande); ;

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

                return Ok("Modification du code PIN effectuée avec succès");


            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("code-pin/verify")]
        public async Task<IActionResult> VerifierLeCodePin([FromBody] CodePinClientBodyDto _body)
        {
            string _desc_route = "Vérification du code PIN";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {


                int taille_code_pin = _securityconfig.length_pin;
                if (taille_code_pin == 0)
                    taille_code_pin = 4;

                var validator = new CodePinClientBodyDtoValidator(taille_code_pin);
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }


                Model.t_client data_client = GetInfoClient();

                GeneraleRetour e = await _serviceSecurity.VerificationCodePIN(_body, data_client.security_user_id, RecupererToken(), IdDemande);

                if (Tools.Tools.RetourIsSucces(e.status))
                    return Ok("Vérification du code PIN effectuée avec succès");
                else
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("code-pin/reset/otp")]
        public async Task<IActionResult> ReinistialisationDuCodePin()
        {
            string _desc_route = "Rénistialisation du code PIN";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                Model.t_client data_client = GetInfoClient();

                if (data_client == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'utilisateur est introuvable dans le sytème", instance: HttpContext.Request.Path));

                /// Envoi de OTP sur le numero
                t_otp o = await _otpRepo.genererOtp(data_client.Id, data_client.Id.ToString(), type_otp.RESET_CODE_PIN, _paramdata.sms.validite_otp ?? 6);

                await _serviceMessagerie.sendMessageAuClient(type_modele.INITIALISATION_CODE_PIN, data_client.telephone, data_client, o);


                return Ok("OTP envoyé avec succès pour la réinitialisation du code PIN.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("code-pin/reset/otp/confirm")]
        public async Task<IActionResult> ConfirmReinistialisationDuCodePin([FromBody] QueryConfirmationOtpDto _body)
        {
            string _desc_route = "Réinistialisation du code PIN";
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

                /// Recherche du client dans la base
                Model.t_client data_client = GetInfoClient();

                if (data_client == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'utilisateur est introuvable dans le sytème", instance: HttpContext.Request.Path));


                /// Vérifie si l'otp est lié
                int res_otp = await _otpRepo.verifieOtp(data_client.Id, _body.otp, type_otp.RESET_CODE_PIN, data_client.Id.ToString());
                switch (res_otp)
                {
                    case 0:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Expiré", instance: HttpContext.Request.Path));
                    case -1:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));
                    case 1:
                        break;
                };

                ///// Réinistialiser du code PIN
                GeneraleRetour e = await _serviceSecurity.RéinistialiserCodePin(RecupererToken(), IdDemande);

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));


                return Ok("Réinitialisation du code PIN effectuée avec succès.");
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPost("code-pin/reset")]
        public async Task<IActionResult> ReinistialisationDuCodePinByApplication([FromBody] CodePinResetClientBodyDto _body)
        {
            string _desc_route = "Réinistialisation du code PIN";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {
                int taille_code_pin = _securityconfig.length_pin > 0 ? _securityconfig.length_pin : 4;

                string token = RecupererToken();

                var validator = new CodePinResetClientBodyDtoValidator(taille_code_pin);
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

                /// Recherche du client dans la base
                Model.t_client data_client = GetInfoClient();

                if (data_client == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'utilisateur est introuvable dans le sytème", instance: HttpContext.Request.Path));


                ///// Réinistialiser du code PIN
                GeneraleRetour e = await _serviceSecurity.RéinistialiserCodePin(token, IdDemande);

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

                CodePinClientBodyDto _body_pin = new CodePinClientBodyDto
                {
                    pin = _body.pin
                };
                
                GeneraleRetour data_res = await _serviceSecurity.DefinirCodePIN(_body_pin, token, IdDemande);

                if (Tools.Tools.RetourIsSucces(data_res.status))
                    return Ok("Enregistrement du code PIN effectué avec succès");
                else
                    return StatusCode(data_res.status, GeneraleRetour.BuildProblemResponse(data_res, instance: HttpContext.Request.Path));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }

        }


        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ModifierLeMotPasse([FromBody] UpdatePasswordClientDto _body)
        {
            string _desc_route = "Modification du code PIN";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                Model.t_client data_client = GetInfoClient();


                var validator = new UpdatePasswordClientDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    var problems = GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams
                    );
                    return BadRequest(problems);
                }


                GeneraleRetour e = await _serviceSecurity.ModificationMotPasse(data_client, _body, RecupererToken(), IdDemande); ;

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

                return Ok("Modification du mot de passe effectuée avec succès");


            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [HttpPost("password/reset/otp")]
        public async Task<IActionResult> ReinistialisationMotDePasse([FromBody] InitPasswordClientDto _body)
        {
            string _desc_route = "Rénistialisation du mot de passe";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                Model.t_client data_client = await _interopContext.t_client
                    .Where(p => p.security_username == _body.identifiant && p.is_delete != true)
                    .FirstOrDefaultAsync();

                if (data_client == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'utilisateur est introuvable dans le sytème", instance: HttpContext.Request.Path));

                t_otp o = await _otpRepo.genererOtp(data_client.Id, data_client.Id.ToString(), type_otp.RESET_PASSWORD, _paramdata.sms.validite_otp ?? 6);

               await _serviceMessagerie.sendMessageAuClient(type_modele.MOT_PASSE_OUBLIE, data_client.telephone, data_client, o);

                return Ok(new { message = "OTP envoyé avec succès pour la réinitialisation du mot de passe.", challengeId = o.challengeId, contactMasked = Tools.Tools.MaskPhone(data_client.telephone), emailMasked = Tools.Tools.MaskEmail(data_client.email) }
);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [HttpPost("password/reset/otp/confirm")]
        public async Task<IActionResult> ConfirmReinistialisationMotDePasse([FromBody] QueryConfirmationOtpResetPwdDto _body)
        {
            string _desc_route = "Confirmation du réinistialisation du mot de passe";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new QueryConfirmationOtpResetPwdDtoValidator();
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

                /// Vérifie si l'otp est lié

                (int res_otp, t_otp o) = await _otpRepo.verifieOtpAndChallenge(_body.otp, type_otp.RESET_PASSWORD, _body.challenge);

                switch (res_otp)
                {
                    case 0:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Expiré", instance: HttpContext.Request.Path));
                    case -1:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));
                    case 1:
                        break;
                };

                Model.t_client data_client = await _interopContext.t_client.Where(p => p.Id.ToString() == o.idOperationParent && p.is_delete != true).FirstOrDefaultAsync();


                if (data_client == null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));


                ///// Réinistialiser du mot de passe
                GeneraleRetour e = await _serviceSecurity.ModificationMotPasseParInitialisation(data_client, _body.new_password, IdDemande);

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));


                return Ok("Mot de passe modifié avec succès.");
            }

            catch (Exception ex)
            {
                _logger.LogInformation("Step 5: ------------Exception ex");
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [HttpGet("comptes/{codeclient}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetListeDesComptes(string codeclient)
        {
            string _desc_route = "Liste des comptes client";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrEmpty(codeclient))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "La racine du client est requise", instance: HttpContext.Request.Path));

                // Recuperer les rubriques comptables autorisées pour PI
                List<ItemData> rub_comptables_autorisees = await _datarepo.getDataInListByCode<ItemData>(code_datas.RUBRIQUE_COMPTABLE_AUTORISEE.ToString());

                var result = await _ServiceAIF.GetClientListeCompte(codeclient, IdDemande);

                if (!result.operationStatus)
                {
                    return StatusCode(result.status,
                       GeneraleRetour.BuildProblemResponse(new GeneraleRetour { status = result.status, detail = result.erreur }, instance: HttpContext.Request.Path));
                }

                var res_data = JsonConvert.DeserializeObject<MessageCompteListe>(result.data);
                var filteredData = res_data.compte.Where(r => rub_comptables_autorisees
                                           .Any(a => a.Code == r.rubriqueComptable))
                              .ToList();

                return Ok(filteredData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
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
        [HttpPost("comptes/{id}/photos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(string id, [FromForm] IFormFile? file)
        {
            const string _desc_route = "Modifier la photo";

            try
            {


                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest("Le numéro de compte est requis", HttpContext.Request.Path));

                if (file == null || file.Length == 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest("Aucune photo n'a été sélectionnée.", HttpContext.Request.Path));

                // Configuration des tailles et extensions autorisées
                string[] allowedExtensions = _configuration.GetSection("ImageSettings:AllowedExtensions").Get<string[]>() ?? new[] { ".jpg", ".jpeg", ".png" };
                long maxLengthMo = long.TryParse(_configuration["ImageSettings:MaxLength"], out var result) ? result : 2;
                long maxLengthBytes = maxLengthMo * 1024 * 1024;

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(GeneraleRetour.BuildBadRequest($"Les extensions valables sont {string.Join(", ", allowedExtensions)}", HttpContext.Request.Path));

                if (file.Length > maxLengthBytes)
                    return BadRequest(GeneraleRetour.BuildBadRequest($"L'image dépasse la taille maximale autorisée de {maxLengthMo} Mo.", HttpContext.Request.Path));

                // Recherche des infos client
                var client = GetInfoClient();
                var compte = await _compteRepo.SearchCompteByIbanOrOther(id, client.Id);
                if (compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound("Le compte est inconnu", HttpContext.Request.Path));

                var alias = await _aliasRepo.SearchAliasByIban(compte.ibanOrOther);
                if (alias == null)
                    return NotFound(GeneraleRetour.BuildNotFound("Le compte n'a pas d'alias dans le système", HttpContext.Request.Path));

                // Création du dossier si besoin
                string photoFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photo");
                if (!Directory.Exists(photoFolder))
                    Directory.CreateDirectory(photoFolder);

                // Enregistrement du fichier
                string fileName = alias.valeurAlias + extension;
                string filePath = Path.Combine(photoFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string publicUrl = GetPublicUrl("photo", fileName);

                // Mise à jour de l'alias sur PI
                var aliasDto = new QueryModificationAliasClientDto
                {
                    alias = alias.valeurAlias,
                    photoClient = publicUrl
                };

                var retour = await _serviceAlias.UpdateAlias(aliasDto);
                if (!Tools.Tools.RetourIsSucces(retour.status))
                    return StatusCode(retour.status, GeneraleRetour.BuildProblemResponse(retour, HttpContext.Request.Path));

                return Ok(new { url = publicUrl });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] Accès refusé : {ex.Message}");
                return StatusCode(403, GeneraleRetour.BuildForbid("Permission refusée : " + ex.Message, HttpContext.Request.Path));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] Erreur interne : {ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPost("comptes/{id}/photos/64")]
        public async Task<IActionResult> UploadPhotoByBase64(string id, [FromBody] QueryUpdatePhoto _body)
        {

            string _desc_route = "Modifier la photo";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                string _photoFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photo");

                if (!(long.TryParse(_configuration["ImageSettings:MaxLength"], out long tailleMaximaleEnmo)))
                    tailleMaximaleEnmo = 2; // par defaut 2 Mo

                long tailleMaximale = tailleMaximaleEnmo * 1024 * 1024;

                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de compte est requis", instance: HttpContext.Request.Path));

                if (_body == null || string.IsNullOrEmpty(_body.FileBase64) || string.IsNullOrEmpty(_body.FileName))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données de l'image sont manquantes.", instance: HttpContext.Request.Path));

                string[] allowedExtensions = _configuration.GetSection("ImageSettings:AllowedExtensions").Get<string[]>();

                // Vérification du type de fichier
                var extension = Path.GetExtension(_body.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: $"Les extensions valables sont {string.Join(",", allowedExtensions)}", instance: HttpContext.Request.Path));

                // Décoder la chaîne Base64
                byte[] imageBytes = Convert.FromBase64String(_body.FileBase64);

                if (tailleMaximale > 0)
                    if (imageBytes.Length > tailleMaximale)
                        return BadRequest(GeneraleRetour.BuildBadRequest(detail: $"L'image dépasse la taille maximale autorisée de {tailleMaximaleEnmo} Mo.", instance: HttpContext.Request.Path));


                Model.t_client resp_client = GetInfoClient();

                t_compte _rech_compte = await _compteRepo.SearchCompteByIbanOrOther(id, resp_client.Id);
                if (_rech_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                t_alias _rech_alias = await _aliasRepo.SearchAliasByIban(_rech_compte.ibanOrOther);
                if (_rech_alias == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte n'as pas d'alias dans le système", instance: HttpContext.Request.Path));



                // Définir le chemin où l'image sera stockée
                if (!Directory.Exists(_photoFolder))
                {
                    Directory.CreateDirectory(_photoFolder);
                }


                // Générer un nom de fichier unique et enregistrer l'image
                var fileName = _rech_alias.valeurAlias + extension;

                // Créer le chemin complet du fichier avec un nom unique
                var filePath = Path.Combine(_photoFolder, fileName);

                // Enregistrer le fichier sur le serveur
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);


                var publicUrl = GetPublicUrl("photo", fileName);


                // Modification sur Pi

                QueryModificationAliasClientDto aliasUpd = new QueryModificationAliasClientDto
                {

                    photoClient = publicUrl,
                    alias = _rech_alias.valeurAlias
                };

                GeneraleRetour e = await _serviceAlias.UpdateAlias(aliasUpd);

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return StatusCode(e.status, GeneraleRetour.BuildProblemResponse(e, instance: HttpContext.Request.Path));

                return Ok(new { url = publicUrl });

            }

            catch (FormatException)
            {
                return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le format Base64 de l'image est invalide.", instance: HttpContext.Request.Path));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Permission refusée : " + ex.Message, instance: HttpContext.Request.Path));
            }
            catch (Exception ex)
            {
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
        [HttpPost("alias/revendications")]
        public async Task<IActionResult> RenvendiquerUnAlias([FromBody] QueryRevendiquerUnAlias _body)
        {

            string _desc_route = "Revendiquer un alias";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                var validator = new QueryRevendiquerUnAliasValidator();
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



                GeneraleRetour r = await _serviceAlias.RevendiquerUnAlias(_body.alias, _body.compte, IdDemande);

                if (Tools.Tools.RetourIsSucces(r.status))
                {
                    var t_rev = JsonConvert.DeserializeObject<t_revendication>(r.data);

                    RevendicationDto rev = new RevendicationDto
                    {
                        id = t_rev.Id.ToString(),
                        alias = t_rev.alias,
                        dateVerrouillage = t_rev.dateVerrouillage,
                        dateDemande = t_rev.dateDemande,
                        dateCloture = t_rev.dateCloture,
                        statut = t_rev.statut.ToString(),
                    };

                    return StatusCode(200, rev);
                }
                else
                {
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, HttpContext.Request.Path));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpGet("alias/revendications/{id}")]
        public async Task<IActionResult> RecupererUneRevendication(string id)
        {

            string _desc_route = "Récupérer une revendication";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la revendication est requis", instance: HttpContext.Request.Path));


                t_revendication _rech_rev = await _revendicationRepo.SearchRevendicationById(id);

                if (_rech_rev == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La revendication est introuvable dans le système",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }


                RevendicationDto rev = new RevendicationDto
                {
                    id = _rech_rev.Id.ToString(),
                    alias = _rech_rev.alias,
                    dateVerrouillage = _rech_rev.dateVerrouillage,
                    dateDemande = _rech_rev.dateDemande,
                    dateCloture = _rech_rev.dateCloture,
                    statut = _rech_rev.statut.ToString(),
                };


                if (_rech_rev.statut == statut_revendication.ACCEPTEE)
                {
                    rev.shid = new shidDto
                    {
                        cle = _rech_rev.alias,
                        type = "MBNO",
                    };
                }

                return StatusCode(200, rev);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("alias/revendications/{id}/reponses")]
        public async Task<IActionResult> RepondreUneRevendication(string id, [FromBody] RevendicationReponseDto _body)
        {

            string _desc_route = "Repondre à une revendication";

            try
            {

                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la revendication est requis", instance: HttpContext.Request.Path));


                var validator = new RevendicationReponseDtoValidator();
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
                }

                Model.t_client data_client = GetInfoClient();

                t_revendication _rech_rev = await _revendicationRepo.SearchRevendicationByIdAndSens(id, sensFlux.ENTRANT);

                if (_rech_rev == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La revendication est introuvable dans le système",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }

                GeneraleRetour r = await _serviceAlias.RepondreAUneRevendication(data_client.Id, _rech_rev.idRevendicationPi, (bool)_body.decision, data_client);

                if (!Tools.Tools.RetourIsSucces(r.status))
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, instance: HttpContext.Request.Path));


                var t_rev = JsonConvert.DeserializeObject<t_revendication>(r.data);

                RevendicationDto rev = new RevendicationDto
                {
                    id = t_rev.Id.ToString(),
                    alias = t_rev.alias,
                    dateVerrouillage = t_rev.dateVerrouillage,
                    dateAction = t_rev.dateAction,
                    dateDemande = t_rev.dateDemande,
                    dateCloture = t_rev.dateCloture,
                    statut = t_rev.statut.ToString()
                };
                return StatusCode(200, rev);



            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("alias/revendications/{id}/rejets")]
        public async Task<IActionResult> RejeterUneRevendication(string id, [FromBody] QueryConfirmationOtpDto _body)
        {

            string _desc_route = "Rejeter une revendication";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la revendication est requis", instance: HttpContext.Request.Path));

                if (string.IsNullOrEmpty(_body.otp))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'otp de confirmation est requis", instance: HttpContext.Request.Path));


                Model.t_client resp_client = GetInfoClient();

                t_revendication _rech_rev = await _revendicationRepo.SearchRevendicationByIdAndSens(id, sensFlux.ENTRANT);

                if (_rech_rev == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La revendication est introuvable dans le système",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }


                GeneraleRetour r = await _serviceAlias.ConfirmerLeRejetDuneRevendication(resp_client.Id, _rech_rev.idRevendicationPi, _body, IdDemande);

                if (!Tools.Tools.RetourIsSucces(r.status))
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, instance: HttpContext.Request.Path));


                var t_rev = JsonConvert.DeserializeObject<t_revendication>(r.data);

                RevendicationDto rev = new RevendicationDto
                {
                    id = t_rev.Id.ToString(),
                    alias = t_rev.alias,
                    dateVerrouillage = t_rev.dateVerrouillage,
                    dateAction = t_rev.dateAction,
                    dateDemande = t_rev.dateDemande,
                    dateCloture = t_rev.dateCloture,
                    statut = t_rev.statut.ToString(),
                };
                return StatusCode(200, rev);
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
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
        [HttpPost("contacts")]
        public async Task<IActionResult> CreerUnContact([FromBody] ContactDto _body)
        {

            string _desc_route = "Créer un contact";

            try
            {

                Model.t_client data_client = GetInfoClient();

                List<InvalidParam> invalidParams = new List<InvalidParam>();

                if (string.IsNullOrEmpty(_body.nom))
                    invalidParams.Add(new InvalidParam { name = "nom", reason = "Le nom du nouveau contact est manquant" });

                if (string.IsNullOrEmpty(_body.alias))
                    invalidParams.Add(new InvalidParam { name = "alias", reason = "L'alias du nouveau contact est manquant" });


                if (invalidParams.Count > 0)
                {
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));
                }

                var resp_query = await _interopContext.t_contact_client
                    .Where(p => p.alias == _body.alias && p.r_client_id_fk == data_client.Id && p.is_delete != true)
                    .FirstOrDefaultAsync();

                /// Recherche de l'alias dans la base
                if (resp_query != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "L'alias existe déjà dans la liste des contacts", instance: HttpContext.Request.Path));


                t_contact_client contact = new t_contact_client
                {
                    nom = _body.nom,
                    confiance = _body.confiance,
                    alias = _body.alias,
                    r_client_id_fk = data_client.Id
                };


                _interopContext.t_contact_client.Add(contact);
                _interopContext.SaveChanges();

                ContactDto contactsDto = new ContactDto
                {
                    id = contact.Id,
                    nom = contact.nom,
                    alias = contact.alias,
                    confiance = contact.confiance
                };

                return Ok(contactsDto);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpDelete("contacts/{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {

            string _desc_route = "Supprimer un contact";

            try
            {

                Model.t_client resp_query_client = GetInfoClient();

                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du contact à supprimer est manquant", instance: HttpContext.Request.Path));

                /// Recherche du contact dans la liste des contacts du client dans la base
                var resp_query = await _interopContext.t_contact_client
                    .Where(p => p.Id == id && p.r_client_id_fk == resp_query_client.Id && p.is_delete != true)
                    .FirstOrDefaultAsync();

                if (resp_query == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le contact n'existe pas dans la liste des contacts", instance: HttpContext.Request.Path));


                resp_query.is_delete = true;

                _interopContext.t_contact_client.Update(resp_query);
                _interopContext.SaveChanges();

                return StatusCode(204, "Contact supprimé avec succès");

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpPut("contacts/{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] ContactDto _body)
        {

            string _desc_route = "Modifier un contact";

            try
            {
                Model.t_client data_client = GetInfoClient();


                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du contact à supprimer est manquant", instance: HttpContext.Request.Path));

                /// Recherche du contact dans la liste des contacts du client dans la base
                var resp_query = await _interopContext.t_contact_client
                    .Where(p => p.Id == id && p.r_client_id_fk == data_client.Id && p.is_delete != true)
                    .FirstOrDefaultAsync();

                if (resp_query == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le contact n'existe pas dans la liste des contacts", instance: HttpContext.Request.Path));

                var resp_query_exist = await _interopContext.t_contact_client
                  .Where(p => p.alias == _body.alias && p.r_client_id_fk == data_client.Id && p.is_delete != true && p.Id != id)
                  .FirstOrDefaultAsync();

                /// Recherche de l'alias sur un autre contact dans la base
                if (resp_query_exist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "L'alias existe déjà dans la liste des contacts", instance: HttpContext.Request.Path));


                if (!string.IsNullOrEmpty(_body.nom)) resp_query.nom = _body.nom;
                if (!string.IsNullOrEmpty(_body.alias)) resp_query.alias = _body.alias;
                if (_body.confiance != null) resp_query.confiance = _body.confiance;

                _interopContext.t_contact_client.Update(resp_query);
                _interopContext.SaveChanges();

                ContactDto contactsDto = new ContactDto
                {
                    id = resp_query.Id,
                    alias = resp_query.alias,
                    nom = resp_query.nom,
                    confiance = resp_query.confiance
                };

                return StatusCode(200, contactsDto);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {

            string _desc_route = "Liste des contacts";

            try
            {
                Model.t_client resp_query_client = GetInfoClient();

                /// Recherche des contacts dans la liste des contacts du client dans la base
                var resp_query_contact = await _interopContext.t_contact_client
                    .Where(p => p.r_client_id_fk == resp_query_client.Id && p.is_delete != true)
                    .OrderBy(p => p.nom)
                    .ToListAsync();


                // Mapping des résultats à des objets ContactDto
                var contactsDto = resp_query_contact.Select(c => new ContactDto
                {
                    id = c.Id,
                    nom = c.nom,
                    alias = c.alias,
                    confiance = c.confiance
                }).ToList();

                return Ok(new { data = contactsDto, meta = new MetaDto { total = contactsDto.Count() } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");

                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpGet("getannexe/{id}")]
        [HttpGet("annexes/{id}")]
        public async Task<IActionResult> GetAnnexes(string id)
        {
            string _desc_route = "Liste des annexes";

            try
            {
                var ret = await _interopContext.t_data.Where(p => p.code == id).Select(o => o.data).FirstOrDefaultAsync();
                return Ok(new { data = ret, meta = new MetaDto { } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }

        }

        [Authorize]
        [HttpGet("annexes/rubriques")]
        public async Task<IActionResult> ListeDesRubriquesAnnexes()
        {
            string _desc_route = "Liste des rubriques";

            try
            {

                var result = await _interopContext.t_data
                    .Select(o => new { o.code, o.description })
                    .ToListAsync();

                return Ok(new { data = result, meta = new MetaDto { total = result.Count() } });
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
        [HttpGet("favoris/{numerocompte}/{type}/{size?}")]
        public async Task<IActionResult> GetFavoris(string numerocompte, Type_alias type, int size = 10)
        {
            string _desc_route = "Liste des favoris";

            try
            {

                Model.t_client data_client = GetInfoClient();

                if (string.IsNullOrEmpty(numerocompte))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de compte est requis", instance: HttpContext.Request.Path));

                t_compte _rech_compte = await _compteRepo.SearchCompteByIbanOrOther(numerocompte, data_client.Id);
                if (_rech_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));

                var payeDatas = _interopContext.t_transfert
        .Where(a => a.is_delete == false && a.compteClientPayeur == _rech_compte.ibanOrOther)
        .GroupBy(a => new
        {
            typeAlias = Tools.Tools.DeterminerTypeAlias(a.aliasClientPaye),
            nom = a.nomClientPaye,
            iban = a.ibanClientPaye,
            other = a.otherClientPaye,
            alias = a.aliasClientPaye,
            typeClient = a.typeClientPaye,
            dateNaissance = a.dateNaissanceClientPaye,
        })
        .Select(g => new
        {
            g.Key.typeAlias,
            g.Key.typeClient,
            g.Key.nom,
            g.Key.other,
            g.Key.dateNaissance,
            g.Key.iban,
            g.Key.alias,
            NombreDeTransferts = g.Count()
        });

                /// Notre alias est le paye ==> Favoris Envoyeur
                var payeurDatas = _interopContext.t_transfert
                    .Where(a => a.is_delete == false && a.compteClientPaye == _rech_compte.ibanOrOther)
                    .GroupBy(a => new
                    {
                        nom = a.nomClientPayeur,
                        typeAlias = Tools.Tools.DeterminerTypeAlias(a.aliasClientPayeur),
                        iban = a.ibanClientPayeur,
                        other = a.otherClientPayeur,
                        alias = a.aliasClientPayeur,
                        typeClient = a.typeClientPayeur,
                        dateNaissance = a.dateNaissanceClientPayeur,
                    })
                    .Select(g => new
                    {
                        g.Key.typeAlias,
                        g.Key.typeClient,
                        g.Key.nom,
                        g.Key.other,
                        g.Key.dateNaissance,
                        g.Key.iban,
                        g.Key.alias,
                        NombreDeTransferts = g.Count()
                    });

                // Union des deux requetes
                var payeAndPayeurDatas = payeDatas
                    .Union(payeurDatas)
                    .Where(x =>
        (type == Type_alias.SHID && (x.typeAlias == Type_alias.SHID.ToString())) || // Condition 1 : type est alias SHID
        (type == Type_alias.MBNO && (x.typeAlias == Type_alias.MBNO.ToString())) || // Condition 2 : type alias MBNO
        (type == Type_alias.MCOD && (x.typeAlias == Type_alias.MCOD.ToString())) ||  // Condition 3 : type alias MCOD
        (type == Type_alias.VIDE)    /////
        )
                     .OrderByDescending(x => x.NombreDeTransferts)
                    .Take(size); // Uniquement le nombre de ligne demandé


                return Ok(new
                {
                    description = _desc_route,
                    message = "Liste des favoris renvoyé avec succès",
                    data = payeAndPayeurDatas
                });

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);

                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpGet("alias/favoris/{alias}/{type}/{size?}")]
        public async Task<IActionResult> GetAliasFavoris(string alias, Type_alias type, int size = 10)
        {
            string _desc_route = "Liste des favoris";

            try
            {

                if (string.IsNullOrEmpty(alias))
                    return BadRequest(new { description = _desc_route, message = "Veuillez renseigner l'alias" });

                t_alias _rech_alias = await _aliasRepo.SearchAliasByAlias(alias);

                if (_rech_alias == null || _rech_alias.r_isactive == false)
                    return NotFound(new { description = _desc_route, message = "l'alias n'existe pas dans le système" });

                /// Notre alias est le payeur ==> Favoris Beneficiaire
                var payeDatas = _interopContext.t_transfert
        .Where(a => a.is_delete == false && a.aliasClientPayeur == alias)
        .GroupBy(a => new
        {
            typeAlias = Tools.Tools.DeterminerTypeAlias(a.aliasClientPaye),
            nom = a.nomClientPaye,
            iban = a.ibanClientPaye,
            other = a.otherClientPaye,
            alias = a.aliasClientPaye,
            typeClient = a.typeClientPaye,
            dateNaissance = a.dateNaissanceClientPaye,
        })
        .Select(g => new
        {
            g.Key.typeAlias,
            g.Key.typeClient,
            g.Key.nom,
            g.Key.other,
            g.Key.dateNaissance,
            g.Key.iban,
            g.Key.alias,
            NombreDeTransferts = g.Count()
        });
                /// Notre alias est le paye ==> Favoris Envoyeur
                var payeurDatas = _interopContext.t_transfert
                    .Where(a => a.is_delete == false && a.aliasClientPaye == alias)
                    .GroupBy(a => new
                    {
                        nom = a.nomClientPayeur,
                        typeAlias = Tools.Tools.DeterminerTypeAlias(a.aliasClientPayeur),
                        iban = a.ibanClientPayeur,
                        other = a.otherClientPayeur,
                        alias = a.aliasClientPayeur,
                        typeClient = a.typeClientPayeur,
                        dateNaissance = a.dateNaissanceClientPayeur,
                    })
                    .Select(g => new
                    {
                        g.Key.typeAlias,
                        g.Key.typeClient,
                        g.Key.nom,
                        g.Key.other,
                        g.Key.dateNaissance,
                        g.Key.iban,
                        g.Key.alias,
                        NombreDeTransferts = g.Count()
                    });

                // Union des deux requetes
                var payeAndPayeurDatas = payeDatas
                    .Union(payeurDatas)
                    .Where(x =>
        (type == Type_alias.SHID && (x.typeAlias == Type_alias.SHID.ToString())) || // Condition 1 : type est alias SHID
        (type == Type_alias.MBNO && (x.typeAlias == Type_alias.MBNO.ToString())) || // Condition 2 : type alias MBNO
        (type == Type_alias.MCOD && (x.typeAlias == Type_alias.MCOD.ToString())) ||  // Condition 3 : type alias MCOD
        (type == Type_alias.VIDE)    /////
        )
                     .OrderByDescending(x => x.NombreDeTransferts)
                    .Take(size); // Uniquement le nombre de ligne demandé


                return Ok(new
                {
                    description = _desc_route,
                    message = "Liste des favoris renvoyé avec succès",
                    data = payeAndPayeurDatas
                });

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");

                var problem = GeneraleRetour.BuildProblemResponse500(
                   instance: HttpContext.Request.Path
               );

                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {

            string _desc_route = "Liste des catégories";

            try
            {

                Model.t_client resp_client = GetInfoClient();

                List<t_categorie> data_categorie = await _interopContext.t_categories
                          .Where(c => (c.niveau == niveau_categorie.BACK_OFFICE || (c.niveau == niveau_categorie.CLIENT && c.r_client_id_fk == resp_client.Id)
                          ) && c.is_delete != true)
                         .OrderBy(c => c.nom).ToListAsync();


                // Mapping des résultats à des objets CategorieDto
                var categories = data_categorie.Select(c => new CategorieDto
                {
                    Id = c.Id,
                    nom = c.nom,
                    icon = c.icon,
                    niveau = c.niveau
                }).ToList();

                return Ok(new { data = categories, meta = new MetaDto { total = categories.Count() } });

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }


        }

        [Authorize]
        [HttpPost("categories")]
        public async Task<IActionResult> CreerUneCategorie([FromBody] QueryCategorieDto _body)
        {

            string _desc_route = "Créer une catégorie";
            _logger.LogInformation($"[EndPoint {_desc_route}] ===============================> MOBILE CONTROLLER");

            try
            {
                Model.t_client resp_client = GetInfoClient();


                if (string.IsNullOrEmpty(_body.nom))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le nom de la catégorie est requis", instance: HttpContext.Request.Path));


                /// Recherche SI le nom de la catégorie existe deja
                var categorie_exist = await _interopContext.t_categories
                        .Where(c => (c.niveau == niveau_categorie.BACK_OFFICE || (c.niveau == niveau_categorie.CLIENT && c.r_client_id_fk == resp_client.Id)
                        ) && c.is_delete != true && c.nom == _body.nom)
                       .OrderBy(c => c.nom).FirstOrDefaultAsync();

                if (categorie_exist != null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "La catégorie existe déjà dans la liste des catégories du client", instance: HttpContext.Request.Path));

                t_categorie c = new t_categorie
                {
                    nom = _body.nom,
                    icon = _body.icon,
                    niveau = niveau_categorie.CLIENT,
                    r_client_id_fk = resp_client.Id
                };

                _interopContext.t_categories.Add(c);
                _interopContext.SaveChanges();

                // Mapping des résultats à l'objet CategorieDto
                CategorieDto categorie = new CategorieDto
                {
                    Id = c.Id,
                    nom = c.nom,
                    icon = c.icon,
                    niveau = c.niveau
                };

                return Ok(categorie);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}]  ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategorie(int id)
        {

            string _desc_route = "Supprimer une catégorie";
            _logger.LogInformation($"[EndPoint {_desc_route}] ===============================> MOBILE CONTROLLER");

            try
            {
                Model.t_client resp_client = GetInfoClient();


                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la catégorie est requis", instance: HttpContext.Request.Path));


                /// Recherche de la catégorie dans la liste des catégorie du client dans la base
                var resp_categorie = await _interopContext.t_categories
                    .Where(p => p.Id == id && p.is_delete != true)
                    .FirstOrDefaultAsync();

                if (resp_categorie == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La catégorie n'existe pas dans la liste des catégories du client", instance: HttpContext.Request.Path));

                if (resp_categorie.niveau == niveau_categorie.BACK_OFFICE)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Vous ne pouvez pas supprimer cette catégorie", instance: HttpContext.Request.Path));

                if (resp_categorie.r_client_id_fk != resp_client.Id)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La catégorie n'existe pas dans la liste des catégories du client", instance: HttpContext.Request.Path));

                resp_categorie.is_delete = true;
                _interopContext.t_categories.Update(resp_categorie);
                _interopContext.SaveChanges();

                return StatusCode(204, "Catégorie supprimée avec succès");

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategorie(int id, [FromBody] CategorieDto _body)
        {

            string _desc_route = "Modifier une catégorie";

            try
            {

                Model.t_client resp_client = GetInfoClient();

                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la catégorie est requis", instance: HttpContext.Request.Path));

                /// Recherche de la catégorie dans la liste des categorie du client dans la base
                /// 
                var resp_categorie = await _interopContext.t_categories
                   .Where(p => p.Id == id && p.is_delete != true)
                   .FirstOrDefaultAsync();

                if (resp_categorie == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La catégorie n'existe pas dans la liste des catégories du client", instance: HttpContext.Request.Path));

                if (resp_categorie.niveau == niveau_categorie.BACK_OFFICE)
                    return StatusCode(403, GeneraleRetour.BuildNotFound(detail: "Vous ne pouvez pas modifier cette catégorie", instance: HttpContext.Request.Path));

                if (resp_categorie.r_client_id_fk != resp_categorie.Id)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La catégorie n'existe pas dans la liste des catégories du client", instance: HttpContext.Request.Path));

                /// Recherche SI le nom de la catégorie existe deja
                var categorie_exist = await _interopContext.t_categories
                        .Where(c => (c.niveau == niveau_categorie.BACK_OFFICE || (c.niveau == niveau_categorie.CLIENT && c.r_client_id_fk == resp_client.Id)
                        ) && c.is_delete != true && c.nom == _body.nom)
                       .OrderBy(c => c.nom).FirstOrDefaultAsync();

                if (categorie_exist != null)
                    return StatusCode(403, GeneraleRetour.BuildNotFound(detail: "La catégorie existe déjà dans la liste des catégories du client", instance: HttpContext.Request.Path));


                if (!string.IsNullOrEmpty(_body.icon)) resp_categorie.icon = _body.icon;
                if (!string.IsNullOrEmpty(_body.nom)) resp_categorie.nom = _body.nom;

                _interopContext.t_categories.Update(resp_categorie);
                _interopContext.SaveChanges();

                // Mapping des résultats à l'objet CategorieDto
                CategorieDto categorie = new CategorieDto
                {
                    Id = resp_categorie.Id,
                    nom = resp_categorie.nom,
                    icon = resp_categorie.icon,
                    niveau = resp_categorie.niveau
                };

                return StatusCode(200, categorie);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpGet("transferts")]
        public async Task<IActionResult> GetTransferts([FromQuery] string compte, [FromQuery] int? page, [FromQuery] int? limit, [FromQuery] string? sort_by, [FromQuery] string? sens, [FromQuery] string? clientPaye, [FromQuery] string? clientPayeur, [FromQuery] string? categorie, DateTime? dateOperation, DateTime? dateOperationgte, DateTime? dateOperationlte, DateTime? dateOperationgt, DateTime? dateOperationlt)
        {


            string _desc_route = "Liste des transferts";

            try
            {


                if (string.IsNullOrEmpty(compte))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de compte est requis", instance: HttpContext.Request.Path));

                Model.t_client data_client = GetInfoClient();
                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(compte, data_client.Id);

                if (data_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                var query = _interopContext.t_transfert.AsQueryable();
                query = query.Where(t => (t.compteClientPaye == data_compte.ibanOrOther || t.compteClientPayeur == data_compte.ibanOrOther) && t.is_delete != true && (t.statut_general == STATUT_TRANSFERT.irrevocable || t.statut_general == STATUT_TRANSFERT.desactive));

                // Filtrer par numéro de compte (payeur ou payé)
                if (!string.IsNullOrEmpty(clientPaye))
                {
                    query = query.Where(t => t.aliasClientPaye == clientPaye || t.ibanClientPaye == clientPaye);
                }

                if (!string.IsNullOrEmpty(clientPayeur))
                {
                    query = query.Where(t => t.aliasClientPayeur == clientPayeur || t.ibanClientPayeur == clientPayeur);
                }

                // Filtrer par catégorie
                if (!string.IsNullOrEmpty(categorie))
                {
                    query = query.Where(t => t.r_categorie_payeur_id_fk.ToString() == categorie);
                }

                // Filtrer par sens de la transaction
                if (!string.IsNullOrEmpty(sens))
                {

                    if (sens == "credit")
                    {
                        query = query.Where(t => (t.compteClientPaye == data_compte.ibanOrOther)); // Ex. Crédit
                    }
                    else if (sens == "debit")
                    {
                        query = query.Where(t => (t.compteClientPayeur == data_compte.ibanOrOther)); // Ex. Débit
                    }
                }


                if (dateOperationgte.HasValue)
                    query = query.Where(t => t.dateHeureIrrevocabilite >= dateOperationgte.Value);


                if (dateOperationlte.HasValue)
                    query = query.Where(t => t.dateHeureIrrevocabilite <= dateOperationlte.Value);

                if (dateOperationgt.HasValue)
                    query = query.Where(t => t.dateHeureIrrevocabilite > dateOperationgt.Value);

                if (dateOperationlt.HasValue)
                    query = query.Where(t => t.dateHeureIrrevocabilite < dateOperationlt.Value);

                if (dateOperation.HasValue)
                    query = query.Where(t => t.dateHeureIrrevocabilite == dateOperation.Value);



                /// Verification des filtres

                if (!string.IsNullOrEmpty(sort_by))
                {

                    // Tri
                    if (sort_by == "-dateOperation")
                    {
                        query = query.OrderByDescending(t => t.dateHeureIrrevocabilite);
                    }
                    else if (sort_by == "dateOperation")
                    {
                        query = query.OrderBy(t => t.dateHeureIrrevocabilite);
                    }
                    else
                    {
                        query = query.OrderByDescending(t => t.dateHeureIrrevocabilite);
                    }
                }
                else
                {
                    query = query.OrderByDescending(t => t.dateHeureIrrevocabilite);
                }


                int total = await query.CountAsync(); // Compte total pour toutes les pages

                int pages = 1;
                if (page != null)
                    pages = (int)page;

                int limits = 25;
                if (limit != null)
                    limits = (int)limit;

                if (pages <= 0)
                    pages = 1;

                if (limits <= 0)
                    limits = 10;

                // Pagination
                if (limit > 0)
                {
                    query = query.Skip((pages - 1) * limits).Take(limits);
                }

                // Charger la liste des pays
                Dictionary<string, string> countryDictionary = await _datarepo.getDataInDictionaryByCode(code_datas.PAYS.ToString());
                List<t_participant> Participants = await _participantrepo.getAll();

                var data = await query.Select(t => _serviceTransfert.ConvertirTransfertEnTransfertDto(t, data_compte.ibanOrOther, countryDictionary, Participants)).ToListAsync();

                string? previousPage = pages > 1 ? (pages - 1).ToString() : null;
                string? nextPage = pages * limits < total ? (pages + 1).ToString() : null;

                // Création de l'objet meta
                var meta = new MetaDto
                {
                    total = total,
                    previous = previousPage,
                    next = nextPage,
                    current = pages.ToString(),
                    limit = limits
                };

                return Ok(
                    new
                    {
                        data = data,
                        meta = meta,
                    }
               );
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
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
        [HttpPost("transferts/ping")]
        public async Task<IActionResult> CreerUnTransfertDeDisponibilite([FromBody] QueryBodyTransactionDispoDto _body)
        {

            string _desc_route = "Initiation d'un transfert de disponibilté";
            string IdDemande = RecupererIdDemandeEnCours();
            Console.WriteLine($"==================>Transfer /ping apres recuperation de IDdemandeEnCours");
            try
            {

                var validator = new QueryBodyTransactionDispoDtoValidator();
                var results = validator.Validate(_body);
                Console.WriteLine($"==================>Transfer /ping apres validator.Validate(_body)");

                Console.WriteLine($"==================>etat de resultat {results.IsValid}");

                if (!results.IsValid)
                {
                    Console.WriteLine($"==================>Transfer Resultat non valide {results.IsValid}");

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
                    Console.WriteLine($"Return bad request==================> Content :{problem}");

                    return BadRequest(problem);
                }

                Model.t_client data_client = GetInfoClient();

                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(_body.compte, data_client.Id);

                Console.WriteLine($"Return Le compte ==================> Content :{JsonConvert.SerializeObject(data_compte)}");

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
                Console.WriteLine($"Return L'ALIAS ==================> Content :{JsonConvert.SerializeObject(alias_client_connecte)}");

                if (alias_client_connecte == null)

                {
                    var problem = GeneraleRetour.BuildNotFound(
                      detail: "Le compte n'as pas d'alias dans le système",
                      instance: HttpContext.Request.Path
                  );

                    return NotFound(problem);
                }


                GeneraleRetour res_transaction = new GeneraleRetour();

                switch (_body.action)
                {
                    case "send_now": // Transfert immédiat

                        res_transaction = await _serviceTransfert.InitierTransfertDeDisponibilite(alias_client_connecte.valeurAlias, _body, IdDemande);
                        _logger.LogInformation($"Return send_now message  ==================> Content :{JsonConvert.SerializeObject(res_transaction)}");

                        break;

                    default:
                        _logger.LogInformation($"Return le default du send_now message  ==================> Content :{JsonConvert.SerializeObject(res_transaction)}");

                        return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
                }

                _logger.LogInformation($"Tools.Tools.RetourIsSucces(res_transaction.status)  ==================> Content :{Tools.Tools.RetourIsSucces(res_transaction.status)}");

                if (Tools.Tools.RetourIsSucces(res_transaction.status))
                {

                    var transaction = JsonConvert.DeserializeObject<t_transfert_dispo>(res_transaction.data);
                    _logger.LogInformation($"transaction  ==================> Content :{res_transaction.data}");

                    TransactionDto t = await _serviceTransfert.ConvertirTransfertEnTransfertDispoDto(transaction, data_compte.ibanOrOther);
                    _logger.LogInformation($"ONT RETOURNE LE CODE STATUS 200 A l'UTILISATEUR =====================>");
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
        [HttpPut("transferts/ping/{id}")]
        public async Task<IActionResult> ConfirmerUnTransfertDeDisponibilite(string id, [FromBody] QueryConfirmTransactionDto _body)
        {

            string _desc_route = "Confirmation d'un transfert de disponibilité";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

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
                    from t in _interopContext.t_transfert_dispo
                    join c in _interopContext.t_compte
                    on t.compteClientPayeur equals c.ibanOrOther
                    where t.endToEndId == id
                    && t.is_delete != true
                    && c.is_delete != true
                    && c.r_client_id == data_client.Id
                    && t.etape == ETAPE_TRANSFERT.INITIEE
                    select new
                    {
                        TransfertDispo = t,
                        data_compte = c
                    }
                    ).FirstOrDefaultAsync();


                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction ne figure pas parmi celles à confirmer.", instance: HttpContext.Request.Path));


                t_transfert_dispo transfert = _data.TransfertDispo;


                if ((_body.montant > 0 && _body.montant != transfert.montant) || (!string.IsNullOrEmpty(_body.motif) && transfert.motif != transfert.motif))
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Les données de confirmation ne sont pas conformes", instance: HttpContext.Request.Path));


                if (Tools.Tools.canalEstCanalDemandePaiement(_data.TransfertDispo.canalCommunication))
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


                e = await _serviceTransfert.ConfirmerTransfertDeDisponibilite(transfert, IdDemande);


                if (Tools.Tools.RetourIsSucces(e.status))
                {
                    TransactionDto t = await _serviceTransfert.ConvertirTransfertEnTransfertDispoDto(transfert, _data.data_compte.ibanOrOther);
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

        [Authorize]
        [HttpPut("transferts/{id}/rejets")]
        public async Task<IActionResult> RejeterUneDemandePaiementOuAnnulation(string id, [FromBody] QueryRejetMobileDto _body)
        {

            string _desc_route = "Rejeter une demande";
            string iddemande = RecupererIdDemandeEnCours();
            try
            {

                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la transaction est requis", instance: HttpContext.Request.Path));


                Model.t_client data_client = GetInfoClient();

                var _data = await (from t in _interopContext.t_transfert
                                   where t.endToEndId == id && t.is_delete != true
                                   let compte = (
                                   from c in _interopContext.t_compte
                                   where c.r_client_id == data_client.Id
                                   && c.is_delete != true
                                   && (c.ibanOrOther == t.compteClientPayeur || c.ibanOrOther == t.compteClientPaye)
                                   select c
                                   ).FirstOrDefault()
                                   select new
                                   {
                                       Transfert = t,
                                       data_compte = compte
                                   }
                                   ).FirstOrDefaultAsync();


                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction est introuvable dans le système", instance: HttpContext.Request.Path));


            

                bool bCanalPaiement = Tools.Tools.canalEstCanalDemandePaiement(_data.Transfert.canalCommunication);
                if (!((bCanalPaiement && _data.data_compte.ibanOrOther == _data.Transfert.compteClientPayeur) || (_data.data_compte.ibanOrOther == _data.Transfert.compteClientPaye)))
                {
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction est introuvable dans le système", instance: HttpContext.Request.Path));
                }


                GeneraleRetour r = new GeneraleRetour();
                if (bCanalPaiement && _data.data_compte.ibanOrOther == _data.Transfert.compteClientPayeur) // Alors c'est une demande à rejeter
                {
                    // Pour refuser une demande de paiement, je dois etre le payeur , et le canal est PAIEMENT
                    r = await _serviceTransfert.RefuserUneDemandePaiement(_data.Transfert, _body.raison, iddemande);
                }
                else
                {
                    // Pour refuser une annulation, je dois etre le payé
                    r = await _serviceTransfert.RefuserUneAnnulation(_data.Transfert, _body.raison, iddemande);
                }



                if (Tools.Tools.RetourIsSucces(r.status))
                {
                    t_transfert res = JsonConvert.DeserializeObject<t_transfert>(r.data);
                    TransactionDto t = await _serviceTransfert.ConvertirTransfertEnTransfertDto(res, _data.data_compte.ibanOrOther);

                    return StatusCode(200, t);
                }
                else
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, HttpContext.Request.Path));
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("transferts/{id}/categories/{categorie}")]
        public async Task<IActionResult> AjouterUnTransfertDansUneCategorie(string id, int categorie)
        {

            string _desc_route = "Ajouter un transfert dans une catégorie";

            try
            {


                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "La réference de l'opération est requis", instance: HttpContext.Request.Path));

                if (categorie <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de la catégorie est requis", instance: HttpContext.Request.Path));


                Model.t_client data_client = GetInfoClient();

                /// Recherche du transfert dans la liste des transferts
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
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le transfert est introuvable dans la liste", instance: HttpContext.Request.Path));


                t_transfert data_transfert = _data.Transfert;
                bool isClientPayeur = (_data.data_compte.ibanOrOther == data_transfert.compteClientPayeur);

                /// Recherche de la catégorie dans la liste des catégories du client
                var resp_query_categorie = await _interopContext.t_categories
                    .Where(c => (c.niveau == niveau_categorie.BACK_OFFICE || (c.niveau == niveau_categorie.CLIENT && c.r_client_id_fk == data_client.Id)
                    ) && c.is_delete != true && c.Id == categorie)
                      .FirstOrDefaultAsync();


                if (resp_query_categorie == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La catégorie est introuvable dans le système", instance: HttpContext.Request.Path));


                if (isClientPayeur == true) data_transfert.r_categorie_payeur_id_fk = categorie;
                if (isClientPayeur == false) data_transfert.r_categorie_paye_id_fk = categorie;

                _interopContext.t_transfert.Update(data_transfert);
                _interopContext.SaveChanges();


                return StatusCode(200, "Opération effectuée avec succès");


            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }


        }

        [Authorize]
        [HttpPut("transferts/{id}/retours")]
        public async Task<IActionResult> RetournerLesFonds(string id)
        {
            string _desc_route = "Retourner les fonds";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {

                Model.t_client data_client = GetInfoClient();

                var _data = await (
               from e in _interopContext.t_transfert
               from c in _interopContext.t_compte
               where e.endToEndId == id
               && e.is_delete != true
               && c.is_delete != true
               && c.r_client_id == data_client.Id
               && (c.ibanOrOther == e.compteClientPaye)
               select new
               {
                   Transfert = e,
                   data_compte = c
               }).FirstOrDefaultAsync();


                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La transaction est introuvable dans le système", instance: HttpContext.Request.Path));


                GeneraleRetour r = await _serviceTransfert.RetournerLesFonds(_data.Transfert, IdDemande);

                if (Tools.Tools.RetourIsSucces(r.status))
                    return StatusCode(200, "Retour de fonds en cours d'envoi");
                else
                    return StatusCode(r.status, GeneraleRetour.BuildProblemResponse(r, HttpContext.Request.Path));

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [Authorize]
        [HttpGet("transferts/{id}/retours/reponses")]
        [Produces("application/json", "text/event-stream")]

        public async Task<IActionResult> ObtenirStatutRetourFond(string id)
        {
            const string _desc_route = "Obtenir le statut d'un retour de fonds";

            if (string.IsNullOrWhiteSpace(id))
            {
                var problem = GeneraleRetour.BuildBadRequest(
                    detail: "L'identifiant du retour de fonds est requis",
                    instance: HttpContext.Request.Path
                );
                return BadRequest(problem);
            }

            // Si le client demande SSE
            var accept = Request.Headers["Accept"].ToString();
            var wantsSse = accept?.IndexOf("text/event-stream", StringComparison.OrdinalIgnoreCase) >= 0;
            if (wantsSse)
                return await StreamStatutRetourFondSse(id, HttpContext);

            // ---- JSON one-shot (comportement classique) ----
            try
            {
                var data_client = GetInfoClient(); // ta méthode existante
                if (data_client is null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Autorisations insuffisantes", instance: HttpContext.Request.Path));

                await using var db = await _dbFactory.CreateDbContextAsync(HttpContext.RequestAborted);
               
                var result = await (
                    from r in db.t_retour_fonds.AsNoTracking()
                    join t in db.t_transfert.AsNoTracking()
                        on r.endToEndId equals t.endToEndId
                    where r.endToEndId == id
                          && r.is_delete != true
                          && t.is_delete != true
                    select new
                    {
                        Retour = r,
                        compteClientPayeur = t.compteClientPayeur,
                        compteClientPaye = t.compteClientPaye
                    }
                ).FirstOrDefaultAsync(HttpContext.RequestAborted);


                if (result is null)
                    return NotFound(GeneraleRetour.BuildNotFound("Le retour de fonds est introuvable dans le système", HttpContext.Request.Path));

                // Verifier si je suis le payé, car seul les payés peuvent faire des retours de fonds
                var isPaye = await db.t_compte
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.is_delete != true &&
                        c.r_client_id == data_client.Id &&
                        (c.ibanOrOther == result.compteClientPaye),
                        HttpContext.RequestAborted);

                if (!isPaye)
                    return NotFound(GeneraleRetour.BuildNotFound("Le retour de fonds est introuvable dans le système", HttpContext.Request.Path));

                var dto = new TransactionStatutDto
                {
                    statut = result.Retour.statut.ToString(),
                    codeRejet = result.Retour.codeRejet,
                    dateIrrevocabilite = result.Retour.dateHeureIrrevocabilite
                };


                // Completer avec la description de l'erreur si le retour de fonds est rejeté
                if (dto.statut == statutRetourFond.rejete.ToString())
                {
                    const string DEFAULT_MSG = "Retour de fonds echoué";

                    string? libelle = null;
                    if (!string.IsNullOrWhiteSpace(dto.codeRejet))
                    {
                        libelle = await _codeErreurRepo.GetLibelleErreurAsync(
                            dto.codeRejet,
                            tag_erreur.CODE_RAISON_REJET.ToString()
                        );
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


        [NonAction]
        private async Task<IActionResult> StreamStatutRetourFondSse(string id, HttpContext ctx)
        {
            const int POLL_SECONDS = 2;

            try
            {
                var data_client = GetInfoClient(); // si dispo ici; sinon passe-le en param
                await using var db = await _dbFactory.CreateDbContextAsync(ctx.RequestAborted);

                var ct = ctx.RequestAborted;

                // r (retour) + t (transfert) pour restreindre au client
                var query =
                    from r in db.t_retour_fonds.AsNoTracking()
                    join t in db.t_transfert.AsNoTracking()
                        on r.endToEndId equals t.endToEndId
                    where r.endToEndId == id
                          && r.is_delete != true
                          && t.is_delete != true
                    select new
                    {
                        Retour = r,
                        compteClientPayeur = t.compteClientPayeur,
                        compteClientPaye = t.compteClientPaye
                    };

                if (data_client is not null)
                {
                    query =
                        from q in query
                        where db.t_compte.AsNoTracking().Any(c =>
                                c.is_delete != true &&
                                c.r_client_id == data_client.Id &&
                                (c.ibanOrOther == q.compteClientPayeur || c.ibanOrOther == q.compteClientPaye)
                             )
                        select q;
                }

                var count = await query.CountAsync(ct);
                if (count == 0)
                {
                    return new NotFoundObjectResult(
                        GeneraleRetour.BuildNotFound("La transaction est introuvable dans le système", ctx.Request.Path)
                    );
                }
                if (count > 1)
                {
                    _logger.LogWarning("SSE retour_fonds {Id}: {Count} lignes trouvées. On prendra la plus récente.", id, count);
                }

                var res = ctx.Response;
                res.StatusCode = StatusCodes.Status200OK;
                res.Headers["Content-Type"] = "text/event-stream";
                res.Headers["Cache-Control"] = "no-cache";
                res.Headers["Connection"] = "keep-alive";
                res.Headers["X-Accel-Buffering"] = "no";
                await res.Body.FlushAsync(ct);

                statutRetourFond? lastStatut = null;
                string? lastCode = null;
                DateTime? lastIrrev = null;

                while (!ct.IsCancellationRequested)
                {
                    // On lit etape pour logique interne, mais on ne l’envoie pas
                    var snap = await (from q in query 
                                      let r = q.Retour
                                      orderby r.dateHeureIrrevocabilite descending,
                                      r.r_createdon descending
                                      select new
                                      {
                                          statut = r.statut,
                                          etape = r.etape,
                                          codeRejet = r.codeRejet,
                                          dateIrrevocabilite = r.dateHeureIrrevocabilite
                                      }).FirstOrDefaultAsync(ct);


                    if (snap is null)
                    {
                        await SendEventAsync(res, "deleted",
                            Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Retour supprimé" }),
                            ctx);
                        break;
                    }

                    // ---- LOGIQUE DEMANDÉE (statut effectif) ----
                    // 1) si etape == rejete => REJETE
                    // 2) sinon si statut == IRREVOCABLE:
                    //      - etape in {valide, rejete} => IRREVOCABLE (final)
                    //      - sinon => INITIE (pas encore irrévocable)
                    // 3) sinon => statut tel quel
                    var effectiveStatut =
                        (snap.etape == etapeRetourFond.rejete)
                            ? statutRetourFond.rejete
                            : (snap.statut == statutRetourFond.irrevocable
                                ? ((snap.etape == etapeRetourFond.valide || snap.etape == etapeRetourFond.rejete)
                                    ? statutRetourFond.irrevocable
                                    : statutRetourFond.initie)
                                : snap.statut);

                    // Détection de changement (côté client on ne considère pas etape)
                    var changed = effectiveStatut != lastStatut
                                  || snap.codeRejet != lastCode
                                  || snap.dateIrrevocabilite != lastIrrev;

                    if (changed)
                    {
                        lastStatut = effectiveStatut;
                        lastCode = snap.codeRejet;
                        lastIrrev = snap.dateIrrevocabilite;


                        string? detailRejet = null;

                        if (lastStatut == statutRetourFond.rejete)
                        {
                            const string DEFAULT_MSG = "Retour de fonds echoué";

                            string? libelle = null;
                            if (!string.IsNullOrWhiteSpace(lastCode))
                            {
                                libelle = await _codeErreurRepo.GetLibelleErreurAsync(
                                    lastCode,
                                    tag_erreur.CODE_RAISON_REJET.ToString()
                                );
                            }

                            // Si pas de code, ou si le repo ne renvoie rien de probant => message par défaut
                            detailRejet = !string.IsNullOrWhiteSpace(libelle) ? libelle : DEFAULT_MSG;
                        }


                        // N'envoyer dateIrrevocabilite que si statut effectif est IRREVOCABLE
                        var payload = new
                        {
                            statut = lastStatut.ToString(),
                            codeRejet = lastCode.ToString(),
                            detailRejet,
                            dateIrrevocabilite = (lastStatut == statutRetourFond.irrevocable)
                                ? lastIrrev
                                : (DateTime?)null
                        };

                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                        await SendEventAsync(res, "statut-update", json, ctx);

                        // Terminer lorsque c'est irrévocable (effectif)
                        if (lastStatut == statutRetourFond.irrevocable)
                        {
                            await SendEventAsync(res, "finalized", json, ctx);
                            break;
                        }
                    }
                    else
                    {
                        await res.WriteAsync($": keep-alive {DateTime.UtcNow:o}\n\n", ct);
                        await res.Body.FlushAsync(ct);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(POLL_SECONDS), ct);
                }
            }
            catch (OperationCanceledException)
            {
                // client parti: OK
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur SSE retour_fonds {Id}", id);
            }

            return new EmptyResult();
        }


        [Authorize]
        [HttpPost("souscriptions")]
        public async Task<IActionResult> FaireUneSouscription([FromBody] QueryCreateSouscriptionDto _body)
        {

            string _desc_route = "Création d'une souscription";

            try
            {

                var validator = new QueryCreateSouscriptionDtoValidator();
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


                var _data = await (
                   from e in _interopContext.t_transfert
                   from c in _interopContext.t_compte
                   where e.endToEndId == _body.endToEndId
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
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La souscription sélectionnée est introuvable",
                       instance: HttpContext.Request.Path
                   );
                }

                t_transfert transfert_initie = _data.Transfert;

                DataPayeDto data_paye = new DataPayeDto
                {

                    endToEndId = transfert_initie.endToEndId,
                    iban = transfert_initie.ibanClientPaye,
                    other = transfert_initie.otherClientPaye,
                    typeCompte = transfert_initie.typeCompteClientPaye,
                    alias = transfert_initie.aliasClientPaye,
                    participant = transfert_initie.codeMembreParticipantPaye,
                    ville = transfert_initie.villeClientPaye,
                    nom = transfert_initie.nomClientPaye,
                    adresseComplete = transfert_initie.adresseClientPaye,
                    dateNaissance = transfert_initie.dateNaissanceClientPaye,
                    paysNaissance = transfert_initie.paysNaissanceClientPaye,
                    villeNaissance = transfert_initie.villeNaissanceClientPaye,
                    paysResidence = transfert_initie.paysClientPaye,
                    numeroIdentification = transfert_initie.numeroIdentificationClientPaye,
                    systemeIdentification = transfert_initie.systemeIdentificationClientPaye,
                    photo = transfert_initie.photoClientPaye,
                    devise = transfert_initie.deviseCompteClientPaye,
                    numeroRCCM = transfert_initie.numeroRCCMClientPaye,
                    type = transfert_initie.typeClientPaye,
                };

                DataPayeDto data_payeur = new DataPayeDto
                {

                    endToEndId = transfert_initie.endToEndId,
                    iban = transfert_initie.ibanClientPayeur,
                    other = transfert_initie.otherClientPayeur,
                    typeCompte = transfert_initie.typeCompteClientPayeur,
                    alias = transfert_initie.aliasClientPayeur,
                    participant = transfert_initie.codeMembreParticipantPayeur,
                    ville = transfert_initie.villeClientPayeur,
                    nom = transfert_initie.nomClientPayeur,
                    adresseComplete = transfert_initie.adresseClientPayeur,
                    dateNaissance = transfert_initie.dateNaissanceClientPayeur,
                    paysNaissance = transfert_initie.paysNaissanceClientPayeur,
                    villeNaissance = transfert_initie.villeNaissanceClientPayeur,
                    paysResidence = transfert_initie.paysClientPayeur,
                    numeroIdentification = transfert_initie.numeroIdentificationClientPayeur,
                    systemeIdentification = transfert_initie.systemeIdentificationClientPayeur,
                    photo = transfert_initie.photoClientPayeur,
                    devise = transfert_initie.deviseCompteClientPayeur,
                    numeroRCCM = transfert_initie.numeroRCCMClientPayeur,
                    type = transfert_initie.typeClientPayeur,
                };


                JsonDocument jsonDoc_data_paye = JsonDocument.Parse(JsonConvert.SerializeObject(data_paye));
                JsonDocument jsonDoc_data_payeur = JsonDocument.Parse(JsonConvert.SerializeObject(data_payeur));


                t_scheduled newScheduled = new t_scheduled
                {

                    aliasClientPayeur = transfert_initie.aliasClientPayeur,
                    ibanClientPayeur = transfert_initie.ibanClientPayeur,
                    otherClientPayeur = transfert_initie.otherClientPayeur,
                    nomClientPayeur = transfert_initie.nomClientPayeur,
                    typeClientPayeur = transfert_initie.typeClientPayeur,
                    codeMembreParticipantPayeur = transfert_initie.codeMembreParticipantPayeur,
                    typeCompteClientPayeur = transfert_initie.typeCompteClientPayeur,
                    deviseCompteClientPayeur = transfert_initie.deviseCompteClientPayeur,
                    paysClientPayeur = transfert_initie.paysClientPayeur,
                    photoClientPayeur = transfert_initie.photoClientPayeur,
                    adresseClientPayeur = transfert_initie.adresseClientPayeur,
                    villeClientPayeur = transfert_initie.villeClientPayeur,
                    numeroIdentificationClientPayeur = transfert_initie.numeroIdentificationClientPayeur,
                    systemeIdentificationClientPayeur = transfert_initie.systemeIdentificationClientPayeur,
                    dateNaissanceClientPayeur = transfert_initie.dateNaissanceClientPayeur,
                    villeNaissanceClientPayeur = transfert_initie.villeNaissanceClientPayeur,
                    paysNaissanceClientPayeur = transfert_initie.paysNaissanceClientPayeur,
                    numeroRCCMClientPayeur = transfert_initie.numeroRCCMClientPayeur,

                    endToEndId = transfert_initie.endToEndId,
                    montant = transfert_initie.montant,
                    motif = transfert_initie.motif,
                    canal = transfert_initie.canalCommunication,
                    dateDebut = _body.dateDebut,
                    nextExecution = _body.dateDebut,
                    frequence = _body.frequence,
                    periodicite = _body.periodicite,
                    latitudeClientPayeur = transfert_initie.longitudeClientPayeur,
                    longitudeClientPayeur = transfert_initie.longitudeClientPayeur,
                    r_categorie_id_fk = transfert_initie.r_categorie_payeur_id_fk,
                    data_paye = jsonDoc_data_paye,
                    data_payeur = jsonDoc_data_payeur,
                    statut = STATUT_TRANSFERT.initie,
                    dateFin = _body.dateFin,

                    aliasClientPaye = transfert_initie.aliasClientPaye,
                    ibanClientPaye = transfert_initie.ibanClientPaye,
                    otherClientPaye = transfert_initie.otherClientPaye,
                    photoClientPaye = transfert_initie.photoClientPaye,
                    paysClientPaye = transfert_initie.paysClientPaye,
                    typeCompteClientPaye = transfert_initie.typeCompteClientPaye,
                    deviseCompteClientPaye = transfert_initie.deviseCompteClientPaye,
                    numeroRCCMClientPaye = transfert_initie.numeroRCCMClientPaye,
                    numeroIdentificationClientPaye = transfert_initie.numeroIdentificationClientPaye,
                    systemeIdentificationClientPaye = transfert_initie.systemeIdentificationClientPaye,
                    codeMembreParticipantPaye = transfert_initie.codeMembreParticipantPaye,
                    dateNaissanceClientPaye = transfert_initie.dateNaissanceClientPaye,
                    villeNaissanceClientPaye = transfert_initie.villeNaissanceClientPaye,
                    paysNaissanceClientPaye = transfert_initie.paysNaissanceClientPaye,
                    adresseClientPaye = transfert_initie.adresseClientPaye,
                    villeClientPaye = transfert_initie.villeClientPaye,
                    longitudeClientPaye = transfert_initie.longitudeClientPaye,
                    latitudeClientPaye = transfert_initie.latitudeClientPaye,
                    nomClientPaye = transfert_initie.nomClientPaye,
                    typeClientPaye = transfert_initie.typeClientPaye,
                    type = type_planifie.SOUSCRIPTION,
                    txId = transfert_initie.identifiantTransaction,
                    typeDocumentReference = transfert_initie.typeDocumentReference,
                    numeroDocumentReference = transfert_initie.numeroDocumentReference,
                    montantAchat = transfert_initie.montantAchat,
                    montantRetrait = transfert_initie.montantRetrait,
                    fraisRetrait = transfert_initie.fraisRetrait,
                    signatureNumeriqueMandat = transfert_initie.signatureNumeriqueMandat,
                    montantRemisePaiementImmediat = transfert_initie.montantRemisePaiementImmediat,
                    autorisationModificationMontant = transfert_initie.autorisationModificationMontant,
                    tauxRemisePaiementImmediat = transfert_initie.tauxRemisePaiementImmediat,
                    r_client_auteur_id_fk = data_client.Id
                };

                newScheduled.compteClientPaye = transfert_initie.ibanClientPaye;
                if (string.IsNullOrEmpty(newScheduled.compteClientPaye))
                    newScheduled.compteClientPaye = transfert_initie.otherClientPaye;

                newScheduled.compteClientPayeur = transfert_initie.ibanClientPayeur;
                if (string.IsNullOrEmpty(newScheduled.compteClientPayeur))
                    newScheduled.compteClientPayeur = transfert_initie.otherClientPayeur;


                _interopContext.t_scheduled.Add(newScheduled);
                _interopContext.SaveChanges();

                bool isPayeur = (newScheduled.compteClientPayeur == _data.data_compte.ibanOrOther);
                TransactionDto d = await _serviceTransfert.CreateSouscriptionMobileDto(newScheduled, isPayeur);

                return Ok(d);

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }


        [Authorize]
        [HttpGet("souscriptions/{id}/details")]
        public async Task<IActionResult> RecupererUneSouscription([FromRoute] string id)
        {

            string _desc_route = "Détails d'une souscription";

            try
            {


                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant de la souscription est requis",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }

                Model.t_client data_client = GetInfoClient();


                var _data = await (
                 from s in _interopContext.t_scheduled
                 from c in _interopContext.t_compte
                 where s.Id.ToString() == id
                 && s.is_delete != true
                 && c.is_delete != true
                 && s.r_client_auteur_id_fk == data_client.Id
                 && c.r_client_id == data_client.Id && (c.ibanOrOther == s.compteClientPayeur || c.ibanOrOther == s.compteClientPaye)
                 select new
                 {
                     Scheduled = s,
                     data_compte = c
                 }).FirstOrDefaultAsync();


                if (_data == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La souscription n'existe pas dans le système",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }


                t_scheduled t_scheduled = _data.Scheduled;

                bool isPayeur = (t_scheduled.compteClientPayeur == _data.data_compte.ibanOrOther);

                TransactionDto d = await _serviceTransfert.CreateSouscriptionMobileDto(t_scheduled, isPayeur);

                return Ok(d);

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpGet("souscriptions/{compte}")]

        public async Task<IActionResult> ListeDesSouscriptions([FromRoute] string compte, [FromQuery] int? page, [FromQuery] int? limit, [FromQuery] string? sort_by, [FromQuery] string? categorie)
        {

            string _desc_route = "Liste des souscriptions";

            try
            {

                Model.t_client data_client = GetInfoClient();
                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(compte, data_client.Id);

                if (data_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                var q = _interopContext.t_scheduled.AsQueryable();
                q = q.Where(t => (t.compteClientPaye == data_compte.ibanOrOther || t.compteClientPayeur == data_compte.ibanOrOther) && t.is_delete != true);


                // Filtrer par catégorie
                if (!string.IsNullOrEmpty(categorie))
                {
                    q = q.Where(t => t.r_categorie_id_fk.ToString() == categorie);
                }

                q = q.OrderByDescending(s => s.Id);

                int total = await q.CountAsync(); // Compte total pour toutes les pages

                int pages = 1;
                if (page != null)
                    pages = (int)page;

                int limits = 25;
                if (limit != null)
                    limits = (int)limit;

                if (pages <= 0)
                    pages = 1;

                if (limits <= 0)
                    limits = 10;

                // Pagination
                if (limit > 0)
                {
                    q = q.Skip((pages - 1) * limits).Take(limits);
                }


                // Charger la liste des pays
                Dictionary<string, string> countryDictionary = await _datarepo.getDataInDictionaryByCode(code_datas.PAYS.ToString());
                List<t_participant> Participants = await _participantrepo.getAll();

                var data = await q.Select(t => _serviceTransfert.ConvertirSouscriptionEnTransfertDto(t, data_compte.ibanOrOther, countryDictionary, Participants)).ToListAsync();

                string? previousPage = pages > 1 ? (pages - 1).ToString() : null;
                string? nextPage = pages * limits < total ? (pages + 1).ToString() : null;

                // Création de l'objet meta
                var meta = new MetaDto
                {
                    total = total,
                    previous = previousPage,
                    next = nextPage,
                    current = pages.ToString(),
                    limit = limits
                };

                return Ok(
                    new
                    {
                        data = data,
                        meta = meta,
                    }
               );
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }

        }

        [Authorize]
        [HttpPut("souscriptions/{id}/reactivations")]
        [HttpPut("souscriptions/{id}/desactivations")]
        public async Task<IActionResult> ChangerStatutDUneSouscription([FromRoute] string id)
        {

            bool isDesactive = HttpContext.Request.Path.ToString().Contains("desactivations");

            string _desc_route = "Modification du statut d'une souscription";

            try
            {

                if (isDesactive == true) // Désactivation d'une planification 
                    _desc_route = "Désactivation d'une souscription";
                else // Activation d'une planification
                    _desc_route = "Réactivation d'une souscription";


                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant de la souscription est requis",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }

                Model.t_client data_client = GetInfoClient();


                var _data = await (
                 from s in _interopContext.t_scheduled
                 from c in _interopContext.t_compte
                 where s.Id.ToString() == id
                 && s.is_delete != true
                 && c.is_delete != true
                 && s.r_client_auteur_id_fk == data_client.Id
                 && c.r_client_id == data_client.Id && (c.ibanOrOther == s.compteClientPayeur || c.ibanOrOther == s.compteClientPaye)
                 select new
                 {
                     Scheduled = s,
                     data_compte = c
                 }).FirstOrDefaultAsync();


                if (_data == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La souscription n'existe pas dans le système",
                       instance: HttpContext.Request.Path
                   );

                    return NotFound(problem);
                }


                t_scheduled t_scheduled = _data.Scheduled;

                string _msg_retour = "";
                switch (isDesactive)
                {
                    case false: // Activation d'une planification

                        if (t_scheduled.statut != STATUT_TRANSFERT.desactive)
                        {
                            var problem = GeneraleRetour.BuildProblemResponse(
                                new GeneraleRetour { status = 409, detail = "Le statut de la souscription ne permet pas cette action" },
                                instance: HttpContext.Request.Path);

                            return StatusCode(403, problem);
                        }


                        t_scheduled.statut = STATUT_TRANSFERT.initie;
                        _msg_retour = "Souscription réactivée avec succès";

                        break;
                    case true: // Désactivation d'une planification


                        if (t_scheduled.statut != STATUT_TRANSFERT.initie)
                        {
                            var problem = GeneraleRetour.BuildProblemResponse(
                                new GeneraleRetour { status = 409, detail = "Le statut de la souscription ne permet pas cette action", },
                                instance: HttpContext.Request.Path);
                            return StatusCode(403, problem);
                        }

                        t_scheduled.statut = STATUT_TRANSFERT.desactive;
                        _msg_retour = "Souscription désactivée avec succès";

                        break;
                }

                _interopContext.t_scheduled.Update(t_scheduled);
                await _interopContext.SaveChangesAsync();


                bool isPayeur = (t_scheduled.compteClientPayeur == _data.data_compte.ibanOrOther);

                TransactionDto d = await _serviceTransfert.CreateSouscriptionMobileDto(t_scheduled, isPayeur);

                return Ok(d);

            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpDelete("souscriptions/{id}")]

        public async Task<IActionResult> SupprimerUneSouscription(string id)
        {

            string _desc_route = "Suppression de souscription";

            try
            {

                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildProblemResponse(
                         new GeneraleRetour { status = 400, detail = "L'identifiant de la souscription est requis", },
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }


                Model.t_client data_client = GetInfoClient();


                var _data = await (
                    from s in _interopContext.t_scheduled
                    from c in _interopContext.t_compte
                    where s.Id.ToString() == id
                    && s.is_delete != true
                    && c.is_delete != true
                    && s.r_client_auteur_id_fk == data_client.Id
                    && c.r_client_id == data_client.Id && (c.ibanOrOther == s.compteClientPayeur || c.ibanOrOther == s.compteClientPaye)
                    select new
                    {
                        Scheduled = s,
                        data_compte = c
                    }).FirstOrDefaultAsync();

                if (_data == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La souscription sélectionnée est introuvable",
                       instance: HttpContext.Request.Path
                   );
                    return NotFound(problem);
                }

                t_scheduled t_scheduled = _data.Scheduled;
                t_scheduled.is_delete = true;

                _interopContext.t_scheduled.Update(t_scheduled);
                await _interopContext.SaveChangesAsync();

                return NoContent();
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                var problem = GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path);
                return StatusCode(500, problem);
            }
        }

        [Authorize]
        [HttpPut("souscriptions/{id}")]
        public async Task<IActionResult> ModifierUneSouscription(string id, [FromBody] QueryUpdateSouscriptionDto _body)
        {


            string _desc_route = "Modifier une souscription";

            try
            {


                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant de la souscription est requis",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }


                var validator = new QueryUpdateSouscriptionDtoValidator();
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


                var _data = await (
                                   from s in _interopContext.t_scheduled
                                   from c in _interopContext.t_compte
                                   where s.Id.ToString() == id
                                   && s.is_delete != true
                                   && c.is_delete != true
                                   && s.r_client_auteur_id_fk == data_client.Id
                                   && c.r_client_id == data_client.Id && (c.ibanOrOther == s.compteClientPayeur || c.ibanOrOther == s.compteClientPaye)
                                   select new
                                   {
                                       Scheduled = s,
                                       data_compte = c
                                   }).FirstOrDefaultAsync();

                if (_data == null)
                {
                    var problem = GeneraleRetour.BuildNotFound(
                       detail: "La souscription sélectionnée est introuvable",
                       instance: HttpContext.Request.Path
                   );
                    return NotFound(problem);
                }

                t_scheduled t_scheduled = _data.Scheduled;

                //if (t_scheduled.statut != STATUT_TRANSFERT.EN_INITIE)
                //{
                //    var problem = GeneraleRetour.BuildForbid( detail : "Le statut de la souscription ne permet pas cette action", 
                //        instance: HttpContext.Request.Path );
                //    return StatusCode(403,problem);
                //}


                t_scheduled.motif = _body.motif;
                _interopContext.t_scheduled.Update(t_scheduled);
                await _interopContext.SaveChangesAsync();


                bool isPayeur = (t_scheduled.compteClientPayeur == _data.data_compte.ibanOrOther);

                TransactionDto d = await _serviceTransfert.CreateSouscriptionMobileDto(t_scheduled, isPayeur);

                return Ok(d);
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


        [Authorize]
        [HttpGet("notifications/{compte}")]
        public async Task<IActionResult> ListeDesNotifications(string compte, [FromQuery] int? page, [FromQuery] int? limit, [FromQuery] string? sort_by, [FromQuery] string? keyword, [FromQuery] string? type, [FromQuery] DateTime? dateDebut, [FromQuery] DateTime? dateFin)
        {


            string _desc_route = "Liste des notifications";

            try
            {


                if (string.IsNullOrEmpty(compte))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de compte est requis", instance: HttpContext.Request.Path));

                Model.t_client data_client = GetInfoClient();
                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(compte, data_client.Id);

                if (data_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                var query = _interopContext.t_notification.AsQueryable();
                query = query.Where(t => (t.compte == data_compte.ibanOrOther) && t.is_delete != true);


                if (!string.IsNullOrEmpty(type))
                {

                    var types = type.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(t => t.Trim()) // pour enlever les espaces
                   .ToList();

                    query = query.Where(t => types.Contains(t.type));

                }


                if (dateDebut.HasValue)
                    query = query.Where(t => t.dateAction >= dateDebut.Value);


                if (dateFin.HasValue)
                    query = query.Where(t => t.dateAction <= dateFin);



                /// Verification des filtres

                if (!string.IsNullOrEmpty(sort_by))
                {

                    // Tri
                    if (sort_by == "-dateAction")
                    {
                        query = query.OrderByDescending(t => t.dateAction);
                    }
                    else if (sort_by == "dateAction")
                    {
                        query = query.OrderBy(t => t.dateAction);
                    }
                    else
                    {
                        query = query.OrderByDescending(t => t.dateAction);
                    }
                }
                else
                {
                    query = query.OrderByDescending(t => t.dateAction);
                }

                int total = await query.CountAsync(); // Compte total pour toutes les pages

                int pages = 1;
                if (page != null)
                    pages = (int)page;

                int limits = 25;
                if (limit != null)
                    limits = (int)limit;

                if (pages <= 0)
                    pages = 1;

                if (limits <= 0)
                    limits = 10;

                // Pagination
                if (limit > 0)
                {
                    query = query.Skip((pages - 1) * limits).Take(limits);
                }



                var data = await query.Select(t => new NotificationDto
                {
                    id = t.Id.ToString(),
                    type = t.type,
                    idObject = t.idObject,
                    dateAction = t.dateAction,
                    dateLecture = t.dateLecture,
                    estCliquable = t.estCliquable,
                    details = t.details
                }).ToListAsync();




                string? previousPage = pages > 1 ? (pages - 1).ToString() : null;
                string? nextPage = pages * limits < total ? (pages + 1).ToString() : null;

                // Création de l'objet meta
                var meta = new MetaDto
                {
                    total = total,
                    previous = previousPage,
                    next = nextPage,
                    current = pages.ToString(),
                    limit = limits
                };

                return Ok(
                    new
                    {
                        data = data,
                        meta = meta,
                    }
               );
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpGet("notifications/{compte}/non-lues")]
        public async Task<IActionResult> NombreNotificationsNonLues(string compte)
        {


            string _desc_route = "Nombre de notifications non lues";

            try
            {


                if (string.IsNullOrEmpty(compte))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de compte est requis", instance: HttpContext.Request.Path));

                Model.t_client data_client = GetInfoClient();
                t_compte data_compte = await _compteRepo.SearchCompteByIbanOrOther(compte, data_client.Id);

                if (data_compte == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le compte est inconnu", instance: HttpContext.Request.Path));


                string myIbanOrOther = data_compte.ibanOrOther;

                var query = _interopContext.t_notification.AsQueryable();
                query = query.Where(t =>
                ((t.compte == data_compte.ibanOrOther))
                && t.is_delete != true
                && t.dateLecture == null);

                int total = await query.CountAsync(); // Nombre non lues

                return Ok(new { total = total });
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("notifications/{id}")]
        public async Task<IActionResult> MarquerNotificationCommeLue(string id, [FromBody] QueryUpdateNotificationDto _body)
        {


            string _desc_route = "Marquer comme lue";

            try
            {


                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant de la notification est requis",
                        instance: HttpContext.Request.Path
                    );

                    return BadRequest(problem);
                }


                Model.t_client data_client = GetInfoClient();

                var _data = await _interopContext.t_notification
               .Where(p => p.Id.ToString() == id && p.is_delete != true)
               .Select(p => new
               {
                   notification = p,
                   data_compte = _interopContext.t_compte
                   .Where(c =>
                   c.r_client_id == data_client.Id && c.is_delete != true && ((
                    (c.ibanOrOther == p.compte)
              )
           )).FirstOrDefault()
               }).FirstOrDefaultAsync();

                if (_data == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "La notification n'existe pas dans le système", instance: HttpContext.Request.Path));



                t_notification t_notif = _data.notification;

                t_notif.dateLecture = _body.dateLecture;
                _interopContext.t_notification.Update(t_notif);
                await _interopContext.SaveChangesAsync();


                NotificationDto n = new NotificationDto
                {
                    id = t_notif.Id.ToString(),
                    type = t_notif.type,
                    idObject = t_notif.idObject,
                    dateAction = t_notif.dateAction,
                    dateLecture = t_notif.dateLecture,
                    estCliquable = t_notif.estCliquable,
                    details = t_notif.details
                };


                return Ok(n);
            }

            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [DisableRateLimiting]
        [HttpPost("souscriptions/{id}/jobs/run")]
        public async Task<IActionResult> LancerLeCronDeLaSouscription([FromRoute] string id)
        {
            string _desc_route = "Lancer le cron de la souscription";
            
            try
            {

                if (string.IsNullOrWhiteSpace(id))
                {
                    var problem = GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant de la souscription est requis",
                        instance: HttpContext.Request.Path
                    );
                    return BadRequest(problem);
                }

                GeneraleRetour r = await _serviceTransfert.ScheduledProcessus(id,"");

                if (Tools.Tools.RetourIsSucces(r.status))
                  return StatusCode(200, r.detail);
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




      [NonAction]
      private static (string? appId, string? appSecret) ReadBasicOrBody(HttpRequest request, AppConnexionDto? body)
                {
                    string? appId = null, appSecret = null;

                    if (request.Headers.TryGetValue("Authorization", out var auth) &&
                        auth.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var raw = Convert.FromBase64String(auth.ToString()["Basic ".Length..].Trim());
                            var parts = Encoding.UTF8.GetString(raw).Split(':', 2);
                            if (parts.Length == 2) { appId = parts[0]; appSecret = parts[1]; }
                        }
                        catch { /* ignore */ }
                    }

                    appId ??= body?.app_id;
                    appSecret ??= body?.app_secret;
                    return (appId, appSecret);
                }



            }
}