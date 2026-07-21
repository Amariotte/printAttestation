using System.Data;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Reponses;
using ask.Dtos.Request.auth;
using ask.Dtos.Response;
using ask.Dtos.Response.auth;
using ask.Model;
using ask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ask.Controllers
{
    [Route("api/[controller]")]
    public class adminController : ControllerBase
    {
        private readonly askContext _dbContext;
        private readonly ILogger<adminController> _logger;
        private readonly ServiceMessagerie _serviceMessagerie;
        private readonly ParamAppSettings _param_app_settings;


        public adminController(askContext dbContext, ILogger<adminController> logger, ServiceMessagerie serviceMessagerie,IOptions<ParamAppSettings> param_app_settings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _serviceMessagerie = serviceMessagerie;
            _param_app_settings = param_app_settings.Value;

        }

        [NonAction]
        public t_user? GetInfoUser()
        {
            if (HttpContext.Items.ContainsKey("User"))
                return (t_user)HttpContext.Items["User"];
            return null;
        }


        [Authorize]
        [HttpGet("audits/actions")]
        public async Task<IActionResult> GeLog([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            const string _desc_route = "Liste des logs";

            try
            {

                var pagination = new PaginationParams(page, limit);

                var baseQuery = _dbContext.t_trace_action
                 .Where(e => e.r_is_delete != true);

                // total avant pagination
                var total = await baseQuery.CountAsync();

                var logs = await baseQuery
                    .Include(u => u.r_user)
                    .OrderBy(u => u.r_id)
                    .Skip((pagination.Skip))
                    .Take(pagination.Take)
                    .ToListAsync();

        var logsDto = logs.Select(m => new logDto
                {
                    id = m.r_id,
            userId = m.r_user_id,
            description = m.r_description,
            date = m.r_created_at,
            userEmail = m.r_user_email,
            typeAction = m.r_type_action,
            detailJson = m.r_details_json,
             ip = m.r_ip_address,
            userAgent = m.r_user_agent,
            httpMethod = m.r_http_method,
             endpoint = m.r_endpoint,
            statusCode = m.r_status_code,
            durationMs = m.r_duration_ms,
            user = Tools.Tools.BuildUserToUserResponseDto(m.r_user),
                }).ToList();


                return Ok(PaginatedResponse<logDto>.Create(logsDto, total, page, limit));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        #region ========================= MODELES =========================

        [Authorize]
        [HttpGet("modeles")]
        public async Task<IActionResult> GetModeles()
        {
            const string _desc_route = "Liste des modèles";

            try
            {
                var respQuery = await _dbContext.t_modele
                    .Where(e => e.r_is_delete != true)
                    .OrderBy(e => e.r_description)
                    .ToListAsync();



                var modelesDto = respQuery.Select(m => new ModeleDto
                {
                    id = m.r_id,
                    description = m.r_description,
                    subject = m.r_subject,
                    body = m.r_body,
                    plateforme = m.r_plateforme,
                    type = m.r_type
                }).ToList();

                return Ok(new { data = modelesDto, meta = new { total = modelesDto.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("modeles")]
        public async Task<IActionResult> CreateModele([FromBody] ModeleDto _body)
        {
            const string _desc_route = "Créer un modèle";

            try
            {
                List<InvalidParam> invalidParams = new();

                if (string.IsNullOrEmpty(_body.description))
                    invalidParams.Add(new InvalidParam { name = "description", reason = "La description est requise" });

                if (!_body.plateforme.HasValue)
                    invalidParams.Add(new InvalidParam { name = "plateforme", reason = "La plateforme est requise" });

                if (!_body.type.HasValue)
                    invalidParams.Add(new InvalidParam { name = "type", reason = "Le type est requis" });

                if (invalidParams.Count > 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données ne sont pas conformes", instance: HttpContext.Request.Path, invalidParams: invalidParams));

                t_modele entity = new t_modele
                {
                    r_description = _body.description,
                    r_subject = _body.subject,
                    r_body = _body.body,
                    r_plateforme = _body.plateforme,
                    r_type = _body.type,
                };

                _dbContext.t_modele.Add(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new ModeleDto
                {
                    id = entity.r_id,
                    description = entity.r_description,
                    subject = entity.r_subject,
                    body = entity.r_body,
                    plateforme = entity.r_plateforme,
                    type = entity.r_type,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("modeles/{id}")]
        public async Task<IActionResult> UpdateModele(int id, [FromBody] ModeleDto _body)
        {
            const string _desc_route = "Modifier un modèle";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du modèle est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_modele
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le modèle n'existe pas", instance: HttpContext.Request.Path));

                if (!string.IsNullOrEmpty(_body.description)) resQuery.r_description = _body.description;
                if (!string.IsNullOrEmpty(_body.subject)) resQuery.r_subject = _body.subject;
                if (!string.IsNullOrEmpty(_body.body)) resQuery.r_body = _body.body;
                if (_body.plateforme.HasValue) resQuery.r_plateforme = _body.plateforme;
                if (_body.type.HasValue) resQuery.r_type = _body.type;

                _dbContext.t_modele.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return Ok(new ModeleDto
                {
                    id = resQuery.r_id,
                    description = resQuery.r_description,
                    subject = resQuery.r_subject,
                    body = resQuery.r_body,
                    plateforme = resQuery.r_plateforme,
                    type = resQuery.r_type,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpDelete("modeles/{id}")]
        public async Task<IActionResult> DeleteModele(int id)
        {
            const string _desc_route = "Supprimer un modèle";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du modèle est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_modele
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le modèle n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_modele.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                return StatusCode(204, "Modèle supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion


        #region ========================= UTLISATEURS =========================
        [Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            const string _desc_route = "Liste des utilisateurs";

            try
            {


                t_user userConnecte = GetInfoUser();

                if (userConnecte == null)
                {
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(
                        detail: "Utilisateur non authentifié",
                        instance: HttpContext.Request.Path));
                }

                var pagination = new PaginationParams(page, limit);

                IQueryable<t_user> baseQuery = _dbContext.t_user
                    .Where(u => !u.r_is_delete);

                // Filtrage selon le type de l'utilisateur connecté
                switch (userConnecte.r_type)
                {
                    case TYPE_USER.Administrateur:
                        // Aucun filtre : voit tout
                        break;

                    case TYPE_USER.Responsable_site:
                        baseQuery = baseQuery.Where(u => u.r_site_id_fk == userConnecte.r_site_id_fk);
                        break;

                    case TYPE_USER.Utilisateur:
                        baseQuery = baseQuery.Where(u => u.r_id == userConnecte.r_id);
                        break;

                    default:
                        return StatusCode(403, GeneraleRetour.BuildForbid(
                        detail: "Utilisateur non authentifié",
                        instance: HttpContext.Request.Path));
                }

                var total = await baseQuery.CountAsync();

                var users = await baseQuery
                    .Include(u => u.r_site)
                    .OrderBy(u => u.r_id)
                    .Skip(pagination.Skip)
                    .Take(pagination.Take)
                    .ToListAsync();

                var usersDto = users.Select(m => Tools.Tools.BuildUserToUserResponseDto(m)).ToList();

                return Ok(PaginatedResponse<UserResponseDto>.Create(usersDto, total, page, limit));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPost("users")]
        public async Task<IActionResult> CréerUnUtilisateur([FromBody] UserDto _body)
        {
            const string _desc_route = "Créer un utilisateur";

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
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Un compte existe déjà avec cette adresse email. Veuillez utiliser une autre adresse ou vous connecter."
                        },
                        instance: HttpContext.Request.Path));


                if (_body.siteId != null)
                {
                    var site = await _dbContext.t_site
                        .FirstOrDefaultAsync(s => s.r_id == _body.siteId && s.r_is_delete != true);
                    if (site == null)
                    {
                        return BadRequest(GeneraleRetour.BuildBadRequest(
                            detail: "Le site spécifié est introuvable.",
                            instance: HttpContext.Request.Path));
                    }
                }



                string myPass = _param_app_settings.defaultPassword;

                var user = new t_user
                {
                    r_nom = _body.nom,
                    r_prenom = _body.prenom,
                    r_email = _body.email,
                    r_telephone = _body.telephone,
                    r_type = _body.roleId,
                    r_statut = STATUT_USER.ACTIVE,
                    r_password_change_required = true,
                    r_password = BCrypt.Net.BCrypt.HashPassword(myPass),
                    r_date_last_statut = DateTime.UtcNow,
                    r_site_id_fk = _body.siteId
                };



                await _dbContext.t_user.AddAsync(user);
                await _dbContext.SaveChangesAsync();

            //    await _serviceMessagerie.sendMessageALUtilisateur(TYPE_MODELE.REGISTER_SUCCESS, user, myPass);


                var response = new InscriptionResponseDto
                {
                    message = $"Un e-mail a été envoyé avec succès à l'adresse {Tools.Tools.MaskEmail(user.r_email)}. Veuillez consulter votre boîte de réception pour poursuivre la procédure.",
                    emailMasked = Tools.Tools.MaskEmail(user.r_email),
                    defaultPassword = myPass
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [Authorize]
        [HttpPut("users/{id}")]
        public async Task<IActionResult> ModifierUnUtilisateur(int id,[FromBody] UserDto _body)
        {
            const string _desc_route = "Modifier un utilisateur";

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


                var User = await _dbContext.t_user
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.r_id == id && u.r_is_delete != true);


                if (User == null)
                {
                    return NotFound(GeneraleRetour.BuildNotFound(
                       detail: "L'utilisateur est introuvable",
                       instance: HttpContext.Request.Path
                    ));
                }



                var existingUser = await _dbContext.t_user
                    .Include(u => u.r_site)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.r_email == _body.email && u.r_is_delete != true && u.r_id != User.r_id);

                if (existingUser != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Un compte existe déjà avec cette adresse email. Veuillez utiliser une autre adresse ou vous connecter."
                        },
                        instance: HttpContext.Request.Path));

                if (_body.siteId != null)
                {
                    var site = await _dbContext.t_site
                        .FirstOrDefaultAsync(s => s.r_id == _body.siteId && s.r_is_delete != true);
                    if (site == null)
                    {
                        return BadRequest(GeneraleRetour.BuildBadRequest(
                            detail: "Le site spécifié est introuvable.",
                            instance: HttpContext.Request.Path));
                    }
                }


                User.r_nom = _body.nom;
                User.r_prenom = _body.prenom;
                User.r_email = _body.email;
                User.r_telephone = _body.telephone;
                User.r_type = _body.roleId;
                User.r_site_id_fk = _body.siteId;

                _dbContext.t_user.Update(User);
                await _dbContext.SaveChangesAsync();

                return Ok(Tools.Tools.BuildUserToUserResponseDto(User));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPut("users/{id}/desactivations")]
        public async Task<IActionResult> DesactiverUnUtilisateur(int id)
        {
            const string _desc_route = "Désactiver un utilisateur";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de l'utilisateur est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_user
                    .Include(u => u.r_site)
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'utilisateur n'existe pas", instance: HttpContext.Request.Path));


                if (resQuery.r_statut == STATUT_USER.ACTIVE)
                {
                    resQuery.r_is_active = false;
                    resQuery.r_statut = STATUT_USER.DESACTIVE;
                    resQuery.r_date_last_statut = DateTime.UtcNow;
                    _dbContext.t_user.Update(resQuery);
                    await _dbContext.SaveChangesAsync();

               //     _serviceMessagerie.sendMessageALUtilisateur(TYPE_MODELE.COMPTE_DESACTIVE, resQuery,null);

                }

                
                return Ok(Tools.Tools.BuildUserToUserResponseDto(resQuery));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpPut("users/{id}/activations")]
        public async Task<IActionResult> ActiverUnUtilisateur(int id)
        {
            const string _desc_route = "Activer un utilisateur";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant de l'utilisateur est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_user
                    .Include(u => u.r_site)
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "L'utilisateur n'existe pas", instance: HttpContext.Request.Path));

                if (resQuery.r_statut == STATUT_USER.DESACTIVE)
                {
                    resQuery.r_is_active = true;
                    resQuery.r_statut = STATUT_USER.ACTIVE;
                    resQuery.r_date_last_statut = DateTime.UtcNow;
                    _dbContext.t_user.Update(resQuery);
                    await _dbContext.SaveChangesAsync();


                  //  _serviceMessagerie.sendMessageALUtilisateur(TYPE_MODELE.COMPTE_ACTIVE, resQuery, null);

                }

                return Ok(Tools.Tools.BuildUserToUserResponseDto(resQuery));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion

        [Authorize]
        [HttpPut("users/{id}/reset-password")]
        public async Task<IActionResult> ReinistialiserLeMotDePasse(int id)
        {
            const string _desc_route = "Réinitialiser le mot de passe d'un utilisateur";

            try
            {
                
                var User = await _dbContext.t_user
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.r_id == id && u.r_is_delete != true);

                if (User == null)
                {
                    return NotFound(GeneraleRetour.BuildNotFound(
                       detail: "L'utilisateur est introuvable",
                       instance: HttpContext.Request.Path
                    ));
                }

                string myPass = _param_app_settings.defaultPassword;
                User.r_password = BCrypt.Net.BCrypt.HashPassword(myPass);
                User.r_password_change_required = true;

                _dbContext.t_user.Update(User);
                await _dbContext.SaveChangesAsync();


              //  await _serviceMessagerie.sendMessageALUtilisateur(TYPE_MODELE.RESET_PASSWORD, User, myPass);

                var response = new InscriptionResponseDto
                {
                    message = $"Un e-mail a été envoyé avec succès à l'adresse {Tools.Tools.MaskEmail(User.r_email)}. Veuillez consulter votre boîte de réception pour poursuivre la procédure.",
                    emailMasked = Tools.Tools.MaskEmail(User.r_email),
                    defaultPassword = myPass
                };

                return Ok(response);


            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        #region ========================= SITES =========================
        [Authorize]
        [HttpGet("sites")]
        public async Task<IActionResult> GetSites([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            const string _desc_route = "Liste des sites";

            try
            {


                var pagination = new PaginationParams(page, limit);


                var baseQuery = _dbContext.t_site
                    .Where(e => e.r_is_delete != true);

                // total avant pagination
                var total = await baseQuery.CountAsync();

                var sites = await baseQuery
                    .OrderBy(u => u.r_id)
                    .Skip((pagination.Skip))
                    .Take(pagination.Take)
                    .ToListAsync();

             
                var siteDto = sites.Select(m => Tools.Tools.BuildSiteToSiteResponseDto(m)).ToList();

                return Ok(PaginatedResponse<SiteResponseDto>.Create(siteDto, total, page, limit));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPost("sites")]
        public async Task<IActionResult> CréerUnSite([FromBody] SiteDto _body)
        {
            const string _desc_route = "Créer un site";

            try
            {
                var validator = new SiteDtoValidator();
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

                var existingSite = await _dbContext.t_site
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.r_code == _body.code && u.r_is_delete != true);

                if (existingSite != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Un site existe déjà avec ce code. Veuillez utiliser un autre code."
                        },
                        instance: HttpContext.Request.Path));


                var site = new t_site
                {
                    r_nom = _body.nom,
                    r_code = _body.code,
                };


                await _dbContext.t_site.AddAsync(site);
                await _dbContext.SaveChangesAsync();


                return Ok(Tools.Tools.BuildSiteToSiteResponseDto(site));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [Authorize]
        [HttpPut("sites/{id}")]
        public async Task<IActionResult> ModifierUnSite(int id, [FromBody] SiteDto _body)
        {
            const string _desc_route = "Modifier un site";

            try
            {
                var validator = new SiteDtoValidator();
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


                var site = await _dbContext.t_site
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.r_id == id && s.r_is_delete != true);


                if (site == null)
                {
                    return NotFound(GeneraleRetour.BuildNotFound(
                       detail: "Le site est introuvable",
                       instance: HttpContext.Request.Path
                    ));
                }



                var existingSite = await _dbContext.t_site
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.r_code == _body.code && s.r_is_delete != true && s.r_id != site.r_id);

                if (existingSite != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Un site existe déjà avec ce code. Veuillez utiliser un autre code."
                        },
                        instance: HttpContext.Request.Path));


                string myPass = Tools.Tools.GeneratePassword(includeSpecialChars: false);


                site.r_nom = _body.nom;
                site.r_code = _body.code;

                _dbContext.t_site.Update(site);
                await _dbContext.SaveChangesAsync();

                return Ok(Tools.Tools.BuildSiteToSiteResponseDto(site));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpDelete("sites/{id}")]
        public async Task<IActionResult> SupprimerUnSite(int id)
        {
            const string _desc_route = "Supprimer un site";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du site est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_site
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le site n'existe pas", instance: HttpContext.Request.Path));

                    resQuery.r_is_delete = true;
                    _dbContext.t_site.Update(resQuery);
                    await _dbContext.SaveChangesAsync();

               
                return Ok(Tools.Tools.BuildSiteToSiteResponseDto(resQuery));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion
    }
}
