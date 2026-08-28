using System.Data;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Reponses;
using ask.Dtos.Response;
using ask.Dtos.Response.auth;
using ask.Model;
using ask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using print_attestation.Dtos.Reponses;
using print_attestation.Dtos.Request;
using print_attestation.Dtos.Response;

namespace ask.Controllers
{
    [Route("api/[controller]")]
    public class adminController : ControllerBase
    {
        private readonly askContext _dbContext;
        private readonly ILogger<adminController> _logger;
        private readonly ServiceMessagerie _serviceMessagerie;
        private readonly ParamAppSettings _param_app_settings;
        private readonly TraceService _traceService;


        public adminController(askContext dbContext, ILogger<adminController> logger, ServiceMessagerie serviceMessagerie, IOptions<ParamAppSettings> param_app_settings, TraceService traceService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _serviceMessagerie = serviceMessagerie;
            _param_app_settings = param_app_settings.Value;
            _traceService = traceService;

        }

        [NonAction]
        public t_user? GetInfoUser()
        {
            if (HttpContext.Items.ContainsKey("User"))
                return (t_user)HttpContext.Items["User"];
            return null;
        }

        [NonAction]
        private static int ComputeEvolution(int currentValue, int previousValue)
        {
            if (previousValue <= 0)
                return currentValue > 0 ? 100 : 0;

            return (int)Math.Round(((currentValue - previousValue) / (double)previousValue) * 100);
        }

