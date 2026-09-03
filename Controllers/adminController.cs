using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using print_attestation.ContextDb;
using print_attestation.Dtos.General;
using print_attestation.Dtos.Request;
using print_attestation.Dtos.Response;
using print_attestation.Dtos.Response.auth;
using print_attestation.Model;
using print_attestation.ScopeAttribute;
using print_attestation.Security;
using print_attestation.Services;


namespace print_attestation.Controllers
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

        [NonAction]
        private static bool CanAssignUserType(TYPE_UTILISATEUR actorType, TYPE_UTILISATEUR targetType)
        {
            return actorType switch
            {
                TYPE_UTILISATEUR.ADMINISTRATEUR => true,
                TYPE_UTILISATEUR.RESPONSABLE_RESEAU => targetType != TYPE_UTILISATEUR.ADMINISTRATEUR && targetType != TYPE_UTILISATEUR.RESPONSABLE_RESEAU,
                TYPE_UTILISATEUR.BUREAU_DIRECT => targetType == TYPE_UTILISATEUR.UTILISATEUR,
                TYPE_UTILISATEUR.RESPONSABLE_INTERMEDIAIRE => targetType == TYPE_UTILISATEUR.UTILISATEUR,
                TYPE_UTILISATEUR.UTILISATEUR => false,
                _ => false
            };
        }

        [Authorize]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int user = 0, [FromQuery] int site = 0)
        {
            var userInfo = GetInfoUser();
            if (userInfo == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            IQueryable<t_user> usersQuery = _dbContext.t_user.AsQueryable();

          

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
        [HttpGet("dashboard/evolutions/week")]
        public async Task<IActionResult> GetEvolutionsWeek(
            [FromQuery] string periode = "semaine",
            [FromQuery] int? annee = null,
            [FromQuery] int user = 0,
            [FromQuery] int site = 0)
        {
            var userConnecte = GetInfoUser();
            if (userConnecte == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var mode = (periode ?? "semaine").Trim().ToLowerInvariant();
            var now = DateTime.UtcNow.Date;

            DateTime debutPeriode;
            DateTime finPeriode;

            switch (mode)
            {
                case "7jours":
                    debutPeriode = now.AddDays(-6);
                    finPeriode = now.AddDays(1);
                    break;
                case "semaine":
                    var delta = ((int)now.DayOfWeek + 6) % 7;
                    debutPeriode = now.AddDays(-delta);
                    finPeriode = debutPeriode.AddDays(7);
                    break;
                case "annee":
                    var year = annee.GetValueOrDefault(now.Year);
                    debutPeriode = new DateTime(year, 1, 1);
                    finPeriode = debutPeriode.AddYears(1);
                    break;
                default:
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Paramètre periode invalide (7jours|semaine|annee)", instance: HttpContext.Request.Path));
            }

            var rechercheAction = TYPE_ACTION.RECHERCHE_ATTESTATION.ToString();

            var jobsQuery = _dbContext.t_job.Where(j => j.r_created_at >= debutPeriode && j.r_created_at < finPeriode);
            var recherchesQuery = _dbContext.t_trace_action.Where(a => a.r_created_at >= debutPeriode && a.r_created_at < finPeriode && a.r_type_action == rechercheAction);
            var demandesQuery = _dbContext.t_demande_annulation.Where(d => d.r_created_at >= debutPeriode && d.r_created_at < finPeriode);

            if (!User.HasScope(Scopes.administrateur))
            {
                if (User.HasScope(Scopes.responsable_reseau))
                {
                    jobsQuery = jobsQuery.Where(j => (j.r_user != null && j.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || j.r_user_id_fk == userConnecte.r_id);
                    recherchesQuery = recherchesQuery.Where(a => (a.r_user != null && a.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || a.r_created_by == userConnecte.r_id);
                    demandesQuery = demandesQuery.Where(d => (d.r_user != null && d.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || d.r_user_id_fk == userConnecte.r_id);
                }
                else if (User.HasScope(Scopes.bureau_direct) || User.HasScope(Scopes.responsable_intermediaire))
                {
                    jobsQuery = jobsQuery.Where(j => (j.r_user != null && j.r_user.r_site_id_fk == userConnecte.r_site_id_fk) || j.r_user_id_fk == userConnecte.r_id);
                    recherchesQuery = recherchesQuery.Where(a => (a.r_user != null && a.r_user.r_site_id_fk == userConnecte.r_site_id_fk) || a.r_created_by == userConnecte.r_id);
                    demandesQuery = demandesQuery.Where(d => (d.r_user != null && d.r_user.r_site_id_fk == userConnecte.r_site_id_fk) || d.r_user_id_fk == userConnecte.r_id);
                }
                else
                {
                    jobsQuery = jobsQuery.Where(j => j.r_user_id_fk == userConnecte.r_id);
                    recherchesQuery = recherchesQuery.Where(a => a.r_created_by == userConnecte.r_id);
                    demandesQuery = demandesQuery.Where(d => d.r_user_id_fk == userConnecte.r_id);
                }
            }

            if (site > 0)
            {
                jobsQuery = jobsQuery.Where(j => j.r_user != null && j.r_user.r_site_id_fk == site);
                recherchesQuery = recherchesQuery.Where(a => a.r_user != null && a.r_user.r_site_id_fk == site);
                demandesQuery = demandesQuery.Where(d => d.r_user != null && d.r_user.r_site_id_fk == site);
            }

            if (user > 0)
            {
                jobsQuery = jobsQuery.Where(j => j.r_user_id_fk == user);
                recherchesQuery = recherchesQuery.Where(a => a.r_created_by == user);
                demandesQuery = demandesQuery.Where(d => d.r_user_id_fk == user);
            }

            var jobsData = await jobsQuery
                .Where(j => j.r_created_at.HasValue)
                .Select(j => new { Date = j.r_created_at!.Value.Date, Status = j.r_status })
                .ToListAsync();

            var recherchesData = await recherchesQuery
                .Where(a => a.r_created_at.HasValue)
                .Select(a => a.r_created_at!.Value.Date)
                .ToListAsync();

            var demandesData = await demandesQuery
                .Where(d => d.r_created_at.HasValue)
                .Select(d => new { Date = d.r_created_at!.Value.Date, Status = d.r_status })
                .ToListAsync();

            var jours = new[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
            var semaines = new List<evolutionPeriodeDto>();

            if (mode == "annee")
            {
                for (var start = debutPeriode; start < finPeriode; start = start.AddDays(7))
                {
                    var end = start.AddDays(7);
                    var numeroSemaine = System.Globalization.ISOWeek.GetWeekOfYear(start);

                    var tachesEnCours = jobsData.Count(x => x.Date >= start && x.Date < end && x.Status == STATUT_JOB.RUNNING);
                    var tachesAnnulees = jobsData.Count(x => x.Date >= start && x.Date < end && x.Status == STATUT_JOB.CANCELLED);
                    var tachesTerminees = jobsData.Count(x => x.Date >= start && x.Date < end && x.Status == STATUT_JOB.COMPLETED);

                    var demandesAttentes = demandesData.Count(x => x.Date >= start && x.Date < end && x.Status == STATUT_DEMANDE_ANNULATION.EN_ATTENTE);
                    var demandesTraitees = demandesData.Count(x => x.Date >= start && x.Date < end && x.Status == STATUT_DEMANDE_ANNULATION.TRAITE);
                    var demandesRejetees = demandesData.Count(x => x.Date >= start && x.Date < end && x.Status == STATUT_DEMANDE_ANNULATION.REJETE);

                    semaines.Add(new evolutionPeriodeDto
                    {
                        numero = numeroSemaine,
                        nom = $"Semaine {numeroSemaine}",
                        recherches = recherchesData.Count(d => d >= start && d < end),
                        taches = new tachesMoisDto
                        {
                            enCours = tachesEnCours,
                            annulees = tachesAnnulees,
                            terminees = tachesTerminees,
                            total = tachesEnCours + tachesAnnulees + tachesTerminees
                        },
                        demandes = new demandesMoisDto
                        {
                            attentes = demandesAttentes,
                            traitees = demandesTraitees,
                            rejetees = demandesRejetees,
                            total = demandesAttentes + demandesTraitees + demandesRejetees
                        }
                    });
                }
            }
            else
            {
                var nbJours = (finPeriode - debutPeriode).Days;
                for (var i = 0; i < nbJours; i++)
                {
                    var date = debutPeriode.AddDays(i);
                    var numeroJour = ((int)date.DayOfWeek + 6) % 7;

                    var tachesEnCours = jobsData.Count(x => x.Date == date && x.Status == STATUT_JOB.RUNNING);
                    var tachesAnnulees = jobsData.Count(x => x.Date == date && x.Status == STATUT_JOB.CANCELLED);
                    var tachesTerminees = jobsData.Count(x => x.Date == date && x.Status == STATUT_JOB.COMPLETED);

                    var demandesAttentes = demandesData.Count(x => x.Date == date && x.Status == STATUT_DEMANDE_ANNULATION.EN_ATTENTE);
                    var demandesTraitees = demandesData.Count(x => x.Date == date && x.Status == STATUT_DEMANDE_ANNULATION.TRAITE);
                    var demandesRejetees = demandesData.Count(x => x.Date == date && x.Status == STATUT_DEMANDE_ANNULATION.REJETE);

                    semaines.Add(new evolutionPeriodeDto
                    {
                        numero = i + 1,
                        nom = $"{jours[numeroJour]} {date:dd/MM}",
                        recherches = recherchesData.Count(d => d == date),
                        taches = new tachesMoisDto
                        {
                            enCours = tachesEnCours,
                            annulees = tachesAnnulees,
                            terminees = tachesTerminees,
                            total = tachesEnCours + tachesAnnulees + tachesTerminees
                        },
                        demandes = new demandesMoisDto
                        {
                            attentes = demandesAttentes,
                            traitees = demandesTraitees,
                            rejetees = demandesRejetees,
                            total = demandesAttentes + demandesTraitees + demandesRejetees
                        }
                    });
                }
            }

            var resultat = new evolutionDto
            {
                periode = mode,
                annee = mode == "annee" ? annee.GetValueOrDefault(now.Year) : null,
                recherches = semaines.Sum(x => x.recherches),
                taches = semaines.Sum(x => x.taches?.total ?? 0),
                demandes = semaines.Sum(x => x.demandes?.total ?? 0),
                periodes = semaines
            };

            return Ok(resultat);
        }




        [Authorize]
        [HttpGet("dashboard/evolutions")]
        public async Task<IActionResult> GetEvolutions(
            [FromQuery] int? annee = null,
            [FromQuery] int user = 0,
            [FromQuery] int site = 0)
        {
            var userConnecte = GetInfoUser();
            if (userConnecte == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var now = DateTime.UtcNow;
            var debutPeriode = annee.HasValue && annee.Value > 0
                ? new DateTime(annee.Value, 1, 1)
                : new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var finPeriode = debutPeriode.AddMonths(12);

            var rechercheAction = TYPE_ACTION.RECHERCHE_ATTESTATION.ToString();

            var jobsQuery = _dbContext.t_job.Where(j => j.r_created_at >= debutPeriode && j.r_created_at < finPeriode);
            var recherchesQuery = _dbContext.t_trace_action.Where(a => a.r_created_at >= debutPeriode && a.r_created_at < finPeriode && a.r_type_action == rechercheAction);
            var demandesQuery = _dbContext.t_demande_annulation.Where(d => d.r_created_at >= debutPeriode && d.r_created_at < finPeriode);

            if (!User.HasScope(Scopes.administrateur))
            {
                if (User.HasScope(Scopes.responsable_reseau))
                {
                    jobsQuery = jobsQuery.Where(j => (j.r_user != null && j.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || j.r_user_id_fk == userConnecte.r_id);
                    recherchesQuery = recherchesQuery.Where(a => (a.r_user != null && a.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || a.r_created_by == userConnecte.r_id);
                    demandesQuery = demandesQuery.Where(d => (d.r_user != null && d.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || d.r_user_id_fk == userConnecte.r_id);
                }
                else if (User.HasScope(Scopes.bureau_direct) || User.HasScope(Scopes.responsable_intermediaire))
                {
                    jobsQuery = jobsQuery.Where(j => (j.r_user != null && j.r_user.r_site_id_fk == userConnecte.r_site_id_fk) || j.r_user_id_fk == userConnecte.r_id);
                    recherchesQuery = recherchesQuery.Where(a => (a.r_user != null && a.r_user.r_site_id_fk == userConnecte.r_site_id_fk) || a.r_created_by == userConnecte.r_id);
                    demandesQuery = demandesQuery.Where(d => (d.r_user != null && d.r_user.r_site_id_fk == userConnecte.r_site_id_fk) || d.r_user_id_fk == userConnecte.r_id);
                }
                else
                {
                    jobsQuery = jobsQuery.Where(j => j.r_user_id_fk == userConnecte.r_id);
                    recherchesQuery = recherchesQuery.Where(a => a.r_created_by == userConnecte.r_id);
                    demandesQuery = demandesQuery.Where(d => d.r_user_id_fk == userConnecte.r_id);
                }
            }

            if (site > 0)
            {
                jobsQuery = jobsQuery.Where(j => j.r_user != null && j.r_user.r_site_id_fk == site);
                recherchesQuery = recherchesQuery.Where(a => a.r_user != null && a.r_user.r_site_id_fk == site);
                demandesQuery = demandesQuery.Where(d => d.r_user != null && d.r_user.r_site_id_fk == site);
            }

            if (user > 0)
            {
                jobsQuery = jobsQuery.Where(j => j.r_user_id_fk == user);
                recherchesQuery = recherchesQuery.Where(a => a.r_created_by == user);
                demandesQuery = demandesQuery.Where(d => d.r_user_id_fk == user);
            }

            var jobsParMois = await jobsQuery
                .GroupBy(j => new { Year = j.r_created_at!.Value.Year, Month = j.r_created_at!.Value.Month, Statut = j.r_status })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Statut, Count = g.Count() })
                .ToListAsync();

            var recherchesParMois = await recherchesQuery
                .GroupBy(a => new { Year = a.r_created_at!.Value.Year, Month = a.r_created_at!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var demandesParMois = await demandesQuery
                .GroupBy(d => new { Year = d.r_created_at!.Value.Year, Month = d.r_created_at!.Value.Month, Statut = d.r_status })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Statut, Count = g.Count() })
                .ToListAsync();

            var nomsMois = new[]
            {
                "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
            };

            var mois = new List<evolutionPeriodeDto>();

            for (int i = 0; i < 12; i++)
            {
                var dateMois = debutPeriode.AddMonths(i);
                var m = dateMois.Month;
                var y = dateMois.Year;

                var recherchesCount = recherchesParMois.FirstOrDefault(x => x.Year == y && x.Month == m)?.Count ?? 0;

                var tachesEnCours = jobsParMois.Where(x => x.Year == y && x.Month == m && x.Statut == STATUT_JOB.RUNNING).Sum(x => x.Count);
                var tachesAnnulees = jobsParMois.Where(x => x.Year == y && x.Month == m && x.Statut == STATUT_JOB.CANCELLED).Sum(x => x.Count);
                var tachesTerminees = jobsParMois.Where(x => x.Year == y && x.Month == m && x.Statut == STATUT_JOB.COMPLETED).Sum(x => x.Count);

                var demandesAttentes = demandesParMois.Where(x => x.Year == y && x.Month == m && x.Statut == STATUT_DEMANDE_ANNULATION.EN_ATTENTE).Sum(x => x.Count);
                var demandesTraitees = demandesParMois.Where(x => x.Year == y && x.Month == m && x.Statut == STATUT_DEMANDE_ANNULATION.TRAITE).Sum(x => x.Count);
                var demandesRejetees = demandesParMois.Where(x => x.Year == y && x.Month == m && x.Statut == STATUT_DEMANDE_ANNULATION.REJETE).Sum(x => x.Count);

                mois.Add(new evolutionPeriodeDto
                {
                    numero = m,
                    nom = $"{nomsMois[m - 1]} {y}",
                    recherches = recherchesCount,
                    taches = new tachesMoisDto
                    {
                        enCours = tachesEnCours,
                        annulees = tachesAnnulees,
                        terminees = tachesTerminees,
                        total = tachesEnCours + tachesAnnulees + tachesTerminees
                    },
                    demandes = new demandesMoisDto
                    {
                        attentes = demandesAttentes,
                        traitees = demandesTraitees,
                        rejetees = demandesRejetees,
                        total = demandesAttentes + demandesTraitees + demandesRejetees
                    }
                });
            }

            var resultat = new evolutionDto
            {
                recherches = mois.Sum(x => x.recherches),
                taches = mois.Sum(x => (x.taches?.enCours ?? 0) + (x.taches?.annulees ?? 0) + (x.taches?.terminees ?? 0)),
                demandes = mois.Sum(x => (x.demandes?.attentes ?? 0) + (x.demandes?.traitees ?? 0) + (x.demandes?.rejetees ?? 0)),
                periodes = mois
            };

            return Ok(resultat);
        }


        [Authorize]
        [RequireScope(Scopes.administrateur)]
        [HttpGet("audits/actions")]
        public async Task<IActionResult> GeLog([FromQuery] int page = 1, [FromQuery] int limit = 10 , [FromQuery] string action = "", [FromQuery] string search = "")
        {
            const string _desc_route = "Liste des logs";

            try
            {

                t_user userConnecte = GetInfoUser();


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
        [RequireScope(Scopes.administrateur)]
        [HttpGet("audits/acces")]
        public async Task<IActionResult> GeLogAcces([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "" , [FromQuery] string action = "")
        {
            const string _desc_route = "Liste des logs";

            try
            {

                var pagination = new PaginationParams(page, limit);

                t_user userConnecte = GetInfoUser();

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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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

                _dbContext.t_modele.Remove(resQuery);
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


                if (User.HasScope(Scopes.responsable_intermediaire)) // Uniquement le site de l'utilisateur connecté
                {
                   
                    baseQuery = baseQuery.Where(u => u.r_site != null && userConnecte.r_site_id_fk == u.r_site_id_fk);
                }


               
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToUpper().Trim();

                    baseQuery = baseQuery.Where(x =>
                        x.r_nom.ToUpper().Contains(search) ||
                        (x.r_nom+' ' + x.r_prenom).ToUpper().Contains(search) ||
                        x.r_email.ToUpper().Contains(search) ||
                        x.r_telephone.ToUpper().Contains(search) ||
                        x.r_prenom.ToUpper().Contains(search)
                    );
                }

                if (status > 0)
                {
                    baseQuery = baseQuery.Where(x => (int)x.r_statut == status);
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
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUtilisateur(int id)
        {
            const string _desc_route = "Détails d'un utilisateur";

            try
            {

                var User = await _dbContext.t_user
                    .Include(u => u.r_site)
                    .FirstOrDefaultAsync(u => u.r_id == id && u.r_is_delete != true);


                if (User == null)
                {
                    return NotFound(GeneraleRetour.BuildNotFound(
                       detail: "L'utilisateur est introuvable",
                       instance: HttpContext.Request.Path
                    ));
                }

       

                return Ok(Tools.Tools.BuildUserToUserResponseDto(User));

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

                var userConnecte = GetInfoUser();
                if (userConnecte == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

                var requestedType = _body.roleId ?? TYPE_UTILISATEUR.UTILISATEUR;
                if (!CanAssignUserType(userConnecte.r_type, requestedType))
                {
                    return StatusCode(403, GeneraleRetour.BuildForbid(
                        detail: "Vous n'êtes pas autorisé à attribuer ce type d'utilisateur.",
                        instance: HttpContext.Request.Path));
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
                    r_statut = STATUT_USER.ACTIVE,
                    r_password_change_required = true,
                    r_password = BCrypt.Net.BCrypt.HashPassword(myPass),
                    r_date_last_statut = DateTime.UtcNow,
                    r_site_id_fk = _body.siteId ?? 0,
                    r_type = requestedType,
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


            
                var userConnecte = GetInfoUser();
                if (userConnecte == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

                var requestedType = _body.roleId ?? TYPE_UTILISATEUR.UTILISATEUR;
                if (!CanAssignUserType(userConnecte.r_type, requestedType))
                {
                    return StatusCode(403, GeneraleRetour.BuildForbid(
                        detail: "Vous n'êtes pas autorisé à attribuer ce type d'utilisateur.",
                        instance: HttpContext.Request.Path));
                }

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
                    .Include(us => us.r_site)
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
                User.r_site_id_fk = _body.siteId ?? 0;
                User.r_type = requestedType;

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
        [RequireScope(Scopes.administrateur)]
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
                    r_type = (TYPE_SITE)_body.type
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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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

                /// Vérifiez si le site est associé à des utilisateurs

                var resUsers = await _dbContext.t_user
                    .Where(e => e.r_site_id_fk == id && e.r_is_delete != true)
                    .FirstOrDefaultAsync();

                if (resUsers != null)
                    return Conflict(GeneraleRetour.BuildProblemResponse(
                        new GeneraleRetour
                        {
                            status = 409,
                            detail = "Le site est associé à des utilisateurs. Impossible de le supprimer."
                        },
                        instance: HttpContext.Request.Path));

                _dbContext.t_site.Remove(resQuery);
                await _dbContext.SaveChangesAsync();

                await _traceService.TraceActionAsync(TYPE_ACTION.SUPPRESSION_SITE, description: $"Suppression du site : {resQuery.r_nom}");

                return NotFound();
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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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
        [RequireScope(Scopes.administrateur)]
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

                _dbContext.t_motif_annulation.Remove(resQuery);
                await _dbContext.SaveChangesAsync();

                await _traceService.TraceActionAsync(TYPE_ACTION.SUPPRESSION_MOTIF_ANNULATION, description: $"Suppression du motif d'annulation : {resQuery.r_libelle}");

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        #endregion




        [Authorize]
        [RequireAnyScope(Scopes.administrateur)]

        [HttpGet("sites/types")]
        public async Task<IActionResult> GetSitesTypes([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            const string _desc_route = "Liste des sites types";

            try
            {
                var pagination = new PaginationParams(page, limit);

                var baseQuery = Enum.GetValues<TYPE_SITE>()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();
                    baseQuery = baseQuery.Where(x =>
                        Tools.Tools.EquivalenceTypeSite(x).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        x.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var total = baseQuery.Count();

                var siteTypes = baseQuery
                    .Skip(pagination.Skip)
                    .Take(pagination.Take)
                    .Select(m => Tools.Tools.BuildSiteTypeToSiteTypeResponseDto(m))
                    .ToList();

                return Ok(PaginatedResponse<siteTypeResponseDto>.Create(siteTypes, total, page, limit));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


    }
}
