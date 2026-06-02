using ask.ContextDb;
using ask.Dtos.General;
using ask.Services;
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
        private readonly IDbContextFactory<askContext> _dbFactory;
        private readonly IOracleService _oracleService;


        //private readonly ILogger _logger;
        public askController(IDbContextFactory<askContext> dbFactory,askContext askContext, ServiceAsaci ServiceAsaci, IOptions<ParamMessage> paramdata, IConfiguration configuration, IWebHostEnvironment env, ILogger<askController> logger, IOracleService oracleService)
        {

            _configuration = configuration;
            _ServiceAsaci = ServiceAsaci;
            _dbFactory = dbFactory;
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


      
        [HttpGet("attestations/{cleRecherche}")]
        public async Task<IActionResult> GetAttestation(string cleRecherche)
        {
            string _desc_route = "Obtenir une attestation";

            try
            {
                if (string.IsNullOrWhiteSpace(cleRecherche))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Le numéro de l'attestation est requis", instance: HttpContext.Request.Path));

                // Validation de sécurité pour éviter les injections SQL
                if (!Tools.Tools.IsValidSearchKey(cleRecherche))
                    return BadRequest(GeneraleRetour.BuildBadRequest(detail: "Format de recherche invalide", instance: HttpContext.Request.Path));

                // Requête SQL sécurisée avec paramètre
                string _sql = @"
                    SELECT NUMEPOLI, DATEFFAT, DATECHAT, MARQVEHI, TYPEVEHI, 
                           NUMEIMMA, NUMECHAS, LIBERISQ, NUMATTDI,LIEN_PDF,LIEN__QR,LIEN_IMG
                    FROM attestation_risque
                    WHERE (LIEN_PDF IS NOT NULL OR LIEN_IMG IS NOT NULL OR LIEN__QR IS NOT NULL)
                      AND (NUMEIMMA = :cleRecherche OR NUMECHAS = :cleRecherche OR NUMATTDI = :cleRecherche OR TO_CHAR(NUMEPOLI) = :cleRecherche)";

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

    }
}