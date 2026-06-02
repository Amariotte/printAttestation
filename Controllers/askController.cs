using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Model;
using ask.Services;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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


        //private readonly ILogger _logger;
        public askController(IDbContextFactory<askContext> dbFactory,askContext askContext, ServiceAsaci ServiceAsaci, IOptions<ParamMessage> paramdata, IConfiguration configuration, IWebHostEnvironment env, ILogger<askController> logger)
        {

            _configuration = configuration;
            _ServiceAsaci = ServiceAsaci;
            _dbFactory = dbFactory;
            _env = env;
            _paramdata = paramdata.Value;
            _logger = logger;
            _dbContext = askContext;
           
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

        [NonAction]
        public string RecupererToken()
        {
            if (Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {

                string token = authorizationHeader.ToString();

                if (token.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.ToString().Substring("Bearer ".Length).Trim();

                return token;
            }

            return "";
        }

        [NonAction]
        public string RecupererIdDemandeEnCours()
        {
            return Request.Headers["id-dmd-header"].ToString();
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
        [HttpGet("attestations/{cleRecherche}")]
        public async Task<IActionResult> GetAttestation(string cleRecherche)
        {
            string _desc_route = "Obtenir une attestation";
            string IdDemande = RecupererIdDemandeEnCours();

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
                           NUMEIMMA, NUMECHAS, LIBERISQ, NUMATTDI
                    FROM attestation_risque
                    WHERE (lien_pdf IS NOT NULL OR lien_img IS NOT NULL OR lien__qr IS NOT NULL)
                      AND (NUMEIMMA = @cleRecherche OR NUMECHAS = @cleRecherche OR NUMATTDI = @cleRecherche)";

                var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = _sql;

                // Ajouter le paramètre de manière sécurisée (protection contre SQL injection)
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@cleRecherche";
                parameter.Value = cleRecherche;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                var results = new List<object>();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        numeroPolicier = reader["NUMEPOLI"]?.ToString(),
                        dateEffet = reader["DATEFFAT"],
                        dateEcheance = reader["DATECHAT"],
                        marqueVehicule = reader["MARQVEHI"]?.ToString(),
                        typeVehicule = reader["TYPEVEHI"]?.ToString(),
                        numeroImmatriculation = reader["NUMEIMMA"]?.ToString(),
                        numeroChassis = reader["NUMECHAS"]?.ToString(),
                        libelleRisque = reader["LIBERISQ"]?.ToString(),
                        numeroAttestation = reader["NUMATTDI"]?.ToString()
                    });
                }

                await connection.CloseAsync();

                if (!results.Any())
                    return NotFound(GeneraleRetour.BuildNotFound(detail: "Aucune attestation trouvée", instance: HttpContext.Request.Path));

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

        /// <summary>
        /// Récupère les informations de l'attestation CEDEAO avec métadonnées (JSON)
        /// </summary>
        [HttpGet("attestations/{numAttestation}/cedeao/info")]
        public async Task<IActionResult> GetAttestationCedeaoInfo(string numAttestation)
        {
            string _desc_route = "Récupération des informations de l'attestation Cedeao";

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

                var response = new
                {
                    numeroAttestation = numAttestation,
                    base64Image = res_data.base64?.ToString(),
                    urlDownload = res_data.urlDownload?.ToString(),
                    reference = res_data.reference?.ToString()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EndPoint {_desc_route}] ===============================>{ex.Message}");
                return StatusCode(500, GeneraleRetour.BuildProblemResponse500(instance: HttpContext.Request.Path));
            }
        }


    }
}