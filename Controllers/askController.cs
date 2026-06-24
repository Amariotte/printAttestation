using System.Net;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Response;
using ask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.Http;
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


        //private readonly ILogger _logger;
        public askController(askContext askContext, ServiceAsaci ServiceAsaci, IOptions<ParamMessage> paramdata, IConfiguration configuration, IWebHostEnvironment env, ILogger<askController> logger, IOracleService oracleService)
        {

            _configuration = configuration;
            _ServiceAsaci = ServiceAsaci;
            _env = env;
            _paramdata = paramdata.Value;
            _logger = logger;
            _dbContext = askContext;
            _oracleService = oracleService;

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


        [HttpGet("attestationsX/{cleRechercheEncode}")]
        public async Task<IActionResult> GetAttestation(string cleRechercheEncode,[FromQuery] int page = 1,[FromQuery] int limit = 10)
        {
            string _desc_route = "Obtenir une attestation";

      
                try
                {

                var pagination = new PaginationParams(page, limit);

                cleRechercheEncode = WebUtility.UrlDecode(cleRechercheEncode);

                if (string.IsNullOrWhiteSpace(cleRechercheEncode))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de l'attestation est requis", instance: HttpContext.Request.Path));

                if (!Tools.Tools.IsValidSearchKey(cleRechercheEncode))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Format de recherche invalide", instance: HttpContext.Request.Path));

                int offset = (page - 1) * pagination.limit;

                // Requête pour le total
                string countSql = @"
            SELECT COUNT(*) AS TOTAL
            FROM attestation_risque
            WHERE (LIEN_PDF IS NOT NULL OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
              AND (NUMEIMMA = :cleRecherche OR 
                   NUMECHAS = :cleRecherche OR 
                   NUMATTDI = :cleRecherche OR 
                   TO_CHAR(NUMEPOLI) = :cleRecherche OR 
                   (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) = :cleRecherche)";

                var countParams = new Dictionary<string, object> { { ":cleRecherche", cleRechercheEncode } };
                var countRows = await _oracleService.ExecuteQueryAsync(countSql, countParams);
                int total = 0;
                if (countRows.Any() && countRows[0].ContainsKey("TOTAL"))
                    int.TryParse(countRows[0]["TOTAL"]?.ToString(), out total);

                if (total == 0)
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));



                // Pagination compatible Oracle 11g using ROWNUM
                string pageSql = @"
                            SELECT * FROM (
                                          SELECT t.*, ROWNUM rn
                                                  FROM (
    SELECT TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI) AS NUMEPOLI, DATEFFAT, DATECHAT, MARQVEHI, TYPEVEHI,
           NUMEIMMA, NUMECHAS, LIBERISQ, NUMATTDI, LIEN_PDF, LIEN__QR, LIEN_IMG, CODEINTE
    FROM attestation_risque
    WHERE (LIEN_PDF IS NOT NULL OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
      AND (NUMEIMMA = :cleRecherche 
           OR NUMECHAS = :cleRecherche 
           OR NUMATTDI = :cleRecherche 
           OR TO_CHAR(NUMEPOLI) = :cleRecherche 
           OR (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) = :cleRecherche)
    ORDER BY DATECHAT DESC
                     ) t
               WHERE ROWNUM <= :maxRow
                       )
             WHERE rn > :offset";


                var pageParams = new Dictionary<string, object>
                {
                    { ":cleRecherche", cleRechercheEncode },
                    { ":offset", offset },
                    { ":maxRow", (offset + pagination.limit) }
                };


                var rows = await _oracleService.ExecuteQueryAsync(pageSql, pageParams);


                var items = rows.Select(row => new Dtos.Response.AttestationResponseDto
                {
                    numPolice = row.ContainsKey("NUMEPOLI") ? row["NUMEPOLI"]?.ToString() : null,
                    dateEffet = row.ContainsKey("DATEFFAT") ? (row["DATEFFAT"] as DateTime?) : null,
                    dateEcheance = row.ContainsKey("DATECHAT") ? (row["DATECHAT"] as DateTime?) : null,
                    marqueVehicule = row.ContainsKey("MARQVEHI") ? row["MARQVEHI"]?.ToString() : null,
                    typeVehicule = row.ContainsKey("TYPEVEHI") ? row["TYPEVEHI"]?.ToString() : null,
                    numImmatriculation = row.ContainsKey("NUMEIMMA") ? row["NUMEIMMA"]?.ToString() : null,
                    numChassis = row.ContainsKey("NUMECHAS") ? row["NUMECHAS"]?.ToString() : null,
                    nomAssure = row.ContainsKey("LIBERISQ") ? row["LIBERISQ"]?.ToString() : null,
                    numAttestation = row.ContainsKey("NUMATTDI") ? row["NUMATTDI"]?.ToString() : null,
                    urlPdf = row.ContainsKey("LIEN_PDF") ? row["LIEN_PDF"]?.ToString() : null,
                    urlQr = row.ContainsKey("LIEN__QR") ? row["LIEN__QR"]?.ToString() : null,
                    urlImage = row.ContainsKey("LIEN_IMG") ? row["LIEN_IMG"]?.ToString() : null,
                }).ToList();

              


                return Ok(PaginatedResponse<AttestationResponseDto>.Create(items, total, page, pagination.limit));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpGet("attestations/{cleRechercheEncode}")]
        public async Task<IActionResult> GetAttestation(string cleRechercheEncode)
        {
            string _desc_route = "Obtenir une attestation";

            try
            {

                string cleRecherche =  WebUtility.UrlDecode(cleRechercheEncode);

                if (string.IsNullOrWhiteSpace(cleRecherche))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de l'attestation est requis", instance: HttpContext.Request.Path));

                // Validation de sécurité pour éviter les injections SQL
                if (!Tools.Tools.IsValidSearchKey(cleRecherche))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Format de recherche invalide", instance: HttpContext.Request.Path));

                // Requête SQL sécurisée avec paramètre
                string _sql = @"SELECT TO_CHAR(a.CODEINTE) || '/' || TO_CHAR(a.NUMEPOLI) AS NUMEPOLI,a.DATEFFAT,a.DATECHAT,a.MARQVEHI,a.TYPEVEHI,a.NUMEIMMA,a.NUMECHAS,a.LIBERISQ,a.NUMATTDI,a.LIEN_PDF,a.LIEN__QR,a.LIEN_IMG,a.CODEINTE,i.RAISOCIN
                                       FROM attestation_risque a LEFT JOIN intermediaire i ON a.CODEINTE = i.CODEINTE
                                       WHERE (a.LIEN_PDF IS NOT NULL OR a.LIEN_IMG IS NOT NULL OR a.LIEN__QR IS NOT NULL)
                                         AND ( a.NUMEIMMA = :cleRecherche OR a.NUMECHAS = :cleRecherche OR a.NUMATTDI = :cleRecherche OR TO_CHAR(a.NUMEPOLI) = :cleRecherche OR TO_CHAR(a.CODEINTE) || '/' || TO_CHAR(a.NUMEPOLI) = :cleRecherche)
                                         AND TRUNC(a.DATECHAT) >= TRUNC(SYSDATE)
                                         ORDER BY a.DATECHAT DESC;";

                // Utilisation du service Oracle avec paramètres
                var parameters = new Dictionary<string, object>
                {
                    { ":cleRecherche", cleRecherche }
                };

                var rows = await _oracleService.ExecuteQueryAsync(_sql, parameters);

                if (!rows.Any())
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));

                // Mapper les résultats
                var results = rows.Select(row => new Dtos.Response.AttestationResponseDto
                {
                    numPolice = row.ContainsKey("NUMEPOLI") ? row["NUMEPOLI"]?.ToString() : null,
                    dateEffet = row.ContainsKey("DATEFFAT") ? (row["DATEFFAT"] as DateTime?) : null,
                    dateEcheance = row.ContainsKey("DATECHAT") ? (row["DATECHAT"] as DateTime?) : null,
                    marqueVehicule = row.ContainsKey("MARQVEHI") ? row["MARQVEHI"]?.ToString() : null,
                    typeVehicule = row.ContainsKey("TYPEVEHI") ? row["TYPEVEHI"]?.ToString() : null,
                    numImmatriculation = row.ContainsKey("NUMEIMMA") ? row["NUMEIMMA"]?.ToString() : null,
                    numChassis = row.ContainsKey("NUMECHAS") ? row["NUMECHAS"]?.ToString() : null,
                    nomAssure = row.ContainsKey("LIBERISQ") ? row["LIBERISQ"]?.ToString() : null,
                    numAttestation = row.ContainsKey("NUMATTDI") ? row["NUMATTDI"]?.ToString() : null,
                    urlPdf = row.ContainsKey("LIEN_PDF") ? row["LIEN_PDF"]?.ToString() : null,
                    urlQr = row.ContainsKey("LIEN__QR") ? row["LIEN__QR"]?.ToString() : null,
                    urlImage = row.ContainsKey("LIEN_IMG") ? row["LIEN_IMG"]?.ToString() : null,
                    nomIntermediaire = row.ContainsKey("RAISOCIN") ? row["RAISOCIN"].ToString() : null,

                }).ToList();


                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

 

        [Authorize]
        [HttpPost("attestations/cedeao")]
        public async Task<IActionResult> GetAttestationMultipleCedeao([FromBody] List<string> numAttestations)
        {
            string _desc_route = "Impression des attestations Cedeao (multiple)";
            // no-op patch: ensure context consistency before adding new endpoint

            try
            {
                if (numAttestations == null || !numAttestations.Any())
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins un numéro d'attestation est requis", instance: HttpContext.Request.Path));

                var successes = new List<(string Num, byte[] Bytes)>();
                var errors = new List<string>();

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
        [HttpPost("attestations/zip")]
        public async Task<IActionResult> GetAttestationsZip([FromBody] List<string> numAttestations)
        {
            string _desc_route = "Télécharger les attestations (zip)";

            try
            {
                if (numAttestations == null || !numAttestations.Any())
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Au moins un numéro d'attestation est requis", instance: HttpContext.Request.Path));

                var successes = new List<(string Num, byte[] Bytes, string FileName)>();
                var errors = new List<string>();

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


    }
}