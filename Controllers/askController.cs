using System.Data;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Reponses;
using ask.Dtos.Request.auth;
using ask.Dtos.Response;
using ask.Model;
using ask.Services;
using InteroperabiliteProject.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OracleApi.Services;
using print_attestation.Migrations;


namespace ask.Controllers
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
            if (jobType != "atd" && jobType != "cedeao")
            {
                return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Type de tâche invalide (atd|cedeao)", instance: HttpContext.Request.Path));
            }

            if (jobType == "cedeao")
            {
                await _traceService.TraceActionAsync(TYPE_ACTION.GENERATION_CEDEAO_ZIP, userId: dataUser.r_id,userEmail: dataUser.r_email,description: $"Génération de l'archive de {numAttestations.Count} cedeao : ID = {job.r_job_id}");
            }
            else
            {
                await _traceService.TraceActionAsync(TYPE_ACTION.GENERATION_ATD_ZIP, userId: dataUser.r_id, userEmail: dataUser.r_email, description: $"Génération de l'archive de {numAttestations.Count} attestation(s) : ID = {job.r_job_id}");
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

                    if (jobType == "cedeao")
                    {
                        await svc.GenerateZipCEDEAO(job);
                    }
                    else
                    {
                        await svc.GenerateZipATD(job);
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

            var user = GetInfoUser();
            if (user == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));


            var pagination = new PaginationParams(page, limit);

            var query = _dbContext.t_job.AsQueryable();

            // si pas admin, ne lister que les jobs de l'utilisateur
            if (user.r_type != TYPE_USER.Administrateur)
            {
                query = query.Where(x => x.r_user_id_fk == user.r_id);
            }

            // Filtre par statut si fourni (valeurs : RUNNING, COMPLETED, CANCELLED)
            if (status > 0)
            {
               query = query.Where(x => (int)x.r_status == status);
           }

            // Filtre par type si fourni (atd|cedeao)
            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim();
                query = query.Where(x => x.r_type == type);
            }

      
            var total = await query.CountAsync();


            var jobs = await query
                 .Include(u => u.r_user)
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
            var user = GetInfoUser();
            if (user == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var rec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == jobId);
            if (rec == null)
                return NotFound(GeneraleRetour.BuildNotFound(detail: "Job introuvable", instance: HttpContext.Request.Path));

            if (user.r_type != TYPE_USER.Administrateur && rec.r_user_id_fk != user.r_id)
                return Forbid();

            var job = _manager.Get(jobId);

            return Ok(new { record = rec, inMemory = job != null ? new { job.r_job_id, job.r_total, job.r_status, job.r_file_name } : null });
        }


        [Authorize]
        [HttpPost("attestations/jobs/{jobId}/cancel")]
        public async Task<IActionResult> StopJob(string jobId)
        {
            var user = GetInfoUser();
            if (user == null)
                return Unauthorized(GeneraleRetour.BuildUnauthorized(detail: "Utilisateur non authentifié", instance: HttpContext.Request.Path));

            var rec = await _dbContext.t_job.FirstOrDefaultAsync(x => x.r_job_id == jobId);
            if (rec == null)
                return NotFound(GeneraleRetour.BuildNotFound(detail: "Job introuvable", instance: HttpContext.Request.Path));

            if (user.r_type != TYPE_USER.Administrateur && rec.r_user_id_fk != user.r_id)
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

            rec.r_status = STATUT_JOB.CANCELLED;
            rec.r_is_active = false;
            await _dbContext.SaveChangesAsync();


            await _traceService.TraceActionAsync(TYPE_ACTION.ANNULATION_GENERATION_ZIP, userId: user.r_id, userEmail: user.r_email, description: $"Annulation de la génération de la tâche : {rec.r_job_id}");


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

                if (status == "ACTIVE")
                {
                    statutSql = " AND TRUNC(a.DATECHAT) >= TRUNC(SYSDATE)";
                }
                else if (status == "EXPIREE")
                {
                    statutSql = " AND TRUNC(a.DATECHAT) < TRUNC(SYSDATE)";
                }

                // Requête SQL pour compter le nombre total d'attestations correspondant à la recherche
                string _sqlCount = @"SELECT count(*) AS nb
                                        FROM attestation_risque a LEFT JOIN intermediaire i ON a.CODEINTE = i.CODEINTE
                                        WHERE (a.LIEN_PDF IS NOT NULL OR a.LIEN_IMG IS NOT NULL OR a.LIEN__QR IS NOT NULL)
                                          AND ( TRIM(a.NUMEIMMA) = :cleRecherche OR TRIM(a.NUMECHAS) = :cleRecherche OR TRIM(a.NUMATTDI) = :cleRecherche OR TO_CHAR(a.NUMEPOLI) = :cleRecherche OR TO_CHAR(a.CODEINTE) || '/' || TRIM(TO_CHAR(a.NUMEPOLI)) = :cleRecherche)
                                           {statutSql}";
                _sqlCount = _sqlCount.Replace("{statutSql}", statutSql);

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
                                          AND (TRIM(a.NUMEIMMA) = :cleRecherche OR TRIM(a.NUMECHAS) = :cleRecherche OR 
                                               TRIM(a.NUMATTDI) = :cleRecherche OR TO_CHAR(a.NUMEPOLI) = :cleRecherche OR 
                                               TO_CHAR(a.CODEINTE) || '/' || TRIM(TO_CHAR(a.NUMEPOLI)) = :cleRecherche)
                                           {statutSql}
                                        ORDER BY a.CREE__LE DESC, a.DATECHAT DESC, a.DATEFFAT DESC
                                    ) t
                                    WHERE ROWNUM <= :maxRow
                                )
                                WHERE rn > :offset";

                _sql = _sql.Replace("{statutSql}", statutSql);

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
                var results = rows.Select(row => new Dtos.Response.AttestationResponseDto
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
        [HttpPost("attestations/cedeao/zip")]
        public async Task<IActionResult> GetAttestationCedeaoZip([FromBody] List<string> numAttestations)
        {
            string _desc_route = "Impression des attestations Cedeao (multiple)";
            // no-op patch: ensure context consistency before adding new endpoint

            try
            {

                numAttestations = numAttestations
                   .Where(x => !string.IsNullOrWhiteSpace(x))
                   .Distinct()
                   .ToList();


                if (numAttestations == null || !numAttestations.Any())
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins un numéro d'attestation est requis", instance: HttpContext.Request.Path));


                t_user dataUser = GetInfoUser();

                var successes = new List<(string Num, byte[] Bytes)>();
                var errors = new List<string>();

                await _traceService.TraceActionAsync(
                 TYPE_ACTION.EXPORT_CEDEAO_ZIP,
                 userId: dataUser.r_id,
                 userEmail: dataUser.r_email,
                 description: $"Impression de {numAttestations.Count} attestation(s) Cedeao ");

                foreach (var num in numAttestations)
                {
                    if (string.IsNullOrWhiteSpace(num))
                    {
                        errors.Add($"{num}: numéro vide");
                        continue;
                    }

                    try
                    {
                        var result = await _ServiceAsaci.printCedeao(num);
                        if (result.status != 200)
                        {
                            errors.Add($"{num}: statut {result.status} - {result.detail}");
                            continue;
                        }

                        var res_data = JsonConvert.DeserializeObject<dynamic>(result.data);
                        string base64Image = res_data.base64?.ToString();
                        if (string.IsNullOrWhiteSpace(base64Image))
                        {
                            errors.Add($"{num}: image Base64 manquante");
                            continue;
                        }

                        byte[] imageBytes = Tools.Tools.ConvertBase64ToImageBytes(base64Image);
                        successes.Add((num, imageBytes));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors de la récupération Cedeao pour {Num}", num);
                        errors.Add($"{num}: exception - {ex.Message}");
                    }
                }

                if (!successes.Any() && errors.Any())
                {
                    return StatusCode(424, GeneraleRetour.BuildProblemResponse(new GeneraleRetour { status = 424, detail = "Aucune attestation récupérée" }, instance: HttpContext.Request.Path));
                }

                using var ms = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var s in successes)
                    {

                        var entry = archive.CreateEntry($"Cedeao_{s.Num}.png", System.IO.Compression.CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(s.Bytes, 0, s.Bytes.Length);
                    }

                    if (errors.Any())
                    {
                        var entry = archive.CreateEntry("errors.txt", System.IO.Compression.CompressionLevel.Fastest);
                        using var writer = new System.IO.StreamWriter(entry.Open());
                        foreach (var e in errors)
                        {
                            await writer.WriteLineAsync(e);
                        }
                        await writer.FlushAsync();
                    }
                }

                ms.Position = 0;
                var fileName = $"Cedeao_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
                return File(ms.ToArray(), "application/zip", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));

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

                t_user dataUser = GetInfoUser();


                await _traceService.TraceActionAsync(
               TYPE_ACTION.TELECHARGEMENT_CEDEAO,
               userId: dataUser.r_id,
               userEmail: dataUser.r_email,
               description: $"Impression de l'attestation Cedeao : {numAttestation}");

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
        [HttpPost("attestations/atd/zip")]
        public async Task<IActionResult> GetAttestationsZip([FromBody] List<string> numAttestations)
        {
            string _desc_route = "Télécharger les attestations (zip)";

            try
            {


                numAttestations = numAttestations
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();


                if (numAttestations == null || !numAttestations.Any())
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins un numéro d'attestation est requis", instance: HttpContext.Request.Path));


                var successes = new List<(string FileName, byte[] Bytes)>();
                var errors = new List<string>();

                t_user dataUser = GetInfoUser();

                await _traceService.TraceActionAsync(
               TYPE_ACTION.EXPORT_ATD_ZIP,
               userId: dataUser.r_id,
               userEmail: dataUser.r_email,
               description: $"Téléchargement des attestations : {numAttestations.Count} attestation(s)");


                var numList = numAttestations.ToList();

                // Génération des paramètres Oracle
                var parameters = new Dictionary<string, object>();
                var placeholders = new List<string>();
                for (int i = 0; i < numList.Count; i++)
                {
                    string paramName = $":num{i}";

                    placeholders.Add(paramName);

                    parameters.Add(paramName, numList[i]);
                }


                // Une seule requête Oracle
                string sql = $@"SELECT  LIEN_PDF, LIEN_IMG,LIEN__QR, NUMATTDI
                       FROM attestation_risque
                            WHERE NUMATTDI IN ({string.Join(",", placeholders)})";


                var rows = await _oracleService.ExecuteQueryAsync(sql, parameters);


                // Indexation par numéro pour accès rapide
                var attestations = rows.ToDictionary(
                    x => x["NUMATTDI"]?.ToString(),
                    x => x
                );

                int current = 0;

                foreach (var num in numList)
                {
                    current++;

                    _logger.LogError(current + "===========>" + num);

                    try
                    {

                        if (!attestations.TryGetValue(num, out var row))
                        {
                            errors.Add($"{num}: attestation introuvable");
                        }

                        string? path = row["LIEN_PDF"]?.ToString() ?? row["LIEN_IMG"]?.ToString() ?? row["LIEN__QR"]?.ToString();

                        if (string.IsNullOrEmpty(path))
                        {
                            errors.Add($"{num}: fichier absent");
                            continue;
                        }


                        byte[] bytes;


                        if (path.StartsWith("http"))
                        {
                            using var client = new HttpClient();
                            bytes = await client.GetByteArrayAsync(path);
                        }
                        else
                        {

                            if (!Path.IsPathRooted(path))
                            {
                                path = Path.Combine(
                                    _env.WebRootPath,
                                    path.TrimStart('/')
                                );
                            }


                      

                            bytes = await System.IO.File.ReadAllBytesAsync(path);
                        }

                        successes.Add(($"Attestation_{num}.png", bytes));

                    }
                    catch (Exception ex)
                    {

                        errors.Add($"{num}:{ex.Message}");
                        _logger.LogWarning(ex, "Erreur lors de la récupération attestation pour {Num}", num);

                    }
                }

                if (!successes.Any() && errors.Any())
                {
                    return StatusCode(424, GeneraleRetour.BuildProblemResponse(new GeneraleRetour { status = 424, detail = "Aucune attestation récupérée" }, instance: HttpContext.Request.Path));
                }

                using var ms = new MemoryStream();
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var s in successes)
                    {
                        var entry = archive.CreateEntry(s.FileName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(s.Bytes, 0, s.Bytes.Length);
                    }

                    if (errors.Any())
                    {
                        var entry = archive.CreateEntry("errors.txt", CompressionLevel.Fastest);
                        using var writer = new System.IO.StreamWriter(entry.Open());
                        foreach (var e in errors)
                        {
                            await writer.WriteLineAsync(e);
                        }
                        await writer.FlushAsync();
                    }
                }

                ms.Position = 0;
                var fileNameZip = $"Attestations_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
                return File(ms.ToArray(), "application/zip", fileNameZip);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

     
        [Authorize]
        [HttpPost("attestations/cedeao/zip/sse")]
        public async Task GetAttestationCedeaoZipSse([FromBody] List<string> numAttestations)
        {
            HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
            HttpContext.Response.Headers.Append("Connection", "keep-alive");

            string _desc_route = "Impression des attestations Cedeao SSE";


            numAttestations = numAttestations
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();


            async Task SendEvent(string type, object data)
            {
                var json = JsonConvert.SerializeObject(data);

                await HttpContext.Response.WriteAsync(
                    $"event: {type}\n" +
                    $"data: {json}\n\n"
                );

                await HttpContext.Response.Body.FlushAsync();
            }

            try
            {
                if (numAttestations == null || !numAttestations.Any())
                {
                    await SendEvent("error", new
                    {
                        message = "Au moins un numéro d'attestation est requis"
                    });

                    return;
                }


                t_user dataUser = GetInfoUser();


                await _traceService.TraceActionAsync(
                    TYPE_ACTION.EXPORT_CEDEAO_ZIP,
                    userId: dataUser.r_id,
                    userEmail: dataUser.r_email,
                    description: $"Impression de {numAttestations.Count} attestation(s) Cedeao"
                );


                await SendEvent("start", new
                {
                    total = numAttestations.Count
                });


                var successes = new List<(string Num, byte[] Bytes)>();
                var errors = new List<string>();


                int index = 0;


                foreach (var num in numAttestations)
                {
                    index++;

                    await SendEvent("progress", new
                    {
                        current = index,
                        total = numAttestations.Count,
                        numero = num
                    });


                    if (string.IsNullOrWhiteSpace(num))
                    {
                        errors.Add($"{num}: numéro vide");

                        await SendEvent("error", new
                        {
                            numero = num,
                            message = "Numéro vide"
                        });

                        continue;
                    }


                    try
                    {
                        var result = await _ServiceAsaci.printCedeao(num);


                        if (result.status != 200)
                        {
                            errors.Add($"{num}: {result.detail}");

                            await SendEvent("error", new
                            {
                                numero = num,
                                message = result.detail
                            });

                            continue;
                        }


                        var res_data =
                            JsonConvert.DeserializeObject<dynamic>(result.data);


                        string base64Image = res_data.base64?.ToString();


                        if (string.IsNullOrWhiteSpace(base64Image))
                        {
                            errors.Add($"{num}: Base64 manquant");

                            await SendEvent("error", new
                            {
                                numero = num,
                                message = "Image Base64 manquante"
                            });

                            continue;
                        }


                        byte[] imageBytes =
                            Tools.Tools.ConvertBase64ToImageBytes(base64Image);


                        successes.Add((num, imageBytes));


                        await SendEvent("success", new
                        {
                            numero = num,
                            message = "Attestation récupérée"
                        });


                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{num}: {ex.Message}");

                        await SendEvent("error", new
                        {
                            numero = num,
                            message = ex.Message
                        });
                    }
                }



                if (!successes.Any())
                {
                    await SendEvent("complete", new
                    {
                        success = false,
                        message = "Aucune attestation générée"
                    });

                    return;
                }



                // Génération ZIP

                using var ms = new MemoryStream();


                using (var archive = new ZipArchive(
                    ms,
                    ZipArchiveMode.Create,
                    true))
                {

                    foreach (var item in successes)
                    {
                        var entry = archive.CreateEntry(
                            $"Cedeao_{item.Num}.png",
                            CompressionLevel.Optimal);


                        using var entryStream = entry.Open();

                        await entryStream.WriteAsync(
                            item.Bytes,
                            0,
                            item.Bytes.Length);
                    }


                    if (errors.Any())
                    {
                        var entry = archive.CreateEntry("errors.txt");

                        using var writer = new StreamWriter(entry.Open());

                        foreach (var error in errors)
                            await writer.WriteLineAsync(error);

                        await writer.FlushAsync();
                    }
                }



                var fileName =
                    $"Cedeao_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";


                var path = Path.Combine(
                    Path.GetTempPath(),
                    fileName);


                await System.IO.File.WriteAllBytesAsync(
                    path,
                    ms.ToArray());



                await SendEvent("complete", new
                {
                    success = true,
                    file = fileName,
                    url = $"/api/download/{fileName}",
                    total = successes.Count,
                    errors = errors.Count
                });


            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"[EndPoint {_desc_route}]");


                await SendEvent("error", new
                {
                    message = "Erreur serveur"
                });
            }
        }



        [Authorize]
        [HttpPost("attestations/atd/zip/sse")]
        public async Task GetAttestationsZipSse([FromBody] List<string> numAttestations)
        {
            string _desc_route = "Téléchargement attestations ZIP SSE";


            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";


            async Task SendEvent(string type, object data)
            {
                var json = JsonConvert.SerializeObject(data);

                await Response.WriteAsync(
                    $"event: {type}\n" +
                    $"data: {json}\n\n"
                );

                await Response.Body.FlushAsync();
            }


            try
            {

                numAttestations = numAttestations?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();


                if (numAttestations == null || !numAttestations.Any())
                {
                    await SendEvent("error", new
                    {
                        message = "Au moins un numéro d'attestation est requis"
                    });

                    return;
                }


                t_user dataUser = GetInfoUser();


                await _traceService.TraceActionAsync(
                    TYPE_ACTION.EXPORT_ATD_ZIP,
                    userId: dataUser.r_id,
                    userEmail: dataUser.r_email,
                    description:
                    $"Téléchargement de {numAttestations.Count} attestation(s)"
                );


                await SendEvent("start", new
                {
                    total = numAttestations.Count
                });



                var successes =
                    new List<(string Num, byte[] Bytes, string FileName)>();

                var errors =
                    new List<string>();


                using var httpClient = new HttpClient();


                int current = 0;


                foreach (var num in numAttestations)
                {

                    current++;


                    await SendEvent("progress", new
                    {
                        current,
                        total = numAttestations.Count,
                        numero = num
                    });



                    try
                    {

                        string sql = @"SELECT LIEN_PDF,LIEN_IMG,LIEN__QR,NUMATTDI 
                                       FROM attestation_risque 
                                       WHERE ( LIEN_PDF IS NOT NULL  OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
                                           AND  ( NUMATTDI = :num OR NUMEIMMA = :num 
                                                  OR NUMECHAS = :num OR TO_CHAR(NUMEPOLI)=:num 
                                                  OR (TO_CHAR(CODEINTE)||'/'||TO_CHAR(NUMEPOLI))=:num)";


                        var parameters = new Dictionary<string, object>
                            {{":num", num}};

                        var rows =
                            await _oracleService.ExecuteQueryAsync(
                                sql,
                                parameters
                            );


                        if (!rows.Any())
                        {
                            errors.Add(
                                $"{num}: attestation introuvable"
                            );


                            await SendEvent("error", new
                            {
                                numero = num,
                                message = "Attestation introuvable"
                            });

                            continue;
                        }



                        var row = rows[0];


                        string selected = row["LIEN_PDF"]?.ToString() ?? row["LIEN_IMG"]?.ToString() ?? row["LIEN__QR"]?.ToString();



                        if (string.IsNullOrWhiteSpace(selected))
                        {

                            errors.Add(
                                $"{num}: aucun lien disponible"
                            );


                            await SendEvent("error", new
                            {
                                numero = num,
                                message = "Aucun lien disponible"
                            });

                            continue;
                        }



                        byte[] data = null;

                        string ext = ".png";



                        if (selected.StartsWith("data:",
                            StringComparison.OrdinalIgnoreCase))
                        {

                            var parts =
                                selected.Split(',', 2);


                            data =
                                Convert.FromBase64String(parts[1]);


                            if (parts[0].Contains("pdf"))
                                ext = ".pdf";

                        }


                        else if (selected.StartsWith("http",
                            StringComparison.OrdinalIgnoreCase))
                        {

                            data =
                                await httpClient
                                .GetByteArrayAsync(selected);


                            ext =
                                Path.GetExtension(
                                    new Uri(selected).LocalPath
                                );

                        }


                        else
                        {

                            string path = selected;


                            if (!Path.IsPathRooted(path))
                            {
                                path =
                                Path.Combine(
                                    _env.WebRootPath,
                                    selected.TrimStart('/')
                                );
                            }


                            if (System.IO.File.Exists(path))
                            {
                                data =
                                await System.IO.File.ReadAllBytesAsync(path);

                                ext =
                                Path.GetExtension(path);
                            }

                        }



                        if (data == null || data.Length == 0)
                        {

                            errors.Add(
                                $"{num}: fichier vide"
                            );


                            await SendEvent("error", new
                            {
                                numero = num,
                                message = "Fichier vide"
                            });


                            continue;
                        }


                        ext = ".png";
                        successes.Add(
                            (
                            num,
                            data,
                            $"Attestation_{num}{ext}"
                            )
                        );



                        await SendEvent("success", new
                        {
                            numero = num,
                            message = "Attestation récupérée"
                        });


                    }
                    catch (Exception ex)
                    {

                        errors.Add(
                            $"{num}: {ex.Message}"
                        );


                        await SendEvent("error", new
                        {
                            numero = num,
                            message = ex.Message
                        });

                    }

                }



                if (!successes.Any())
                {

                    await SendEvent("complete", new
                    {
                        success = false,
                        message = "Aucune attestation générée"
                    });

                    return;
                }




                // Création ZIP

                using var ms = new MemoryStream();


                using (var archive =
                    new ZipArchive(
                        ms,
                        ZipArchiveMode.Create,
                        true))
                {

                    foreach (var item in successes)
                    {

                        var entry =
                            archive.CreateEntry(
                                item.FileName,
                                CompressionLevel.Optimal);


                        using var stream =
                            entry.Open();


                        await stream.WriteAsync(
                            item.Bytes,
                            0,
                            item.Bytes.Length
                        );
                    }



                    if (errors.Any())
                    {

                        var entry =
                            archive.CreateEntry(
                                "errors.txt");


                        using var writer =
                            new StreamWriter(
                                entry.Open());


                        foreach (var error in errors)
                            await writer.WriteLineAsync(error);


                        await writer.FlushAsync();

                    }

                }




                var fileNameZip =
                    $"Attestations_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";


                var pathZip =
                    Path.Combine(
                        Path.GetTempPath(),
                        fileNameZip);



                await System.IO.File.WriteAllBytesAsync(
                    pathZip,
                    ms.ToArray()
                );



                await SendEvent("complete",
                new
                {
                    success = true,
                    url = $"/api/download/{fileNameZip}",
                    file = fileNameZip,
                    total = successes.Count,
                    errors = errors.Count
                });


            }
            catch (Exception ex)
            {

                _logger.LogError(ex,
                    $"[{_desc_route}]");


                await SendEvent("error",
                new
                {
                    message = "Erreur serveur"
                });
            }
        }


        [Authorize]
        [HttpGet("attestations/download/{fileName}")]
       
        public async Task<IActionResult> DownloadZip(string fileName)
        {
            var path = Path.Combine(Path.GetTempPath(), fileName);
            string _desc_route = "Télécharger le fichier ZIP";

            t_user userConnecte = GetInfoUser();

            if (!System.IO.File.Exists(path))
            {
                return NotFound("Fichier introuvable.");
            }


            await _traceService.TraceActionAsync(TYPE_ACTION.TELECHARGEMENT_ZIP, userId: userConnecte.r_id, userEmail: userConnecte.r_email, description: $"Téléchargement de l'archive {fileName}");



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
        [NonAction]
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



    }

}