        [Authorize]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int user = 0, [FromQuery] int site = 0)
        {
            var userInfo = GetInfoUser();
            if (userInfo == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            IQueryable<t_user> usersQuery = _dbContext.t_user.AsQueryable();

            switch (userInfo.r_type)
            {
                case TYPE_UTILISATEUR.Administrateur:
                    break;
                case TYPE_UTILISATEUR.Responsable_Reseau:
                    usersQuery = usersQuery.Where(u => u.r_type > TYPE_UTILISATEUR.Responsable_Reseau);
                    break;
                case TYPE_UTILISATEUR.Responsable_site:
                    usersQuery = usersQuery.Where(u => u.r_site_id_fk == userInfo.r_site_id_fk);
                    break;
                default:
                    usersQuery = usersQuery.Where(u => u.r_id == userInfo.r_id);
                    break;
            }

            if (site > 0)
                usersQuery = usersQuery.Where(u => u.r_site_id_fk == site);

            if (user > 0)
                usersQuery = usersQuery.Where(u => u.r_id == user);

            var scopedUserIds = usersQuery.Select(u => u.r_id);

            var jobsQuery = _dbContext.t_job.Where(j => scopedUserIds.Contains(j.r_user_id_fk));
            var actionsQuery = _dbContext.t_trace_action.Where(a => a.r_user_id.HasValue && scopedUserIds.Contains(a.r_user_id.Value));
            var connexionsQuery = _dbContext.t_trace_connexion.Where(c => c.r_user_id.HasValue && scopedUserIds.Contains(c.r_user_id.Value));

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = monthStart.AddMonths(-1);

            var jobsTotal = await jobsQuery.CountAsync();
            var jobsSuccess = await jobsQuery.CountAsync(j => j.r_status == STATUT_JOB.COMPLETED);
            var jobsFailed = await jobsQuery.CountAsync(j => j.r_status == STATUT_JOB.FAILED);
            var jobsPending = await jobsQuery.CountAsync(j => j.r_status == STATUT_JOB.RUNNING);
            var jobsCancelled = await jobsQuery.CountAsync(j => j.r_status == STATUT_JOB.CANCELLED);

            var jobsThisMonth = await jobsQuery.CountAsync(j => j.r_created_at >= monthStart);
            var jobsPreviousMonth = await jobsQuery.CountAsync(j => j.r_created_at >= previousMonthStart && j.r_created_at < monthStart);

            var downloadActions = new[]
            {
                TYPE_ACTION.TELECHARGEMENT_ATD.ToString(),
                TYPE_ACTION.TELECHARGEMENT_CEDEAO.ToString(),
                TYPE_ACTION.TELECHARGEMENT_ZIP.ToString()
            };

            var downloadTotal = await actionsQuery.CountAsync(a => downloadActions.Contains(a.r_type_action));
            var downloadMonth = await actionsQuery.CountAsync(a => downloadActions.Contains(a.r_type_action) && a.r_created_at >= monthStart);
            var downloadPreviousMonth = await actionsQuery.CountAsync(a => downloadActions.Contains(a.r_type_action) && a.r_created_at >= previousMonthStart && a.r_created_at < monthStart);

            var searchTotal = await actionsQuery.CountAsync(a => a.r_type_action == TYPE_ACTION.RECHERCHE_ATTESTATION.ToString());
            var searchMonth = await actionsQuery.CountAsync(a => a.r_type_action == TYPE_ACTION.RECHERCHE_ATTESTATION.ToString() && a.r_created_at >= monthStart);
            var searchPreviousMonth = await actionsQuery.CountAsync(a => a.r_type_action == TYPE_ACTION.RECHERCHE_ATTESTATION.ToString() && a.r_created_at >= previousMonthStart && a.r_created_at < monthStart);

            var usersTotal = await usersQuery.CountAsync();
            var usersActive = await usersQuery.CountAsync(u => u.r_statut == STATUT_USER.ACTIVE);

            var connexionsThisMonth = await connexionsQuery.CountAsync(c => c.r_created_at >= monthStart);
            var connexionsPreviousMonth = await connexionsQuery.CountAsync(c => c.r_created_at >= previousMonthStart && c.r_created_at < monthStart);

            var dashboard = new dashboardStatsDto
            {
                croissance = ComputeEvolution(jobsThisMonth, jobsPreviousMonth),
                telechargements = new downloadStatsDto
                {
                    total = downloadTotal,
                    mois = downloadMonth
                },
                search = new searchStatsDto
                {
                    total = searchTotal,
                    previousMois = searchPreviousMonth,
                    mois = searchMonth,
                    croissance = ComputeEvolution(searchMonth, searchPreviousMonth)
                },
                users = new usersStatsDto
                {
                    total = usersTotal,
                    actives = usersActive
                },
                jobs = new jobStatsDto
                {
                    total = jobsTotal,
                    succes = jobsSuccess,
                    failed = jobsFailed,
                    cancelled = jobsCancelled,
                    pending = jobsPending,
                    mois = jobsThisMonth,
                    previousMois = jobsPreviousMonth,
                    croissance = ComputeEvolution(jobsThisMonth, jobsPreviousMonth)
                },
                attestationsEvolution = ComputeEvolution(jobsThisMonth, jobsPreviousMonth),
                totalEvolution = ComputeEvolution(downloadMonth, downloadPreviousMonth),
                totalEvconnexionsolution = ComputeEvolution(connexionsThisMonth, connexionsPreviousMonth)
            };

            return Ok(dashboard);
        }




        [Authorize]
        [HttpGet("dashboard/evolutions")]
        public async Task<IActionResult> GetEvolutions(
            [FromQuery] int? annee = null,
            [FromQuery] int user = 0,
            [FromQuery] int site = 0)
        {
            var userInfo = GetInfoUser();
            if (userInfo == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            // Périmètre selon le type d'utilisateur connecté
            IQueryable<t_user> usersQuery = _dbContext.t_user.AsQueryable();

            switch (userInfo.r_type)
            {
                case TYPE_UTILISATEUR.Administrateur:
                    break;
                case TYPE_UTILISATEUR.Responsable_Reseau:
                    usersQuery = usersQuery.Where(u => u.r_type > TYPE_UTILISATEUR.Responsable_Reseau);
                    break;
                case TYPE_UTILISATEUR.Responsable_site:
                    usersQuery = usersQuery.Where(u => u.r_site_id_fk == userInfo.r_site_id_fk);
                    break;
                default:
                    usersQuery = usersQuery.Where(u => u.r_id == userInfo.r_id);
                    break;
            }

            if (site > 0)
                usersQuery = usersQuery.Where(u => u.r_site_id_fk == site);

            if (user > 0)
                usersQuery = usersQuery.Where(u => u.r_id == user);

            var scopedUserIds = usersQuery.Select(u => u.r_id);

            // Détermination de la plage temporelle
            var now = DateTime.UtcNow;
            int targetYear;
            DateTime periodeDebut;
            DateTime periodeFin;

            if (annee.HasValue && annee.Value > 0)
            {
                // Année spécifique : 12 mois de janvier à décembre
                targetYear = annee.Value;
                periodeDebut = new DateTime(targetYear, 1, 1);
                periodeFin = periodeDebut.AddYears(1);
            }
            else
            {
                // 12 derniers mois glissants
                periodeDebut = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
                periodeFin = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                targetYear = 0; // mode glissant
            }

            // Chargement groupé des données en mémoire pour éviter N+1
            var searchActionStr = TYPE_ACTION.RECHERCHE_ATTESTATION.ToString();

            var jobsParMois = await _dbContext.t_job
                .Where(j => scopedUserIds.Contains(j.r_user_id_fk)
                         && j.r_created_at >= periodeDebut
                         && j.r_created_at < periodeFin)
                .GroupBy(j => new { j.r_created_at!.Value.Year, j.r_created_at!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var recherchesParMois = await _dbContext.t_trace_action
                .Where(a => a.r_user_id.HasValue
                         && scopedUserIds.Contains(a.r_user_id.Value)
                         && a.r_type_action == searchActionStr
                         && a.r_created_at >= periodeDebut
                         && a.r_created_at < periodeFin)
                .GroupBy(a => new { a.r_created_at!.Value.Year, a.r_created_at!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var annulationsParMois = await _dbContext.t_demande_annulation
                .Where(d => scopedUserIds.Contains(d.r_user_id_fk)
                         && d.r_created_at >= periodeDebut
                         && d.r_created_at < periodeFin)
                .GroupBy(d => new { d.r_created_at!.Value.Year, d.r_created_at!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // Noms des mois en français
            var nomsMois = new[]
            {
                "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
            };

            // Construction de la liste des 12 mois
            var moisList = new List<evolutionMoisDto>();

            for (int i = 0; i < 12; i++)
            {
                DateTime moisCourant = periodeDebut.AddMonths(i);
                int m = moisCourant.Month;
                int y = moisCourant.Year;

                moisList.Add(new evolutionMoisDto
                {
                    numero = i,
                    nom = $"{nomsMois[m - 1]} {y}",
                    taches = jobsParMois.FirstOrDefault(x => x.Year == y && x.Month == m)?.Count ?? 0,
                    recherches = recherchesParMois.FirstOrDefault(x => x.Year == y && x.Month == m)?.Count ?? 0,
                    demandesAnnulation = annulationsParMois.FirstOrDefault(x => x.Year == y && x.Month == m)?.Count ?? 0
                });
            }

            var resultat = new evolutionMensuelleDto
            {
                recherches = moisList.Sum(x => x.recherches),
                taches = moisList.Sum(x => x.taches),
                demandes = moisList.Sum(x => x.demandesAnnulation),
                mois = moisList
            };

            return Ok(resultat);
        }





        [Authorize]
        [HttpGet("audits/actions")]
        public async Task<IActionResult> GeLog([FromQuery] int page = 1, [FromQuery] int limit = 10 , [FromQuery] string action = "", [FromQuery] string search = "")
        {
            const string _desc_route = "Liste des logs";

            try
            {

                var pagination = new PaginationParams(page, limit);

                var baseQuery = _dbContext.t_trace_action
                 .Where(e => e.r_is_delete != true);


                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToUpper().Trim();

                    baseQuery = baseQuery.Where(x =>
                        x.r_ip_address.ToUpper().Contains(search) ||
                        x.r_type_action.ToUpper().Contains(search) ||
                        x.r_description.ToUpper().Contains(search) ||
                        x.r_user.r_nom.ToUpper().Contains(search) ||
                        x.r_user.r_prenom.ToUpper().Contains(search) ||
                        (x.r_user.r_nom + ' '+x.r_user.r_prenom).ToUpper().Contains(search) ||
                        x.r_user.r_email.ToUpper().Contains(search)
                    );
                }

                if (!string.IsNullOrWhiteSpace(action))
                {
                    action = action.ToUpper().Trim();
                    baseQuery = baseQuery.Where(x => x.r_type_action.ToUpper().Contains(action));
                }

                // total avant pagination
                var total = await baseQuery.CountAsync();

                var logs = await baseQuery
                    .Include(u => u.r_user)
                    .OrderByDescending(u => u.r_id)
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

        [Authorize]
        [HttpGet("audits/acces")]
        public async Task<IActionResult> GeLogAcces([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "" , [FromQuery] string action = "")
        {
            const string _desc_route = "Liste des logs";

            try
            {

                var pagination = new PaginationParams(page, limit);

                var baseQuery = _dbContext.t_trace_connexion
                 .Where(e => e.r_is_delete != true);


                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();

                    baseQuery = baseQuery.Where(x =>
                        x.r_ip_address.ToUpper().Contains(search) ||
                        x.r_type_evenement.ToUpper().Contains(search) ||
                        x.r_user.r_nom.ToUpper().Contains(search) ||
                        (x.r_user.r_nom +' '+ x.r_user.r_prenom).ToUpper().Contains(search) ||
                        x.r_user.r_prenom.ToUpper().Contains(search) ||
                        x.r_user.r_email.ToUpper().Contains(search)
                    );
                }

                if (!string.IsNullOrWhiteSpace(action))
                {
                    action = action.ToUpper().Trim();
                    baseQuery = baseQuery.Where(x => x.r_type_evenement.ToUpper().Contains(action));
                }


                // total avant pagination
                var total = await baseQuery.CountAsync();

                var logs = await baseQuery
                    .Include(u => u.r_user)
                    .OrderByDescending(u => u.r_id)
                    .Skip((pagination.Skip))
                    .Take(pagination.Take)
                    .ToListAsync();

                var logsDto = logs.Select(m => new logAccesDto
                {
                    id = m.r_id,
                    userId = m.r_user_id,
                    date = m.r_created_at,
                    userEmail = m.r_email,
                    typeEvenement = m.r_type_evenement,
                    detailJson = m.r_details_json,
                    ip = m.r_ip_address,
                    userAgent = m.r_user_agent,
                    success = m.r_succes,
                    raisonEchec = m.r_raison_echec,
                    user = Tools.Tools.BuildUserToUserResponseDto(m.r_user),
                }).ToList();


                return Ok(PaginatedResponse<logAccesDto>.Create(logsDto, total, page, limit));

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
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "", [FromQuery] int status = 0)
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


                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToUpper().Trim();

                    baseQuery = baseQuery.Where(x =>
                        x.r_nom.ToUpper().Contains(search) ||
                        (x.r_nom+' ' + x.r_prenom).ToUpper().Contains(search) ||
                        x.r_email.ToUpper().Contains(search) ||
                        x.r_telephone.ToUpper().Contains(search) ||
                        x.r_prenom.ToUpper().Contains(search) ||
                        x.r_site.r_nom.ToUpper().Contains(search)
                    );
                }

                if (status > 0)
                {
                    baseQuery = baseQuery.Where(x => (int)x.r_statut == status);
                }


                // Filtrage selon le type de l'utilisateur connecté
                switch (userConnecte.r_type)
                {
                    case TYPE_UTILISATEUR.Administrateur:

                        // Aucun filtre : voit tout
                        break;

                    case TYPE_UTILISATEUR.Responsable_Reseau:
                        baseQuery = baseQuery.Where(u => u.r_type > TYPE_UTILISATEUR.Responsable_Reseau);
                        break;
                    case TYPE_UTILISATEUR.Responsable_site:
                        baseQuery = baseQuery.Where(u => u.r_site_id_fk == userConnecte.r_site_id_fk);
                        break;

                    case TYPE_UTILISATEUR.Utilisateur:
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

                _logger.LogError($"[EndPoint _body] ===============================>{JsonConvert.SerializeObject(_body)}");



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


                _logger.LogError($"[EndPoint user] ===============================>{JsonConvert.SerializeObject(user)}");

                await _dbContext.t_user.AddAsync(user);
                await _dbContext.SaveChangesAsync();

           
                await _traceService.TraceActionAsync(TYPE_ACTION.CREER_UTILISATEUR,description: $"Création de l'utilisateur : {user.r_email}");


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
        public async Task<IActionResult> ModifierUnUtilisateur(int id, [FromBody] UserDto _body)
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

                await _traceService.TraceActionAsync(TYPE_ACTION.MODIFIER_UTILISATEUR, description: $"Modification de l'utilisateur : {User.r_email}");

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


                await _traceService.TraceActionAsync(TYPE_ACTION.DESACTIVER_UTILISATEUR, description: $"Modification de l'utilisateur : {resQuery.r_email}");


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
                    
                    await _traceService.TraceActionAsync(TYPE_ACTION.ACTIVER_UTILISATEUR, description: $"Activation de l'utilisateur : {resQuery.r_email}");

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

       
                await _traceService.TraceActionAsync(TYPE_ACTION.REINITIALISATION_UTILISATEUR, description: $"Réinitialisation du mot de passe de l'utilisateur : {User.r_email}");


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
        public async Task<IActionResult> GetSites([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            const string _desc_route = "Liste des sites";

            try
            {


                var pagination = new PaginationParams(page, limit);


                var baseQuery = _dbContext.t_site
                    .Where(e => e.r_is_delete != true);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToUpper().Trim();

                    baseQuery = baseQuery.Where(x =>
                        x.r_nom.ToUpper().Contains(search) ||
                        x.r_code.ToUpper().Contains(search)
                    );
                }

                // total avant pagination
                var total = await baseQuery.CountAsync();

                var sites = await baseQuery
                    .OrderBy(u => u.r_nom)
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

                await _traceService.TraceActionAsync(TYPE_ACTION.CREATION_SITE,description: $"Création d'un site : {_body.nom}");

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
                await _traceService.TraceActionAsync(TYPE_ACTION.MODIFICATION_SITE, description: $"Modification d'un site : {_body.nom}");

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

                await _traceService.TraceActionAsync(TYPE_ACTION.SUPPRESSION_SITE, description: $"Suppression du site : {resQuery.r_nom}");

                return Ok(Tools.Tools.BuildSiteToSiteResponseDto(resQuery));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion





        #region ========================= MOTIFS D'ANNULATION =========================
        [Authorize]
        [HttpGet("motifs-annulation")]
        public async Task<IActionResult> GetMotifsAnnulation([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            const string _desc_route = "Liste des motifs d'annulation";

            try
            {


                var pagination = new PaginationParams(page, limit);


                var baseQuery = _dbContext.t_motif_annulation
                    .Where(e => e.r_is_delete != true);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToUpper().Trim();

                    baseQuery = baseQuery.Where(x =>
                        x.r_libelle.ToUpper().Contains(search) 
                    );
                }

                // total avant pagination
                var total = await baseQuery.CountAsync();

                var motifs = await baseQuery
                    .OrderBy(u => u.r_libelle)
                    .Skip((pagination.Skip))
                    .Take(pagination.Take)
                    .ToListAsync();


                var motifDto = motifs.Select(m => Tools.Tools.BuildMotifAnnulationToMotifAnnulationResponseDto(m)).ToList();

                return base.Ok(PaginatedResponse<MotifAnnulationResponseDto>.Create((List<MotifAnnulationResponseDto>)motifDto, total, page, limit));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpPost("motifs-annulation")]
        public async Task<IActionResult> CréerUnMotifAnnulation([FromBody] MotifAnnulationDto _body)
        {
            const string _desc_route = "Créer un motif d'annulation";

            try
            {


                var validator = new MotifAnnulationDtoValidator();
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

                var existingMotif = await _dbContext.t_motif_annulation
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.r_libelle == _body.libelle && u.r_is_delete != true);

                if (existingMotif != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Un site existe déjà avec ce code. Veuillez utiliser un autre code."
                        },
                        instance: HttpContext.Request.Path));


                var motif = new t_motif_annulation
                {
                    r_libelle = _body.libelle,
                };


                await _dbContext.t_motif_annulation.AddAsync(motif);
                await _dbContext.SaveChangesAsync();

                await _traceService.TraceActionAsync(TYPE_ACTION.CREATION_MOTIF_ANNULATION, description: $"Création d'un motif d'annulation : {_body.libelle}");

                return Ok(Tools.Tools.BuildMotifAnnulationToMotifAnnulationResponseDto(motif));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }



        [Authorize]
        [HttpPut("motifs-annulation/{id}")]
        public async Task<IActionResult> ModifierUnMotifAnnulation(int id, [FromBody] MotifAnnulationDto _body)
        {
            const string _desc_route = "Modifier un motif d'annulation";

            try
            {
                var validator = new MotifAnnulationDtoValidator();
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


                var motif = await _dbContext.t_motif_annulation
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.r_id == id && s.r_is_delete != true);


                if (motif == null)
                {
                    return NotFound(GeneraleRetour.BuildNotFound(
                       detail: "Le motif d'annulation est introuvable",
                       instance: HttpContext.Request.Path
                    ));
                }



                var existingMotif = await _dbContext.t_motif_annulation
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.r_libelle == _body.libelle && s.r_is_delete != true && s.r_id != motif.r_id);

                if (existingMotif != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Un motif d'annulation existe déjà avec ce libellé. Veuillez utiliser un autre libellé."
                        },
                        instance: HttpContext.Request.Path));


                motif.r_libelle = _body.libelle;

                _dbContext.t_motif_annulation.Update(motif);
                await _dbContext.SaveChangesAsync();
                await _traceService.TraceActionAsync(TYPE_ACTION.MODIFICATION_MOTIF_ANNULATION, description: $"Modification d'un motif d'annulation : {_body.libelle}");

                return Ok(Tools.Tools.BuildMotifAnnulationToMotifAnnulationResponseDto(motif));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [Authorize]
        [HttpDelete("motifs-annulation/{id}")]
        public async Task<IActionResult> SupprimerUnMotifAnnulation(int id)
        {
            const string _desc_route = "Supprimer un motif d'annulation";

            try
            {
                if (id <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'identifiant du motif d'annulation est manquant", instance: HttpContext.Request.Path));

                var resQuery = await _dbContext.t_motif_annulation
                    .Where(e => e.r_id == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resQuery == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Le motif d'annulation n'existe pas", instance: HttpContext.Request.Path));

                resQuery.r_is_delete = true;
                _dbContext.t_motif_annulation.Update(resQuery);
                await _dbContext.SaveChangesAsync();

                await _traceService.TraceActionAsync(TYPE_ACTION.SUPPRESSION_MOTIF_ANNULATION, description: $"Suppression du motif d'annulation : {resQuery.r_libelle}");

                return Ok(Tools.Tools.BuildMotifAnnulationToMotifAnnulationResponseDto(resQuery));
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
