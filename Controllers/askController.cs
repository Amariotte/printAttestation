using System.Data;
using System.Net;
using print_attestation.ContextDb;
using print_attestation.Dtos.Response;
using print_attestation.Model;
using print_attestation.Services;
using print_attestation.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OracleApi.Services;
using print_attestation.ScopeAttribute;
using print_attestation.Security;
using print_attestation.Dtos.General;
using print_attestation.Dtos.Request;


namespace print_attestation.Controllers
{
    [Route("api/[controller]")]

    public class askController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        private readonly askContext _dbContext;

        private readonly IConfiguration _configuration;
        private readonly ServiceAsaci _ServiceAsaci;
        private readonly ParamMessage _paramdata;
        private readonly ILogger<askController> _logger;
        private readonly IOracleService _oracleService;
        private readonly TraceService _traceService;
        private readonly ZipJobManager _manager;
        private readonly ZipAttestationService _service;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ParamAppSettings _param_app_settings;

        //private readonly ILogger _logger;
        public askController(askContext askContext, TraceService traceService, ServiceAsaci ServiceAsaci,
            IOptions<ParamMessage> paramdata, IOptions<ParamAppSettings> param_app_settings, IConfiguration configuration, 
            IWebHostEnvironment env, ILogger<askController> logger, IOracleService oracleService,
         ZipJobManager manager,
       ZipAttestationService service, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _ServiceAsaci = ServiceAsaci;
            _env = env;
            _paramdata = paramdata.Value;
            _param_app_settings = param_app_settings.Value;
            _logger = logger;
            _dbContext = askContext;
            _oracleService = oracleService;
            _traceService = traceService;
            _manager = manager;
            _service = service;
            _scopeFactory = scopeFactory;
                
        }

     

        [NonAction]
        public t_user GetInfoUser()
        {
            if (HttpContext.Items.ContainsKey("User"))
            {
                return (t_user)HttpContext.Items["User"];
            }
            else
            {
                return null;
            }
        }

