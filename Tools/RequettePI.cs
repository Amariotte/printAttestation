using InteroperabiliteProject.Controllers;
using InteroperabiliteProject.Implementation;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace InteroperabiliteProject.Tools
{
    public static class RequettePI
    {
        private static ILogger _logger;

        public static void Initialize(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("RequettePI");
        }
        //public static async Task<HttpResponseMessage> ExecuteHttpsPostRequestAsync(string url, string requestBody, string keystorePath, string keystorePassword)
        //{
        //    try
        //    {
        //        // Chargement keystore
        //        var handler = new HttpClientHandler
        //        {

        //            ServerCertificateCustomValidationCallback =
        //        (HttpRequestMessage req, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) => true

        //        };

        //        //var certificate = new X509Certificate2(keystorePath, keystorePassword);

        //        //handler.ClientCertificates.Add(certificate);

        //        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls13;

        //        using (var client = new HttpClient(handler))
        //        {
        //            // Création de la requête POST
        //            var postRequest = new HttpRequestMessage(HttpMethod.Post, url);

        //            postRequest.Content = new StringContent(requestBody);


        //            if (_logger != null)
        //                _logger.LogInformation($"ExecuteHttpsPostRequestAsync ::::REQUEST:::: =====================> URI : {url}  Body ==========> {requestBody}");

        //            postRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //            // Envoi de la requête
        //            HttpResponseMessage response = await client.SendAsync(postRequest);
        //            string ret = await response.Content.ReadAsStringAsync();

        //            if (_logger != null)
        //                _logger.LogInformation($"ExecuteHttpsPostRequestAsync ::::RESPONSE:::: =====================> URI : {url} statusCode : {response.StatusCode} Body ==========> {ret}");

        //            //******************************UPDATE DE LA LIGNE DEMANDE ****************************************

        //            return response;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogInformation($"Exception ====> HttpsPostRequestAsync ::::ExeptionRequettePI:::: =====================> {ex.Message}  Data ========>{ex.Data} Inner exception {ex.InnerException} ========={ex.InnerException?.Message}");
        //        throw;
        //    }

        //}

        ////////////////////////////////// BLOC KARIM 

        public static async Task<HttpResponseMessage> ExecuteHttpsPostRequestAsync( string url, string requestBody, string keystorePath, string keystorePassword)
        {
            try
            {
                // Chargement keystore (certificat client + clé privée + rootCA intégré)
                var certificate = new X509Certificate2(keystorePath, keystorePassword);
                ///// Nouveau bloc traitement certificats

                //////////        // Charger le certificat client (PEM)
                //////////        var repcertificat = "/var/www/Naomi/certificats/";
                //////////        var certificate = X509Certificate2.CreateFromPemFile(
                //////////            repcertificat+"client_cert.pem",
                //////////            repcertificat + "client_key.pem"
                //////////        );


                //////////        certificate = new X509Certificate2(
                //////////        certificate.Export(X509ContentType.Pkcs12),
                //////////        (string?)null,
                //////////        X509KeyStorageFlags.EphemeralKeySet |
                //////////        X509KeyStorageFlags.Exportable
                //////////);
                //////////        var rootCa = X509Certificate2.CreateFromPemFile(repcertificat + "rootCA.pem");

                /////


                var handler = new HttpClientHandler
                {
                    // Pour dev/test : ignorer la validation du certificat serveur
                 ////   ServerCertificateCustomValidationCallback =
                  /////      (HttpRequestMessage req, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) => true,

               //     SslProtocols = System.Security.Authentication.SslProtocols.Tls13
                    SslProtocols =
                    System.Security.Authentication.SslProtocols.Tls12 |
                    System.Security.Authentication.SslProtocols.Tls13
                };

                // Ajout du certificat client au handler
                handler.ClientCertificates.Add(certificate);

                using (var client = new HttpClient(handler))
                {
                    // Création de la requête POST
                    var postRequest = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(requestBody)
                    };
                    postRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    if (_logger != null)
                        _logger.LogInformation($"ExecuteHttpsPostRequestAsync ::::REQUEST:::: URI: {url} Body: {requestBody}");

                    // Envoi de la requête
                    HttpResponseMessage response = await client.SendAsync(postRequest);
                    string ret = await response.Content.ReadAsStringAsync();

                    if (_logger != null)
                        _logger.LogInformation($"ExecuteHttpsPostRequestAsync ::::RESPONSE:::: URI: {url} StatusCode: {response.StatusCode} Body: {ret}");

                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception ====> HttpsPostRequestAsync ::::ExeptionRequettePI:::: {ex.Message} Data: {ex.Data} InnerException: {ex.InnerException?.Message}");
                throw;
            }
        }










        public static async Task<HttpResponseMessage> ExecuteHttpsGetRequestAsync(
            string url, string keystorePath, string keystorePassword)
        {
            try
            {
                // Chargement keystore (certificat client + clé privée + rootCA intégré)
                var certificate = new X509Certificate2(keystorePath, keystorePassword);

                var handler = new HttpClientHandler
                {
                    // Pour dev/test : ignorer la validation du certificat serveur
                /////    ServerCertificateCustomValidationCallback =
                ////        (HttpRequestMessage req, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) => true,

                    // Support TLS 1.2 et TLS 1.3
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                   System.Security.Authentication.SslProtocols.Tls13
                };

                // Ajout du certificat client au handler
                handler.ClientCertificates.Add(certificate);

                using (var client = new HttpClient(handler))
                {
                    // Création de la requête GET
                    var getRequest = new HttpRequestMessage(HttpMethod.Get, url);

                    getRequest.Content = new StringContent(""); // Certains serveurs demandent un body vide pour GET
                    getRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    if (_logger != null)
                        _logger.LogInformation($"ExecuteHttpsGetRequestAsync ::::REQUEST:::: URI: {url}");

                    // Envoi de la requête
                    HttpResponseMessage response = await client.SendAsync(getRequest);
                    string ret = await response.Content.ReadAsStringAsync();

                    if (_logger != null)
                        _logger.LogInformation($"ExecuteHttpsGetRequestAsync ::::RESPONSE:::: URI: {url} StatusCode: {response.StatusCode} Body: {ret}");

                    //******************************UPDATE DE LA LIGNE DEMANDE ****************************************
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception dans ExecuteHttpsGetRequestAsync");
                throw;
            }
        }



        /////////////////////////////////FIN BLOC KARIM





        //public static async Task<HttpResponseMessage> ExecuteHttpsGetRequestAsync(string url, string keystorePath, string keystorePassword)
        //{
        //    try
        //    {
        //        // Chargement keystore
        //        var handler = new HttpClientHandler();

        //        var certificate = new X509Certificate2(keystorePath, keystorePassword);

        //        handler.ClientCertificates.Add(certificate);

        //        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

        //        using (var client = new HttpClient(handler))
        //        {
        //            // Création de la requête POST
        //            var postRequest = new HttpRequestMessage(HttpMethod.Get, url);

        //            postRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //            // Envoi de la requête
        //            HttpResponseMessage response = await client.SendAsync(postRequest);
        //            string ret = await response.Content.ReadAsStringAsync();

        //            //******************************UPDATE DE LA LIGNE DEMANDE ****************************************

        //            return response;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }

        //}

        //    public static async Task<HttpResponseMessage> ExecuteHttpsPostRequestAsync(string url, string requestBody, string keystorePath, string keystorePassword, DemandeligneRepo rps, string demandeID)
        //    {
        //        try
        //        {
        //            //*********************************AJOUTER UNE LIGNE DEMANDE ***********************************************
        //            var retDemandeLigne = await rps.AddDemandeLigne(Model.Type_Requette.Req_Vers_AIP, demandeID, requestBody);
        //            //*********************************AJOUTER UNE LIGNE DEMANDE ***********************************************





        //            // Chargement keystore
        //            var handler = new HttpClientHandler();

        //            var certificate = new X509Certificate2(keystorePath, keystorePassword);

        //            handler.ClientCertificates.Add(certificate);

        //            handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

        //            using (var client = new HttpClient(handler))
        //            {
        //                // Création de la requête POST
        //                var postRequest = new HttpRequestMessage(HttpMethod.Post, url);


        //                postRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        //                postRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //                // Envoi de la requête
        //                HttpResponseMessage response = await client.SendAsync(postRequest);
        //                string ret = await response.Content.ReadAsStringAsync();

        //                //******************************UPDATE DE LA LIGNE DEMANDE ****************************************
        //                var retAIF = await rps.GetOneAsync(Convert.ToInt32(retDemandeLigne.Item2));
        //                if (!retAIF.Item1)
        //                {
        //                    throw new Exception(message: "Echec de la recuperation de la demande en cours");
        //                }



        //                t_demande_ligne Maligne = retAIF.Item2;

        //                Maligne.r_updatedon = DateTime.Now;
        //                Maligne.r_dateheure_rep = DateTime.Now;
        //                Maligne.r_reponse = JsonConvert.SerializeObject(ret);


        //                switch (response.StatusCode)
        //                {
        //                    case System.Net.HttpStatusCode.OK:
        //                    case System.Net.HttpStatusCode.Accepted:
        //                        Maligne.Status = Statut.SUCCES;
        //                        Maligne.StatusCode = (int)response.StatusCode;
        //                        break;
        //                    default:
        //                        Maligne.Status = Statut.ECHEC;
        //                        Maligne.StatusCode = (int)response.StatusCode;
        //                        break;
        //                }

        //                var retAIFUpdate = await rps.UpdateDemandeLigne(Maligne);
        //                if (!retAIFUpdate.Item1)
        //                {
        //                    throw new Exception(message: "Erreur dans la mise a jour de la l'enregistrement");
        //                }
        //                //******************************UPDATE DE LA LIGNE DEMANDE ****************************************




        //                return response;
        //            }
        //        }
        //        catch (Exception ex)
        //        {

        //            throw;
        //        }

        //    }
        //}
    }
}
