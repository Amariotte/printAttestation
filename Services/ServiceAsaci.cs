using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Net.Mail;
using System.Net;
using ask.Interface;
using ask.Dtos.General;
using InteroperabiliteProject.Dtos;
using ask.Model;

namespace ask.Services
{
    public class ServiceAsaci
    {

        private readonly ParamAsaci _paramData;
        private readonly ILogger<ServiceAsaci> _logger;
        private static readonly HttpClient _client = new HttpClient();

        public ServiceAsaci(IOptions<ParamAsaci> paramData, ILogger<ServiceAsaci> logger)
        {
            _paramData = paramData.Value;
            _logger = logger; 
        }

        public async Task<GeneraleRetour> printCedeao(string numAttestation)
        {
            try
            {

                string url = _paramData.urlCedeao.Replace("{numAttestation}", numAttestation);

                // Forcer TLS 1.2
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Handler avec acceptation temporaire des certificats non valides (test uniquement)
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using var client = new HttpClient(handler);
                HttpResponseMessage response = await client.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Status Code: {(int)response.StatusCode}, Response Body: {responseBody}");

                GeneraleRetour service = new GeneraleRetour();

                switch (response.StatusCode)
                {

                    case System.Net.HttpStatusCode.OK:
                        
                        var sucessResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

                        var _body = new
                        {
                            base64 = sucessResponse.data.printed_certificate,
                            urlDownload = sucessResponse.data.certificate.download_link,
                            reference = sucessResponse.data.reference,
                        };

                        service.data = JsonConvert.SerializeObject(_body);
                        service.status = 200;

                        _logger.LogInformation($"Attestation trouvée avec succès: {numAttestation}");
                        return service;


                    case System.Net.HttpStatusCode.NotFound:
                        // Gérer le cas d'une erreur 404 avec réponse JSON

                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        if (errorResponse?.errors != null && errorResponse.errors.Count > 0)
                        {
                            var firstError = errorResponse.errors[0];
                            service.status = (int)(firstError.status ?? 404);
                            service.title = firstError.title?.ToString() ?? "Record not found!";
                            service.detail = firstError.detail?.ToString() ?? "L'attestation digitale n'a pas encore été générée ou le numéro d'attestation n'est pas correct.";
                        }
                        else
                        {
                            service.status = 404;
                            service.title = "Record not found!";
                            service.detail = "L'attestation digitale n'a pas encore été générée ou le numéro d'attestation n'est pas correct.";
                        }
                    }
                    catch
                    {
                        service.status = 404;
                        service.title = "Record not found!";
                        service.detail = "L'attestation digitale n'a pas encore été générée ou le numéro d'attestation n'est pas correct.";
                    }

                    _logger.LogWarning($"Attestation non trouvée: {numAttestation} - {service.detail}");
                    return service;

              
                    default:
                        service.status = 500;
                        service.detail = "Erreur Système, Veuillez contacter l'administrateur";
                        break;
                }

                _logger.LogInformation("Résultat lors de l'envoi de l'edition de la CEDEAO " + service.detail);
                return service;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception lors de l'envoi de l'edition de la CEDEAO " + e.Message);
                GeneraleRetour service = new GeneraleRetour
                {
                    status = 500,
                    detail = "Erreur Systeme, Veuillez contacter l'administrateur",
                };
                return service;
            }
        }



    }




}
