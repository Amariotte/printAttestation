using ask.ContextDb;
using ask.Dtos;
using ask.Dtos.General;
using ask.Dtos.Request.auth;
using ask.Dtos.Request.Auth;
using ask.Interface;
using ask.Model;
using ask.Services;
using FluentValidation;
using InteroperabiliteProject.DtoAppMobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Collections.Specialized.BitVector32;

namespace ask.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly askContext _dbContext;
        private readonly IDbContextFactory<askContext> _dbFactory;
        private readonly JwtService _jwtService;
        private readonly ServiceMessagerie _serviceMessagerie;
        private readonly IotpRepo _otpRepo;
        private readonly IUserRepo _userRepo;
        private readonly ParamMessage _paramdata;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            askContext dbContext,
            IDbContextFactory<askContext> dbFactory,
            JwtService jwtService,
            ServiceMessagerie serviceMessagerie,
            IotpRepo otpRepo,
            IUserRepo userRepo,
            Microsoft.Extensions.Options.IOptions<ParamMessage> paramdata,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _dbFactory = dbFactory;
            _jwtService = jwtService;
            _serviceMessagerie = serviceMessagerie;
            _otpRepo = otpRepo;
            _userRepo = userRepo;
            _paramdata = paramdata.Value;
            _logger = logger;
            _configuration = configuration;
        }


        #region Helpers

        [NonAction]
        public string RecupererIdDemandeEnCours()
        {
            return Request.Headers["id-dmd-header"].ToString();
        }

        [NonAction]
        public string RecupererToken()
        {
            if (Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                string token = authorizationHeader.ToString();
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();
                return token;
            }
            return "";
        }

        [NonAction]
        public t_user? GetInfoUser()
        {
            if (HttpContext.Items.ContainsKey("User"))
                return (t_user)HttpContext.Items["User"];
            return null;
        }

        #endregion


        /// <summary>
        /// POST api/auth/register
        /// Inscription d'un nouvel utilisateur avec envoi d'un OTP de confirmation.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
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

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams));
                }

                var existingUser = await _dbContext.t_user
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.r_email == _body.email && u.r_is_delete != true);

                if (existingUser != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour { status = 409, detail = "Un utilisateur avec cet email existe déjà" },
                        instance: HttpContext.Request.Path));

                var user = new t_user
                {
                    r_nom = _body.nom,
                    r_email = _body.email,
                    r_telephone = _body.telephone,
                    r_password = BCrypt.Net.BCrypt.HashPassword(_body.password)
                };

                await _dbContext.t_user.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = "Inscription effectuée avec succès.",
                    userId = user.r_id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// POST api/auth/register/confirm
        /// Confirmation de l'inscription via un OTP.
        /// </summary>
        [HttpPost("register/confirm")]
        [AllowAnonymous]
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

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams));
                }

                (int res_otp, var o) = await _otpRepo.verifieOtpAndChallenge(_body.otp, TYPE_OTP.CONFIRMATION_REGISTER, _body.challenge);

                switch (res_otp)
                {
                    case 0:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Expiré", instance: HttpContext.Request.Path));
                    case -1:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));
                    case 1:
                        break;
                }

                var user = await _dbContext.t_user
                    .FirstOrDefaultAsync(p => p.r_id.ToString() == o.idOperationParent && p.r_is_delete != true);

                if (user == null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));

                user.r_is_active = true;
                _dbContext.t_user.Update(user);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Inscription confirmée avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// POST api/auth/login
        /// Authentification d'un utilisateur (login).
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Authentification([FromBody] ConnexionDto _body)
        {
            string _desc_route = "Authentification";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {
                var validator = new ConnexionDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams));
                }

                var user = await _dbContext.t_user
                    .FirstOrDefaultAsync(c => c.r_email == _body.email && c.r_is_delete != true);

                if (user == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Identifiants invalides",
                        instance: HttpContext.Request.Path));

                if (string.IsNullOrEmpty(user.r_password) || !BCrypt.Net.BCrypt.Verify(_body.password, user.r_password))
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Identifiants invalides",
                        instance: HttpContext.Request.Path));

                if (user.r_is_active != true)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Le compte n'est pas actif",
                        instance: HttpContext.Request.Path));

                // Charger les rôles de l'utilisateur
                var userRoles = await _dbContext.t_user_roles
                    .Include(ur => ur.r_roleTab)
                    .Where(ur => ur.r_user_id_fk == user.r_id && ur.r_is_delete != true && ur.r_roleTab.r_is_delete != true)
                    .ToListAsync();

                string[] roleCodes = userRoles.Select(ur => ur.r_roleTab!.r_code).ToArray();
                string[] adminRoleCodes = userRoles.Where(ur => ur.r_is_admin).Select(ur => ur.r_roleTab!.r_code).ToArray();
                int[] roleIds = userRoles.Select(ur => ur.r_role_id_fk).ToArray();

                // Charger les scopes associés aux rôles
                var scopes = await _dbContext.t_role_scopes
                    .Include(rs => rs.r_scopeTab)
                    .Where(rs => roleIds.Contains(rs.r_role_id_fk) && rs.r_is_delete != true)
                    .Select(rs => rs.r_scopeTab!.r_nom)
                    .Distinct()
                    .ToArrayAsync();

                // Générer le JWT
                JwtIssueOptions _dataJwt = new JwtIssueOptions
                {
                    UserId = user.r_id,
                    UserEmail = user.r_email,
                    Roles = roleCodes,
                    AdminRoles = adminRoleCodes,
                    Scopes = scopes,
                };

                string accessToken = _jwtService.GenerateJwtToken(_dataJwt);

                // Générer le refresh token
                t_refresh_token refreshTokenData = await _jwtService.GenerateRefreshToken(user.r_id);

                // Enregistrer la session
                var session = new t_session
                {
                    r_user_id_fk = user.r_id,
                    r_ip_address = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    r_user_agent = Request.Headers["User-Agent"].ToString(),
                    r_login_at = DateTime.UtcNow,
                    r_is_active = true
                };
                await _dbContext.t_session.AddAsync(session);
                await _dbContext.SaveChangesAsync();

                int expirySeconds = int.TryParse(_configuration["JwtSettings:ExpiryInSecond"], out var sec) ? sec : 3600;
                int refreshExpiry = (int)(refreshTokenData.r_expires_at!.Value - DateTime.UtcNow).TotalSeconds;

                return Ok(new AuthSecurityRetourDto
                {
                    access_token = accessToken,
                    refresh_token = refreshTokenData.r_token,
                    token_type = "Bearer",
                    expires_in = expirySeconds,
                    refresh_expires_in = refreshExpiry > 0 ? refreshExpiry : 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// POST api/auth/refresh-token
        /// Rafraîchir le token d'accès.
        /// </summary>
        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RafraichirLeToken([FromBody] QueryRefreshToken _body)
        {
            string _desc_route = "Rafraîchir le token";
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

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        instance: HttpContext.Request.Path,
                        detail: "Les données ne sont pas conformes",
                        invalidParams: invalidParams));
                }

                t_user dataUser = GetInfoUser();
                if (dataUser == null)
                    return NotFound(GeneraleRetour.BuildNotFound(
                        detail: "L'utilisateur n'existe pas dans notre système",
                        instance: HttpContext.Request.Path));

                // Vérifier le refresh token en base
                var existingRefresh = await _dbContext.t_refresh_token
                    .FirstOrDefaultAsync(rt => rt.r_token == _body.refresh_token
                                            && rt.r_user_id_fk == dataUser.r_id
                                            && rt.r_is_revoked == false
                                            && rt.r_is_delete != true);

                if (existingRefresh == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Refresh token invalide",
                        instance: HttpContext.Request.Path));

                if (existingRefresh.r_expires_at < DateTime.UtcNow)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Refresh token expiré",
                        instance: HttpContext.Request.Path));

                // Révoquer l'ancien refresh token
                existingRefresh.r_is_revoked = true;
                existingRefresh.r_updated_at = DateTime.UtcNow;

                // Charger les rôles et scopes
                var userRoles = await _dbContext.t_user_roles
                    .Include(ur => ur.r_roleTab)
                    .Where(ur => ur.r_user_id_fk == dataUser.r_id && ur.r_is_delete != true && ur.r_roleTab.r_is_delete != true)
                    .ToListAsync();

                string[] roleCodes = userRoles.Select(ur => ur.r_roleTab!.r_code).ToArray();
                string[] adminRoleCodes = userRoles.Where(ur => ur.r_is_admin).Select(ur => ur.r_roleTab!.r_code).ToArray();
                int[] roleIds = userRoles.Select(ur => ur.r_role_id_fk).ToArray();

                var scopes = await _dbContext.t_role_scopes
                    .Include(rs => rs.r_scopeTab)
                    .Where(rs => roleIds.Contains(rs.r_role_id_fk) && rs.r_is_delete != true)
                    .Select(rs => rs.r_scopeTab!.r_nom)
                    .Distinct()
                    .ToArrayAsync();

                // Générer un nouveau JWT et refresh token
                var newAccessToken = _jwtService.GenerateJwtToken(new JwtIssueOptions
                {
                    UserId = dataUser.r_id,
                    UserEmail = dataUser.r_email,
                    Roles = roleCodes,
                    AdminRoles = adminRoleCodes,
                    Scopes = scopes,
                });

                var newRefreshToken = await _jwtService.GenerateRefreshToken(dataUser.r_id);
                newRefreshToken.r_replaced_by = existingRefresh.r_token;

                _dbContext.t_refresh_token.Update(existingRefresh);
                await _dbContext.SaveChangesAsync();

                int expirySeconds = int.TryParse(_configuration["JwtSettings:ExpiryInSecond"], out var secR) ? secR : 3600;
                int refreshExpiry = (int)(newRefreshToken.r_expires_at!.Value - DateTime.UtcNow).TotalSeconds;

                return Ok(new AuthSecurityRetourDto
                {
                    access_token = newAccessToken,
                    refresh_token = newRefreshToken.r_token,
                    token_type = "Bearer",
                    expires_in = expirySeconds,
                    refresh_expires_in = refreshExpiry > 0 ? refreshExpiry : 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// PUT api/auth/password
        /// Modifier le mot de passe de l'utilisateur connecté.
        /// </summary>
        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ModifierLeMotPasse([FromBody] UpdatePasswordClientDto _body)
        {
            string _desc_route = "Modification du mot de passe";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {
                var validator = new UpdatePasswordClientDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams));
                }

                t_user dataUser = GetInfoUser();
                if (dataUser == null)
                    return NotFound(GeneraleRetour.BuildNotFound(
                        detail: "L'utilisateur n'existe pas dans notre système",
                        instance: HttpContext.Request.Path));

                if (string.IsNullOrEmpty(dataUser.r_password) || !BCrypt.Net.BCrypt.Verify(_body.old_password, dataUser.r_password))
                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "L'ancien mot de passe est incorrect",
                        instance: HttpContext.Request.Path));

                dataUser.r_password = BCrypt.Net.BCrypt.HashPassword(_body.new_password);
                dataUser.r_updated_at = DateTime.Now;
                _dbContext.t_user.Update(dataUser);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Modification du mot de passe effectuée avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// POST api/auth/password/reset/otp
        /// Envoyer un OTP pour la réinitialisation du mot de passe.
        /// </summary>
        [HttpPost("password/reset/otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ReinitalisationMotDePasse([FromBody] InitPasswordClientDto _body)
        {
            string _desc_route = "Réinitialisation du mot de passe";
            string IdDemande = RecupererIdDemandeEnCours();

            try
            {
                if (string.IsNullOrWhiteSpace(_body?.identifiant))
                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "L'identifiant est requis",
                        instance: HttpContext.Request.Path));

                var user = await _dbContext.t_user
                    .FirstOrDefaultAsync(p => p.r_email == _body.identifiant && p.r_is_delete != true);

                if (user == null)
                    return NotFound(GeneraleRetour.BuildNotFound(
                        detail: "L'utilisateur est introuvable dans le système",
                        instance: HttpContext.Request.Path));

                var o = await _otpRepo.genererOtp(user.r_id, user.r_id.ToString(), TYPE_OTP.RESET_PASSWORD, _paramdata.sms.validite_otp ?? 6);

                return Ok(new
                {
                    message = "OTP envoyé avec succès pour la réinitialisation du mot de passe.",
                    challengeId = o.challengeId,
                    contactMasked = Tools.Tools.MaskPhone(user.r_telephone),
                    emailMasked = Tools.Tools.MaskEmail(user.r_email)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// POST api/auth/password/reset/otp/confirm
        /// Confirmer la réinitialisation du mot de passe via OTP.
        /// </summary>
        [HttpPost("password/reset/otp/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmReinitialisationMotDePasse([FromBody] QueryConfirmationOtpResetPwdDto _body)
        {
            string _desc_route = "Confirmation de la réinitialisation du mot de passe";
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

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams));
                }

                (int res_otp, var o) = await _otpRepo.verifieOtpAndChallenge(_body.otp, TYPE_OTP.RESET_PASSWORD, _body.challenge);

                switch (res_otp)
                {
                    case 0:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Expiré", instance: HttpContext.Request.Path));
                    case -1:
                        return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));
                    case 1:
                        break;
                }

                var user = await _dbContext.t_user
                    .FirstOrDefaultAsync(p => p.r_id.ToString() == o.idOperationParent && p.r_is_delete != true);

                if (user == null)
                    return StatusCode(403, GeneraleRetour.BuildForbid(detail: "OTP Invalide", instance: HttpContext.Request.Path));

                user.r_password = BCrypt.Net.BCrypt.HashPassword(_body.new_password);
                user.r_updated_at = DateTime.Now;
                _dbContext.t_user.Update(user);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Mot de passe modifié avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// POST api/auth/logout
        /// Déconnexion de l'utilisateur.
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Deconnexion([FromBody] LogoutDto _body)
        {
            string _desc_route = "Déconnexion";

            try
            {
                var validator = new LogoutDtoValidator();
                var results = validator.Validate(_body);

                if (!results.IsValid)
                {
                    var invalidParams = results.Errors.Select(error => new InvalidParam
                    {
                        name = error.PropertyName,
                        reason = error.ErrorMessage
                    }).ToList();

                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "Les données ne sont pas conformes",
                        instance: HttpContext.Request.Path,
                        invalidParams: invalidParams));
                }

                t_user dataUser = GetInfoUser();
                if (dataUser == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Utilisateur non authentifié",
                        instance: HttpContext.Request.Path));

                // Révoquer le refresh token en base
                var refreshToken = await _dbContext.t_refresh_token
                    .FirstOrDefaultAsync(rt => rt.r_token == _body.refresh_token
                                            && rt.r_user_id_fk == dataUser.r_id
                                            && rt.r_is_revoked == false);

                if (refreshToken != null)
                {
                    refreshToken.r_is_revoked = true;
                    refreshToken.r_updated_at = DateTime.Now;
                    _dbContext.t_refresh_token.Update(refreshToken);
                }

                // Clôturer la session active
                var session = await _dbContext.t_session
                    .Where(s => s.r_user_id_fk == dataUser.r_id && s.r_is_active)
                    .OrderByDescending(s => s.r_login_at)
                    .FirstOrDefaultAsync();

                if (session != null)
                {
                    session.r_is_active = false;
                    session.r_logout_at = DateTime.Now;
                    _dbContext.t_session.Update(session);
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Déconnexion effectuée avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        /// <summary>
        /// GET api/auth/me
        /// Récupérer les informations de l'utilisateur connecté.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetProfil()
        {
            string _desc_route = "Profil utilisateur";

            try
            {
                t_user dataUser = GetInfoUser();
                if (dataUser == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Utilisateur non authentifié",
                        instance: HttpContext.Request.Path));

                return Ok(new
                {
                    id = dataUser.r_id,
                    nom = dataUser.r_nom,
                    prenom = dataUser.r_prenom,
                    email = dataUser.r_email,
                    telephone = dataUser.r_telephone,
                    photo = dataUser.r_photo,
                    is_active = dataUser.r_is_active
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [Authorize]
        [HttpPost("photos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile? file)
        {
            const string _desc_route = "Modifier la photo de profil";

            try
            {


               
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
                var user = GetInfoUser();
               
             
                // Création du dossier si besoin
                string photoFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photo");
                if (!Directory.Exists(photoFolder))
                    Directory.CreateDirectory(photoFolder);

                // Enregistrement du fichier
                string fileName = user.r_code + extension;
                string filePath = Path.Combine(photoFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string publicUrl = GetPublicUrl("photo", fileName);

                _dbContext.t_user.Update(user);
            
                await _dbContext.SaveChangesAsync();

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











    }
}
