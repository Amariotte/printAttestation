using System.Data;
using ask.ContextDb;
using ask.Dtos;
using ask.Dtos.General;
using ask.Dtos.Request.auth;
using ask.Dtos.Request.Auth;
using ask.Dtos.Response.auth;
using ask.Interface;
using ask.Model;
using ask.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly IUserRepo _userRepo;
        private readonly ParamMessage _paramdata;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            askContext dbContext,
            IDbContextFactory<askContext> dbFactory,
            JwtService jwtService,
            ServiceMessagerie serviceMessagerie,
            IUserRepo userRepo,
            Microsoft.Extensions.Options.IOptions<ParamMessage> paramdata,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _dbFactory = dbFactory;
            _jwtService = jwtService;
            _serviceMessagerie = serviceMessagerie;
            _userRepo = userRepo;
            _paramdata = paramdata.Value;
            _logger = logger;
            _configuration = configuration;
        }


        #region Helpers

    

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
        public async Task<IActionResult> Inscription([FromBody] UserDto _body)
        {
            string _desc_route = "Inscription";

            try
            {
                var validator = new UserDtoValidator();
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
                        new GeneraleRetour { 
                            status = 409, 
                            detail = "Un compte existe déjà avec cette adresse email. Veuillez utiliser une autre adresse ou vous connecter."
                        },
                        instance: HttpContext.Request.Path));


                string myPass = Tools.Tools.GeneratePassword();

                var user = new t_user
                {
                    r_nom = _body.nom,
                    r_prenom = _body.prenom,
                    r_email = _body.email,
                    r_telephone = _body.telephone,
                    r_statut = STATUT_USER.ACTIVE,
                    r_password_change_required = true,
                    r_password = BCrypt.Net.BCrypt.HashPassword(myPass)
                };



                await _dbContext.t_user.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                await _serviceMessagerie.sendMessageALUtilisateur(TYPE_MODELE.REGISTER_SUCCESS, user, myPass);


                var response = new InscriptionResponseDto
                {
                    message = $"Un e-mail a été envoyé avec succès à l'adresse {Tools.Tools.MaskEmail(user.r_email)}. Veuillez consulter votre boîte de réception pour poursuivre la procédure.",
                    emailMasked = Tools.Tools.MaskEmail(user.r_email),
                };

                return Ok(response);

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

       

                // Générer le JWT
                JwtIssueOptions _dataJwt = new JwtIssueOptions
                {
                    UserId = user.r_id,
                    UserEmail = user.r_email,
                    Roles = [user.r_type.ToString()]
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
                int refreshExpiry = (int)(refreshTokenData.r_expires_at - DateTime.UtcNow).TotalSeconds;

                return Ok(new AuthSecurityRetourDto
                {
                    access_token = accessToken,
                    refresh_token = refreshTokenData.r_token,
                    token_type = "Bearer",
                    expires_in = expirySeconds,
                    refresh_expires_in = refreshExpiry > 0 ? refreshExpiry : 0,
                    password_change_required = user.r_password_change_required,
                    user = Tools.Tools.BuildUserToUserResponseDto(user),
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


                // Générer un nouveau JWT et refresh token
                var newAccessToken = _jwtService.GenerateJwtToken(new JwtIssueOptions
                {
                    UserId = dataUser.r_id,
                    UserEmail = dataUser.r_email,
                    Roles = [dataUser.r_type.ToString()]
                });

                var newRefreshToken = await _jwtService.GenerateRefreshToken(dataUser.r_id);
                newRefreshToken.r_replaced_by = existingRefresh.r_token;

                _dbContext.t_refresh_token.Update(existingRefresh);
                await _dbContext.SaveChangesAsync();

                int expirySeconds = int.TryParse(_configuration["JwtSettings:ExpiryInSecond"], out var secR) ? secR : 3600;
                int refreshExpiry = (int)(newRefreshToken.r_expires_at - DateTime.UtcNow).TotalSeconds;

                return Ok(new AuthSecurityRetourDto
                {
                    access_token = newAccessToken,
                    refresh_token = newRefreshToken?.r_token,
                    token_type = "Bearer",
                    expires_in = expirySeconds,
                    refresh_expires_in = refreshExpiry > 0 ? refreshExpiry : 0,
                    password_change_required = dataUser?.r_password_change_required ?? false,
                    user = Tools.Tools.BuildUserToUserResponseDto(dataUser),
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
        public async Task<IActionResult> ModifierLeMotPasse([FromBody] UpdatePasswordUserDto _body)
        {
            string _desc_route = "Modification du mot de passe";

            try
            {
                var validator = new UpdatePasswordUserDtoValidator();
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
                dataUser.r_updated_at = DateTime.UtcNow;
                dataUser.r_password_change_required = false;

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
        [HttpPost("password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ReinitalisationMotDePasse([FromBody] InitPasswordUserDto _body)
        {
            string _desc_route = "Réinitialisation du mot de passe";

            try
            {
                if (string.IsNullOrWhiteSpace(_body?.email))
                    return BadRequest(GeneraleRetour.BuildBadRequest(
                        detail: "L'email est requis",
                        instance: HttpContext.Request.Path));

                var user = await _dbContext.t_user
                    .FirstOrDefaultAsync(p => p.r_email == _body.email && p.r_is_delete != true);

                if (user == null)
                    return NotFound(GeneraleRetour.BuildNotFound(
                        detail: "L'email est introuvable dans le système",
                        instance: HttpContext.Request.Path));


                string myPass = Tools.Tools.GeneratePassword();

                user.r_password = BCrypt.Net.BCrypt.HashPassword(myPass);
                user.r_password_change_required = true;
                _dbContext.t_user.Update(user);
                await _dbContext.SaveChangesAsync();

                await _serviceMessagerie.sendMessageALUtilisateur(TYPE_MODELE.RESET_PASSWORD, user, myPass);

                var response = new InscriptionResponseDto
                {
                    message = $"Un e-mail a été envoyé avec succès à l'adresse {Tools.Tools.MaskEmail(user.r_email)}. Veuillez consulter votre boîte de réception pour poursuivre la procédure.",
                    emailMasked = Tools.Tools.MaskEmail(user.r_email),
                };

                return Ok(response);

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
                    refreshToken.r_updated_at = DateTime.UtcNow;
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
                    session.r_logout_at = DateTime.UtcNow;
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
                return Ok(Tools.Tools.BuildUserToUserResponseDto(dataUser));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


    }
}