        #region Attestation
        [Authorize]
        [HttpPost("attestations/{type}/jobs")]
        public async Task<IActionResult> CreateJobsByType(string type, [FromBody] List<string> numAttestations)
        {
            if (numAttestations == null || !numAttestations.Any())
                return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins un numéro d'attestation est requis", instance: HttpContext.Request.Path));

            t_user? dataUser = GetInfoUser();

            if (dataUser == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var nums = numAttestations
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

          
            var job = _manager.Create(nums);
            job.CancellationTokenSource = new System.Threading.CancellationTokenSource();

            var jobType = (type ?? "").Trim().ToLowerInvariant();
            if (jobType != "atd" && jobType != "cedeao" && jobType != "atd_cedeao")
            {
                return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Type de tâche invalide (atd|cedeao|atd_cedeao)", instance: HttpContext.Request.Path));
            }

            switch (jobType)
            {
                case "cedeao":
                    await _traceService.TraceActionAsync(TYPE_ACTION.GENERATION_CEDEAO_ZIP, userId: dataUser.r_id, userEmail: dataUser.r_email, description: $"Génération de l'archive de {numAttestations.Count} cedeao : ID = {job.r_job_id}");
                    break;

                case "atd":
                    await _traceService.TraceActionAsync(TYPE_ACTION.GENERATION_ATD_ZIP, userId: dataUser.r_id, userEmail: dataUser.r_email, description: $"Génération de l'archive de {numAttestations.Count} attestation(s) : ID = {job.r_job_id}");
                    break;
                case "atd_cedeao":
                    await _traceService.TraceActionAsync(TYPE_ACTION.GENERATION_ATD_CEDEAO_ZIP, userId: dataUser.r_id, userEmail: dataUser.r_email, description: $"Génération de l'archive de {numAttestations.Count} attestation(s) : ID = {job.r_job_id}");
                    break;
                default:
                    // Cas par défaut
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Type de tâche invalide (atd|cedeao|atd_cedeao)", instance: HttpContext.Request.Path));
            }

          


            job.r_created_by = dataUser.r_id;
            job.r_user_id_fk = dataUser.r_id;
            job.r_status = STATUT_JOB.RUNNING;
            job.r_total = nums.Count;
            job.r_type = jobType.ToUpperInvariant();
            
            _dbContext.t_job.Add(job);
            await _dbContext.SaveChangesAsync();

           

            // lancer le job
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<ZipAttestationService>();


                    switch (jobType)
                    {
                        case "cedeao":
                            await svc.GenerateZipCEDEAO(job);
                            break;

                        case "atd":
                            await svc.GenerateZipATD(job);
                            break;
                        case "atd_cedeao":
                            await svc.GenerateZipATDAndCEDEAO(job);
                            break;
                    }


              
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur dans le job de génération ZIP");
                }
            });
           

            return Ok(new { jobId = job.r_job_id });
        }


        [Authorize]
        [HttpGet("attestations/jobs/events/{id}")]
        public async Task GetZipEvents(string id)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var job = _manager.Get(id);

            if (job == null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("data: {\"error\":\"Job non trouvé\"}\n\n");
                return;
            }

            var reader = job.Events.Reader;

            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var ev))
                {
                    var json = JsonConvert.SerializeObject(ev);
                    await Response.WriteAsync($"data: {json}\n\n");
                    await Response.Body.FlushAsync();
                }

                if (job.r_status != STATUT_JOB.RUNNING && reader.Count == 0)
                    break;
            }
        }

        [Authorize]

        [HttpGet("attestations/jobs")]
        public async Task<IActionResult> ListJobs([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? type = null, [FromQuery] int? status = null)
        {

            var userConnecte = GetInfoUser();
            if (userConnecte == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));


            var pagination = new PaginationParams(page, limit);

            var baseQuery = _dbContext.t_job.AsQueryable();


            if (!User.HasScope(Scopes.administrateur))
            {

                if (User.HasScope(Scopes.responsable_reseau)) // Voir pour tout les utilisateurs sauf les administrateurs
                {
                    baseQuery = baseQuery.Where(u => ((u.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR  ) || (u.r_user_id_fk != null && u.r_user_id_fk == userConnecte.r_id)));
                }
                else if (User.HasScope(Scopes.bureau_direct)) // Voir pour tout les utilisateurs de son bureau et pour lui meme
                {
                    baseQuery = baseQuery.Where(u => ((u.r_user.r_site.r_id == userConnecte.r_site_id_fk) || (u.r_user_id_fk != null && u.r_user_id_fk == userConnecte.r_id)));
                }
                else if (User.HasScope(Scopes.responsable_intermediaire)) // Voir pour tout les utilisateurs de son bureau et pour lui meme
                {
                    baseQuery = baseQuery.Where(u => ((u.r_user.r_site.r_id == userConnecte.r_site_id_fk) || (u.r_user_id_fk != null && u.r_user_id_fk == userConnecte.r_id)));
                }
                else // Voir uniquement pour lui meme
                {
                    baseQuery = baseQuery.Where(u => ((u.r_user_id_fk != null && u.r_user_id_fk == userConnecte.r_id)));
                }

            }


            // Filtre par statut si fourni (valeurs : RUNNING, COMPLETED, CANCELLED)
            if (status > 0)
            {
                baseQuery = baseQuery.Where(x => (int)x.r_status == status);
            }

            // Filtre par type si fourni (atd|cedeao)
            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim();
                baseQuery = baseQuery.Where(x => x.r_type == type);
            }

      
            var total = await baseQuery.CountAsync();


            var jobs = await baseQuery
                 .Include(u => u.r_user)
                        .ThenInclude(us => us.r_site)
                .OrderByDescending(x => x.r_created_at)
                .Skip((pagination.page - 1) * pagination.limit)
                .Take(pagination.limit)
                .ToListAsync();

            
              var jobsDto = jobs.Select(j => Tools.Tools.BuildJobToJobResponseDto(j)).ToList();

            return Ok(PaginatedResponse<jobReponseDto>.Create(jobsDto, total, pagination.page, pagination.limit));

        }


        [Authorize]
        [HttpGet("attestations/jobs/{jobId}")]
        public async Task<IActionResult> GetJobDetail(string jobId)
        {
            var userConnecte = GetInfoUser();
            if (userConnecte == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var jobRec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == jobId);
            if (jobRec == null)
                return NotFound(GeneraleRetour.BuildNotFound(detail: "Job introuvable", instance: HttpContext.Request.Path));

        

            if (!(await HasAccessToJob(jobRec, userConnecte)))
                return StatusCode(403,GeneraleRetour.BuildForbid(instance: HttpContext.Request.Path, detail:"Accès refusé"));

            return Ok(Tools.Tools.BuildJobToJobResponseDto(jobRec));
        }


        [Authorize]
        [HttpPost("attestations/jobs/{jobId}/cancel")]
        public async Task<IActionResult> StopJob(string jobId)
        {
            var user = GetInfoUser();
            if (user == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var jobRec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == jobId);
            if (jobRec == null)
                return NotFound(GeneraleRetour.BuildNotFound(detail: "Job introuvable", instance: HttpContext.Request.Path));



            if (!(await HasAccessToJob(jobRec, user)))
                return StatusCode(403, GeneraleRetour.BuildForbid(detail: "Accès refusé", instance: HttpContext.Request.Path));

            // arrêter le job en mémoire
            var job = _manager.Get(jobId);
            if (job != null)
            {
                job.Stop();
                try
                {
                    await job.Events.Writer.WriteAsync(new { type = "stopped", data = new { message = "Job arrêté par l'utilisateur" } });
                }
                catch { }
            }

            jobRec.r_status = STATUT_JOB.CANCELLED;
            jobRec.r_is_active = false;
            _dbContext.Update(jobRec);
            await _dbContext.SaveChangesAsync();


            await _traceService.TraceActionAsync(TYPE_ACTION.ANNULATION_GENERATION_ZIP, userId: user.r_id, userEmail: user.r_email, description: $"Annulation de la génération de la tâche : {jobRec.r_job_id}");


            return Ok(new { success = true });
        }

       

        /// <summary>
        /// Convertit une chaîne Base64 en tableau de bytes (image)

        /// <summary>
        /// Sauvegarde une image Base64 sur le disque et retourne le chemin
        /// </summary>
        [NonAction]
        public async Task<string> SaveBase64ImageToFile(string base64String, string fileName, string subfolder = "attestations")
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentException("La chaîne Base64 ne peut pas être vide", nameof(base64String));

            // Nettoyer le préfixe data:image si présent
            if (base64String.Contains(","))
            {
                base64String = base64String.Split(',')[1];
            }

            byte[] imageBytes = Convert.FromBase64String(base64String);

            // Créer le dossier s'il n'existe pas
            string folderPath = Path.Combine(_env.WebRootPath, subfolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Générer le chemin complet
            string filePath = Path.Combine(folderPath, fileName);

            // Sauvegarder le fichier
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            return filePath;
        }

        [Authorize]
        [HttpGet("attestations/{cleRechercheEncode}")]
        public async Task<IActionResult> GetAttestation(string cleRechercheEncode, [FromQuery] int page = 1, [FromQuery] int limit = 10,[FromQuery] string status = "")
        {
            string _desc_route = "Obtenir une attestation";

            try
            {

                t_user dataUser = GetInfoUser();


                string cleRecherche =  WebUtility.UrlDecode(cleRechercheEncode);


                await _traceService.TraceActionAsync(
                  TYPE_ACTION.RECHERCHE_ATTESTATION,
                  userId: dataUser.r_id,
                  userEmail: dataUser.r_email,
                  description: $"Recherche d'attestation : recherche = {cleRecherche}");

                if (string.IsNullOrWhiteSpace(cleRecherche))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de l'attestation est requis", instance: HttpContext.Request.Path));

                // Validation de sécurité pour éviter les injections SQL
                if (!Tools.Tools.IsValidSearchKey(cleRecherche))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Format de recherche invalide", instance: HttpContext.Request.Path));


                var pagination = new PaginationParams(page, limit);


                string statutSql = "";
                string codeInteSql = "";


                var caracteres = _param_app_settings.immatriculation.charactersToReplace ?? [];

                var numeImmaSql = SqlReplace("a.NUMEIMMA", caracteres);
                var cleRechercheSql = SqlReplace(":cleRecherche", caracteres);

                if (status == "ACTIVE")
                {
                    statutSql = " AND TRUNC(a.DATECHAT) >= TRUNC(SYSDATE)";
                }
                else if (status == "EXPIREE")
                {
                    statutSql = " AND TRUNC(a.DATECHAT) < TRUNC(SYSDATE)";
                }

               
                if (User.HasScope(Scopes.utilisateur) || User.HasScope(Scopes.responsable_intermediaire)) // Uniquement le site de l'utilisateur connecté
                {
                    codeInteSql = " AND a.CODEINTE =:codeInte";
                }
               

                // Requête SQL pour compter le nombre total d'attestations correspondant à la recherche
                string _sqlCount = @"SELECT count(*) AS nb
                                        FROM attestation_risque a LEFT JOIN intermediaire i ON a.CODEINTE = i.CODEINTE
                                        WHERE (a.LIEN_PDF IS NOT NULL OR a.LIEN_IMG IS NOT NULL OR a.LIEN__QR IS NOT NULL)
                                          AND ( UPPER(TRIM(a.NUMEIMMA)) = UPPER(:cleRecherche) OR UPPER({numeImmaSql}) = UPPER({cleRechercheSql}) OR UPPER(TRIM(a.NUMECHAS)) = UPPER(:cleRecherche) OR TRIM(a.NUMATTDI) = UPPER(:cleRecherche) OR UPPER(TO_CHAR(a.NUMEPOLI)) = UPPER(:cleRecherche) OR UPPER(TO_CHAR(a.CODEINTE)) || '/' || UPPER(TRIM(TO_CHAR(a.NUMEPOLI))) = UPPER(:cleRecherche))
                                           {statutSql}{codeInteSql}";
                _sqlCount = _sqlCount.Replace("{statutSql}", statutSql);
                _sqlCount = _sqlCount.Replace("{codeInteSql}", codeInteSql);
                _sqlCount = _sqlCount.Replace("{numeImmaSql}", numeImmaSql);
                _sqlCount = _sqlCount.Replace("{cleRechercheSql}", cleRechercheSql);

                // Requête SQL sécurisée avec pagination Oracle (ROWNUM)
                int offset = (page - 1) * limit;
                string _sql = @"SELECT * FROM (
                                    SELECT t.*, ROWNUM rn
                                    FROM (
                                        SELECT TO_CHAR(a.CODEINTE) || '/' || TO_CHAR(a.NUMEPOLI) AS NUMEPOLI,
                                               a.DATEFFAT,a.DATECHAT,a.MARQVEHI,a.TYPEVEHI,a.NUMEIMMA,a.NUMECHAS,
                                               a.PROPATTE,a.NUMATTDI,a.LIEN_PDF,a.LIEN__QR,a.LIEN_IMG,a.CODEINTE,
                                               i.RAISOCIN,a.CREE__LE,
                                         CASE WHEN TRUNC(a.DATECHAT) >= TRUNC(SYSDATE) THEN 'ACTIVE' ELSE 'EXPIREE' END AS STATUT
                                        FROM attestation_risque a 
                                        LEFT JOIN intermediaire i ON a.CODEINTE = i.CODEINTE
                                        WHERE (a.LIEN_PDF IS NOT NULL OR a.LIEN_IMG IS NOT NULL OR a.LIEN__QR IS NOT NULL)
                                          AND (UPPER(TRIM(a.NUMEIMMA)) = UPPER(:cleRecherche) OR UPPER(TRIM(a.NUMECHAS)) = UPPER(:cleRecherche) OR 
                                               UPPER(TRIM(a.NUMATTDI)) = UPPER(:cleRecherche) OR UPPER(TO_CHAR(a.NUMEPOLI)) = :cleRecherche OR
                                               UPPER({numeImmaSql}) = UPPER({cleRechercheSql}) OR
                                               UPPER(TO_CHAR(a.CODEINTE)) || '/' || UPPER(TRIM(TO_CHAR(a.NUMEPOLI))) = UPPER(:cleRecherche))
                                           {statutSql}{codeInteSql}
                                        ORDER BY a.CREE__LE DESC, a.DATECHAT DESC, a.DATEFFAT DESC
                                    ) t
                                    WHERE ROWNUM <= :maxRow
                                )
                                WHERE rn > :offset";

                _sql = _sql.Replace("{statutSql}", statutSql);
                _sql = _sql.Replace("{codeInteSql}", codeInteSql);
                _sql = _sql.Replace("{numeImmaSql}", numeImmaSql);
                _sql = _sql.Replace("{cleRechercheSql}", cleRechercheSql);

                // Paramètres pour la requête COUNT
                var countParameters = new Dictionary<string, object>
                {
                    { ":cleRecherche", cleRecherche }
                };

               
                // Paramètres pour la requête paginée
                var parameters = new Dictionary<string, object>
                {
                    { ":cleRecherche", cleRecherche },
                    { ":offset", offset },
                    { ":maxRow", offset + limit }
                };

                if (!string.IsNullOrWhiteSpace(codeInteSql))
                {
                    string codeInte = dataUser.r_site != null ? dataUser.r_site.r_code : string.Empty;
                    parameters.Add(":codeInte", codeInte);
                    countParameters.Add(":codeInte", codeInte);
                }


                var rowsCount = await _oracleService.ExecuteQueryAsync(_sqlCount, countParameters);
                if (!rowsCount.Any())
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));

                // Total avant pagination - gérer différents types numériques retournés par Oracle
                var totalValue = rowsCount.FirstOrDefault()?["NB"];
                int total = 0;
                if (totalValue != null)
                {
                    total = Convert.ToInt32(totalValue);
                }

                if (total == 0)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));


                var rows = await _oracleService.ExecuteQueryAsync(_sql, parameters);

                // Mapper les résultats (pagination déjà effectuée dans la requête SQL)
                var results = rows.Select(row => new AttestationResponseDto
                {
                    numPolice = row.ContainsKey("NUMEPOLI") ? row["NUMEPOLI"]?.ToString() : null,
                    dateEffet = row.ContainsKey("DATEFFAT") ? (row["DATEFFAT"] as DateTime?) : null,
                    dateEcheance = row.ContainsKey("DATECHAT") ? (row["DATECHAT"] as DateTime?) : null,
                    dateCreation = row.ContainsKey("CREE__LE") ? (row["CREE__LE"] as DateTime?) : null,
                    marqueVehicule = row.ContainsKey("MARQVEHI") ? row["MARQVEHI"]?.ToString() : null,
                    typeVehicule = row.ContainsKey("TYPEVEHI") ? row["TYPEVEHI"]?.ToString() : null,
                    numImmatriculation = row.ContainsKey("NUMEIMMA") ? row["NUMEIMMA"]?.ToString() : null,
                    numChassis = row.ContainsKey("NUMECHAS") ? row["NUMECHAS"]?.ToString() : null,
                    nomAssure = row.ContainsKey("PROPATTE") ? row["PROPATTE"]?.ToString() : null,
                    numAttestation = row.ContainsKey("NUMATTDI") ? row["NUMATTDI"]?.ToString() : null,
                    urlPdf = row.ContainsKey("LIEN_PDF") ? row["LIEN_PDF"]?.ToString() : null,
                    urlQr = row.ContainsKey("LIEN__QR") ? row["LIEN__QR"]?.ToString() : null,
                    urlImage = row.ContainsKey("LIEN_IMG") ? row["LIEN_IMG"]?.ToString() : null,
                    nomIntermediaire = row.ContainsKey("RAISOCIN") ? row["RAISOCIN"].ToString() : null,
                    statut = row.ContainsKey("STATUT") ? row["STATUT"].ToString() : null,

                }).ToList();


           
                return Ok(PaginatedResponse<AttestationResponseDto>.Create(results, total, page, limit));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: ex.Message));
            }
        }



        [Authorize]
        [HttpGet("attestations/{numAttestation}/cedeao")]
        public async Task<IActionResult> AttestationCedeao(string numAttestation)
        {
            string _desc_route = "Impression de l'attestation Cedeao";

            try
            {
                if (string.IsNullOrWhiteSpace(numAttestation))
                    return BadRequest(GeneraleRetour.BuildNotFound(detail: "Le numéro de l'attestation est requis", instance: HttpContext.Request.Path));

                await _traceService.TraceActionAsync(TYPE_ACTION.TELECHARGEMENT_CEDEAO, description: $"Impression de l'attestation Cedeao : {numAttestation}");

                var result = await _ServiceAsaci.printCedeao(numAttestation);
                if (result.status != 200)
                {
                    return StatusCode(result.status,
                        GeneraleRetour.BuildProblemResponse(new GeneraleRetour { status = result.status, detail = result.detail }, instance: HttpContext.Request.Path));
                }

                var res_data = JsonConvert.DeserializeObject<dynamic>(result.data);

                // Convertir Base64 en image
                string base64 = res_data.base64?.ToString();

                if (string.IsNullOrWhiteSpace(base64))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'image Base64 est manquante dans la réponse", instance: HttpContext.Request.Path));

                return Ok(new{base64 = base64 });

            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));

            }
        }

    
        [Authorize]
        [HttpGet("attestations/download/{fileName}")]
       
        public async Task<IActionResult> DownloadZip(string fileName)
        {
            var path = Path.Combine(Path.GetTempPath(), fileName);
            string _desc_route = "Télécharger le fichier ZIP";

            if (!System.IO.File.Exists(path))
            {
                return NotFound("Fichier introuvable.");
            }


            await _traceService.TraceActionAsync(TYPE_ACTION.TELECHARGEMENT_ZIP, description: $"Téléchargement de l'archive {fileName}");



            //// Supprimer le fichier après téléchargement du client
            //HttpContext.Response.OnCompleted(async () =>
            //{
            //    try
            //    {
            //        if (System.IO.File.Exists(path))
            //        {
            //            System.IO.File.Delete(path);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex,$"[{_desc_route}]");
            //    }


            //    await Task.CompletedTask;
            //});

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return File(stream, "application/zip", fileName);
        }


        [HttpGet("attestations/zip/sse/{id}")]
        public async Task GetZipSse(string id)
        {

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            var job = _manager.Get(id);


            if (job == null)
            {
                Response.StatusCode = 404;
                return;
            }


            async Task SendEvent(string type, object data)
            {
                var json = JsonConvert.SerializeObject(data);

                await HttpContext.Response.WriteAsync(
                    $"event: {type}\n" +
                    $"data: {json}\n\n"
                );

                await HttpContext.Response.Body.FlushAsync();
            }


            await foreach (var item in job.Events.Reader.ReadAllAsync())
            {

                var json = JsonConvert.SerializeObject(item);
                var eventData = JsonConvert.DeserializeObject<JobEvent>(json);

                await SendEvent(eventData.type, eventData.data);

            
                if (eventData.type == "complete")
                {
                    // arrêter le SSE
                    break;
                }

            }

            // Supprimer le job après fermeture du SSE
            job.Events.Writer.Complete();

        }

        #endregion
        [Authorize]
        [RequireScope(Scopes.administrateur)]
        [HttpGet("sites")]
        public async Task<IActionResult> ChargerLesSites()
        {
            string _desc_route = "Charger les sites";

            try
            {

                string _sql = @" SELECT CODEINTE, RAISOCIN FROM INTERMEDIAIRE";

                var rows = await _oracleService.ExecuteQueryAsync(_sql);

                if (!rows.Any())
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row.ContainsKey("RAISOCIN") && row["RAISOCIN"] != null)
                    {

                        string codeInt = row["CODEINTE"]?.ToString();
                        var existingSite = await _dbContext.t_site.FirstOrDefaultAsync(s => s.r_code == codeInt);
                        if (existingSite != null)
                        {
                            existingSite.r_nom = row["RAISOCIN"]?.ToString();
                            _dbContext.Update(existingSite);
                            continue;
                        }
                        else
                        {
                            t_site s = new t_site
                            {
                                r_code = row["CODEINTE"]?.ToString(),
                                r_nom = row["RAISOCIN"]?.ToString()
                            };

                            _dbContext.Add(s);
                        }

                    }

                    await _dbContext.SaveChangesAsync();
                }

                return Ok("Opération terminée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }




        [Authorize]
        [HttpGet("demandes/annulations")]
        public async Task<IActionResult> ListDemandeAnnulation([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? type = null, [FromQuery] int? status = null)
        {

            var user = GetInfoUser();
            if (user == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));


            var pagination = new PaginationParams(page, limit);

            var query = _dbContext.t_demande_annulation.AsQueryable();

          
   
            // Filtre par statut si fourni (valeurs : RUNNING, COMPLETED, CANCELLED)
            if (status > 0)
            {
                query = query.Where(x => (int)x.r_status == status);
            }


            var total = await query.CountAsync();


            var demandes = await query
                 .Include(u => u.r_user)
                 .Include(u => u.r_site)
                 .Include(u => u.r_motif_annulation)
                        .Include(us => us.r_site)
                .OrderByDescending(x => x.r_created_at)
                .Skip((pagination.page - 1) * pagination.limit)
                .Take(pagination.limit)
                .ToListAsync();


            var demandesDto = demandes.Select(d => Tools.Tools.BuildDemandeAnnulationResponseDto(d)).ToList();

            return Ok(PaginatedResponse<demandeAnnulationResponseDto>.Create(demandesDto, total, pagination.page, pagination.limit));

        }



        [Authorize]
        [HttpPost("demandes/annulations")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreerUneDemandeAnnulation([FromForm] demandeAnnulationCreateFormDto body)
        {
            const string _desc_route = "Créer une demande d'annulation";

            try
            {
                var user = GetInfoUser();
                if (user == null)
                    return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

                if (body == null)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Les données sont requises", instance: HttpContext.Request.Path));

                if (body.motifAnnulationId <= 0)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le motif d'annulation est requis", instance: HttpContext.Request.Path));

                var hasReference = !string.IsNullOrWhiteSpace(body.numPolice)
                                   || !string.IsNullOrWhiteSpace(body.numAttestation)
                                   || !string.IsNullOrWhiteSpace(body.numImmatriculation);

                if (!hasReference)
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins une référence est requise (numPolice, numAttestation ou numImmatriculation)", instance: HttpContext.Request.Path));

                if (body.files == null || !body.files.Any())
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins un fichier est requis (clé form-data: files)", instance: HttpContext.Request.Path));

                var motif = await _dbContext.t_motif_annulation
                    .FirstOrDefaultAsync(m => m.r_id == body.motifAnnulationId && m.r_is_delete != true);

                if (motif == null)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Motif d'annulation introuvable", instance: HttpContext.Request.Path));

                var demande = new t_demande_annulation
                {
                    r_status = STATUT_DEMANDE_ANNULATION.EN_ATTENTE,
                    r_user_id_fk = user.r_id,
                    r_site_id_fk = user.r_site_id_fk,
                    r_motif_annulation_id_fk = body.motifAnnulationId,
                    r_num_police = body.numPolice?.Trim(),
                    r_num_attestation = body.numAttestation?.Trim(),
                    r_num_immatriculation = body.numImmatriculation?.Trim(),
                    r_created_by = user.r_id,
                    r_created_at = DateTime.UtcNow
                };

                await _dbContext.t_demande_annulation.AddAsync(demande);
                await _dbContext.SaveChangesAsync();

                var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? Path.Combine(_env.ContentRootPath, "wwwroot")
                    : _env.WebRootPath;

                var uploadFolder = Path.Combine(webRoot, "uploads", "demandes-annulations", demande.r_id.ToString());
                Directory.CreateDirectory(uploadFolder);

                var fichiers = new List<t_demande_annulation_fichier>();

                foreach (var file in body.files.Where(f => f != null && f.Length > 0))
                {
                    var extension = Path.GetExtension(file.FileName);
                    var safeName = $"{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadFolder, safeName);

                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    fichiers.Add(new t_demande_annulation_fichier
                    {
                        r_demande_annulation_id_fk = demande.r_id,
                        r_nom_fichier = file.FileName,
                        r_chemin_fichier = filePath,
                        r_created_by = user.r_id,
                        r_created_at = DateTime.UtcNow
                    });
                }

                if (fichiers.Count > 0)
                {
                    await _dbContext.t_demande_annulation_fichier.AddRangeAsync(fichiers);
                    await _dbContext.SaveChangesAsync();
                }

                var created = await _dbContext.t_demande_annulation
                    .Include(d => d.r_user)
                    .Include(d => d.r_fichiers)
                    .Include(d => d.r_site)
                    .Include(d => d.r_motif_annulation)
                    .FirstOrDefaultAsync(d => d.r_id == demande.r_id);

                return Ok(Tools.Tools.BuildDemandeAnnulationResponseDto(created!));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


        [NonAction]
        private async Task<bool> HasAccessToJob(t_job jobRec, t_user user)
        {


            if (!User.HasScope(Scopes.administrateur))
            {

                if (User.HasScope(Scopes.responsable_reseau)) // Voir pour tout les utilisateurs sauf les administrateurs
                {
                   return (jobRec.r_user.r_type != TYPE_UTILISATEUR.ADMINISTRATEUR) || (jobRec.r_user_id_fk != null && jobRec.r_user_id_fk == user.r_id);
                }
                else if (User.HasScope(Scopes.bureau_direct)) // Voir pour tout les utilisateurs de son bureau et pour lui meme
                {
                    return (jobRec.r_user.r_site.r_id == user.r_site_id_fk) || (jobRec.r_user_id_fk != null && jobRec.r_user_id_fk == user.r_id);
                }
                else if (User.HasScope(Scopes.responsable_intermediaire)) // Voir pour tout les utilisateurs de son bureau et pour lui meme
                {
                    return ((jobRec.r_user.r_site.r_id == user.r_site_id_fk) || (jobRec.r_user_id_fk != null && jobRec.r_user_id_fk == user.r_id));
                }
                else // Voir uniquement pour lui meme
                {
                   return ((jobRec.r_user_id_fk != null && jobRec.r_user_id_fk == user.r_id));
                }

            }


            return false;
        }





        public static string SqlReplace(string expression, params string[] caracteres)
        {
            string resultat = $"TRIM({expression})";

            foreach (var caractere in caracteres)
            {
                var caractereSql = caractere.Replace("'", "''");
                resultat = $"REPLACE({resultat}, '{caractereSql}', '')";
            }

            return resultat;
        }
    }

}