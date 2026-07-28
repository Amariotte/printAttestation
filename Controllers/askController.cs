using System.Data;
using System.IO.Compression;
using System.Net;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Response;
using ask.Model;
using ask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OracleApi.Services;

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

     
        //private readonly ILogger _logger;
        public askController(askContext askContext, TraceService traceService, ServiceAsaci ServiceAsaci, IOptions<ParamMessage> paramdata, IConfiguration configuration, IWebHostEnvironment env, ILogger<askController> logger, IOracleService oracleService)
        {

            _configuration = configuration;
            _ServiceAsaci = ServiceAsaci;
            _env = env;
            _paramdata = paramdata.Value;
            _logger = logger;
            _dbContext = askContext;
            _oracleService = oracleService;
            _traceService = traceService;


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

        /// <summary>
        /// Convertit une chaîne Base64 en tableau de bytes (image)
        /// </summary>
        [NonAction]
        public byte[] ConvertBase64ToImageBytes(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentException("La chaîne Base64 ne peut pas être vide", nameof(base64String));

            // Nettoyer le préfixe data:image si présent
            if (base64String.Contains(","))
            {
                base64String = base64String.Split(',')[1];
            }

            return Convert.FromBase64String(base64String);
        }

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
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
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

                        byte[] imageBytes = ConvertBase64ToImageBytes(base64Image);
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

                var successes = new List<(string Num, byte[] Bytes, string FileName)>();
                var errors = new List<string>();

                t_user dataUser = GetInfoUser();

                await _traceService.TraceActionAsync(
               TYPE_ACTION.EXPORT_ATD_ZIP,
               userId: dataUser.r_id,
               userEmail: dataUser.r_email,
               description: $"Téléchargement des attestations : {numAttestations.Count} attestation(s)");

                using var httpClient = new HttpClient();

                foreach (var num in numAttestations)
                {
                    if (string.IsNullOrWhiteSpace(num))
                    {
                        errors.Add($"{num}: numéro vide");
                        continue;
                    }

                    try
                    {
                        string sql = @"SELECT LIEN_PDF, LIEN_IMG, LIEN__QR, NUMATTDI FROM attestation_risque
WHERE (LIEN_PDF IS NOT NULL OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
  AND (NUMATTDI = :num OR NUMEIMMA = :num OR NUMECHAS = :num OR TO_CHAR(NUMEPOLI) = :num OR (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) = :num)";

                        var parameters = new Dictionary<string, object> { { ":num", num } };
                        var rows = await _oracleService.ExecuteQueryAsync(sql, parameters);
                        if (!rows.Any())
                        {
                            errors.Add($"{num}: attestation introuvable");
                            continue;
                        }

                        var row = rows[0];
                        var lienPdf = row.ContainsKey("LIEN_PDF") ? row["LIEN_PDF"]?.ToString() : null;
                        var lienImg = row.ContainsKey("LIEN_IMG") ? row["LIEN_IMG"]?.ToString() : null;
                        var lienQr = row.ContainsKey("LIEN__QR") ? row["LIEN__QR"]?.ToString() : null;

                        string selected = lienPdf ?? lienImg ?? lienQr;
                        if (string.IsNullOrWhiteSpace(selected))
                        {
                            errors.Add($"{num}: aucun lien disponible");
                            continue;
                        }

                        byte[] data = null;
                        string ext = ".png";

                        if (selected.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            // data:[mime];base64,xxxxx
                            var parts = selected.Split(',', 2);
                            if (parts.Length == 2)
                            {
                                data = Convert.FromBase64String(parts[1]);
                                if (parts[0].Contains("pdf")) ext = ".pdf";
                                else if (parts[0].Contains("png") || parts[0].Contains("image")) ext = ".png";
                            }
                        }
                        else if (selected.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                data = await httpClient.GetByteArrayAsync(selected);
                                // try to infer extension from url
                                var uri = new Uri(selected);
                                var seg = Path.GetFileName(uri.LocalPath);
                                if (!string.IsNullOrWhiteSpace(seg) && seg.Contains('.'))
                                    ext = Path.GetExtension(seg);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Erreur téléchargement lien pour {Num}", num);
                                errors.Add($"{num}: échec téléchargement");
                                continue;
                            }
                        }
                        else
                        {
                            // treat as local file path relative to wwwroot or absolute
                            string path = selected;
                            if (!Path.IsPathRooted(path))
                                path = Path.Combine(_env.WebRootPath ?? string.Empty, selected.TrimStart('/', '\\'));

                            if (System.IO.File.Exists(path))
                            {
                                data = await System.IO.File.ReadAllBytesAsync(path);
                                ext = Path.GetExtension(path);
                            }
                            else
                            {
                                errors.Add($"{num}: fichier introuvable: {selected}");
                                continue;
                            }
                        }

                        if (data == null || data.Length == 0)
                        {
                            errors.Add($"{num}: contenu vide");
                            continue;
                        }

                        var fileName = $"Attestation_{num}{ext}";
                        successes.Add((num, data, fileName));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors de la récupération attestation pour {Num}", num);
                        errors.Add($"{num}: exception - {ex.Message}");
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
                            ConvertBase64ToImageBytes(base64Image);


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
            var path = Path.Combine(
                Path.GetTempPath(),
                fileName
            );


            if (!System.IO.File.Exists(path))
            {
                return NotFound("Fichier introuvable");
            }


            var bytes = await System.IO.File.ReadAllBytesAsync(path);


            return File(
                bytes,
                "application/zip",
                fileName
            );
        }
    }
}