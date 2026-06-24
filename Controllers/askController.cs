using System.Net;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Dtos.Request.auth;
using ask.Dtos.Response.auth;
using ask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OracleApi.Services;
using PdfSharp.Diagnostics;

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


        [HttpGet("attestations/{cleRechercheEncode}")]
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

                int offset = (page - 1) * pageSize;

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

                // Requête paginée (Oracle 12c+)
                string pageSql = @"
            SELECT (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) NUMEPOLI, DATEFFAT, DATECHAT, MARQVEHI, TYPEVEHI, 
                   NUMEIMMA, NUMECHAS, LIBERISQ, NUMATTDI, LIEN_PDF, LIEN__QR, LIEN_IMG, CODEINTE
            FROM attestation_risque
            WHERE (LIEN_PDF IS NOT NULL OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
              AND (NUMEIMMA = :cleRecherche OR 
                   NUMECHAS = :cleRecherche OR 
                   NUMATTDI = :cleRecherche OR 
                   TO_CHAR(NUMEPOLI) = :cleRecherche OR 
                   (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) = :cleRecherche)
            ORDER BY DATECHAT DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY";

                var pageParams = new Dictionary<string, object>
        {
            { ":cleRecherche", cleRechercheEncode },
            { ":offset", offset },
            { ":pageSize", pagination.limit }
        };

                var rows = await _oracleService.ExecuteQueryAsync(pageSql, pageParams);

                var items = rows.Select(row => new
                {
                    numPolice = row.ContainsKey("NUMEPOLI") ? row["NUMEPOLI"]?.ToString() : null,
                    dateEffet = row.ContainsKey("DATEFFAT") ? row["DATEFFAT"] : null,
                    dateEcheance = row.ContainsKey("DATECHAT") ? row["DATECHAT"] : null,
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

              


                return Ok(PaginatedResponse<UserResponseDto>.Create(items, total, page, limit));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpGet("attestationsX/{cleRechercheEncode}")]
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
                string _sql = @"
                    SELECT (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) NUMEPOLI, DATEFFAT, DATECHAT, MARQVEHI, TYPEVEHI, 
                           NUMEIMMA, NUMECHAS, LIBERISQ, NUMATTDI,LIEN_PDF,LIEN__QR,LIEN_IMG,CODEINTE
                    FROM attestation_risque
                    WHERE (LIEN_PDF IS NOT NULL OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
                      AND (NUMEIMMA = :cleRecherche OR 
                           NUMECHAS = :cleRecherche OR 
                           NUMATTDI = :cleRecherche OR 
                           TO_CHAR(NUMEPOLI) = :cleRecherche OR 
                        (TO_CHAR(CODEINTE) || '/' || TO_CHAR(NUMEPOLI)) = :cleRecherche)
                      ORDER BY DATECHAT DESC";

                // Utilisation du service Oracle avec paramètres
                var parameters = new Dictionary<string, object>
                {
                    { ":cleRecherche", cleRecherche }
                };

                var rows = await _oracleService.ExecuteQueryAsync(_sql, parameters);

                if (!rows.Any())
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));

                // Mapper les résultats
                var results = rows.Select(row => new
                {
                    numPolice = row.ContainsKey("NUMEPOLI") ? row["NUMEPOLI"]?.ToString() : null,
                    dateEffet = row.ContainsKey("DATEFFAT") ? row["DATEFFAT"] : null,
                    dateEcheance = row.ContainsKey("DATECHAT") ? row["DATECHAT"] : null,
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


                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }

        [Authorize]
        [HttpGet("attestations/{numAttestation}/cedeao/print")]
        public async Task<IActionResult> PrintAttestationCedeao(string numAttestation)
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
                string base64Image = res_data.base64?.ToString();

                if (string.IsNullOrWhiteSpace(base64Image))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "L'image Base64 est manquante dans la réponse", instance: HttpContext.Request.Path));

                byte[] imageBytes = ConvertBase64ToImageBytes(base64Image);

                // Retourner l'image directement
                return File(imageBytes, "image/png", $"Cedeao_{numAttestation}.png");
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

    }
}