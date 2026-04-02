using ask.Dtos.General;
using ask.Dtos.Request.auth;
using ask.Dtos.RequestToSendDto;
using AutoMapper;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.ServicesKeycloack;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ask.Services
{
    public class SecureService
    {
        private readonly HttpClient _httpClient;
        private readonly SecurityConfig _securityConfig;
        private readonly IdemandeLigneRepo _idemandeligneRepo;
        private readonly ILogger<SecureService> _logger;

     
        public SecureService(ILogger<SecureService> logger, IOptions<SecurityConfig> securityConfig, IdemandeLigneRepo demandeLigneRepo, HttpClient httpClient)
        {
            _securityConfig = securityConfig.Value;
            _idemandeligneRepo = demandeLigneRepo;
            _httpClient = httpClient;
            _logger = logger;

        }


        public string GetBaseUri()
        {
            return _securityConfig.Secure.Uri.TrimEnd('/');
        }
    


        public async Task<GeneraleRetour> AuthenticateClientAsync(string username, string password,string iddemande)
        {
            try
            {

                var requestBody = new Dictionary<string, string>
                {
                    { "login", username },
                    { "password", password },
                };

                string bodyJson = JsonConvert.SerializeObject(requestBody);


                var jsonContent = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                string baseUrl  = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/authentification";

                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, bodyJson, fullUri, "Authentification d'un utilisateur");

                var request = new HttpRequestMessage(HttpMethod.Post, fullUri )
                {
                    Content = jsonContent
                };
                //response.EnsureSuccessStatusCode();

                var response = await _httpClient.SendAsync(request);

                await UpdateDemandeligne(demandeligne, response);


                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });

                }

                // AuthResponseDto tokenResponse = JsonConvert.DeserializeObject<AuthResponseDto>(jsonResponse);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                return (new GeneraleRetour { status = 200, detail = "Authentification effectuée avec succès", data = jsonResponse });

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<GeneraleRetour> SupprimerClientAsync(string token,string iddemande)
        {
            try
            {
                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/supprimerclient";

                var request = new HttpRequestMessage(HttpMethod.Delete, fullUri);
               request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                //response.EnsureSuccessStatusCode();
                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, "", fullUri, "Suppression d'un utilisateur");

                var response = await _httpClient.SendAsync(request);
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });

                }

                // AuthResponseDto tokenResponse = JsonConvert.DeserializeObject<AuthResponseDto>(jsonResponse);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                return (new GeneraleRetour { status = 200, detail = "Authentification effectuée avec succès", data = jsonResponse });

            }
            catch (Exception ex)
            {
                throw;
            }

        }


        public async Task<GeneraleRetour> AuthenticateUserAsync(string username, string password ,string iddemande)
        {
            try
            {

                var requestBody = new Dictionary<string, string>
                {
                    { "login", username },
                    { "password", password },
                };
             
                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var baseURI = GetBaseUri();
                var fullUri = $"{baseURI}/api/secure/acces/authentification";

                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = jsonContent
                };


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, jsonContent.ToString(), fullUri, "Authentification d'un utilisateur");
              
                var response = await _httpClient.SendAsync(request);
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                  
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });

                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                return (new GeneraleRetour { status = 200, detail = "Authentification effectuée avec succès", data = jsonResponse });

            }
            catch (Exception ex)
            {

                throw;

            }

        }


        public async Task<GeneraleRetour> CreateClientAsync(ClientSecureDto _input, string token,string? iddemande)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var validator = new ClientSecureDtoValidator();              

                var results = validator.Validate(_input);

                if (!results.IsValid)
                {
                    return (new GeneraleRetour { status = 400, detail = results.Errors.ToString() });
                }

                var jsonContent = new StringContent(JsonConvert.SerializeObject(_input), Encoding.UTF8, "application/json");
                var baseURI = GetBaseUri();
                string fullUri = $"{baseURI}/api/secure/acces/clients/{_securityConfig.Secure.app_key}";


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, jsonContent.ToString(), fullUri, "Création d'un utilisateur");


                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = jsonContent
                }; 

                var response = await _httpClient.SendAsync(request);

                var responseBody = await response.Content.ReadAsStringAsync();

                await UpdateDemandeligne(demandeligne, response);


                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                RetourCreationUserDto data_user = JsonConvert.DeserializeObject<RetourCreationUserDto>(jsonResponse);


                UserSecurityData d = new UserSecurityData
                {
                    username = data_user.data.username,
                    id = data_user.data.id.ToString(),
                    email = data_user.data.email,
                    nomcomplet = data_user.data.nomcomplet,
                    telephone = data_user.data.telephone,
                    nom = data_user.data.nom,
                    prenom = data_user.data.prenom,
                    racine = data_user.data.racine
                };


                return (new GeneraleRetour { status = 200,data  = JsonConvert.SerializeObject(d) });

            }
            catch (Exception ex)
            {

                throw;
            }



        }

        public async Task<GeneraleRetour> UpdateCodePinAsync(UpdateCodePinClientDto _input, string token,string iddemande)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

             

                var validator = new UpdateCodePinSecureClientDtoValidator();

                var results = validator.Validate(_input);

                if (!results.IsValid)
                    return (new GeneraleRetour { status = 400, detail = results.Errors.ToString() });


                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/code-pin";

                var jsonContent = new StringContent(JsonConvert.SerializeObject(_input), Encoding.UTF8, "application/json");

                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, jsonContent.ToString(), fullUri, "Modification du code pin d'un utilisateur");


                var request = new HttpRequestMessage(HttpMethod.Put, fullUri)
                {
                    Content = jsonContent
                };

                var response = await _httpClient.SendAsync(request);
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 200 });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<GeneraleRetour> UpdateMotPasseAsync(UpdatePasswordClientDto _input, string token, string iddemande)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);



                var validator = new UpdatePasswordClientDtoValidator();

                var results = validator.Validate(_input);

                if (!results.IsValid)
                    return (new GeneraleRetour { status = 400, detail = results.Errors.ToString() });


                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/password";

                var jsonContent = new StringContent(JsonConvert.SerializeObject(_input), Encoding.UTF8, "application/json");

                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, jsonContent.ToString(), fullUri, "Modification du mot de passe d'un utilisateur");


                var request = new HttpRequestMessage(HttpMethod.Put, fullUri)
                {
                    Content = jsonContent
                };

                var response = await _httpClient.SendAsync(request);
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 200 });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<GeneraleRetour> UpdateMotPasseByResetAsync(UpdateSecurePasswordClientByResetDto _input, string token, string iddemande)
        {
            try
            {

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var validator = new UpdateSecurePasswordClientByResetDtoValidator();

                var results = validator.Validate(_input);

                if (!results.IsValid)
                    return (new GeneraleRetour { status = 400, detail = results.Errors.ToString() });


                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/acces/clients/{_securityConfig.Secure.app_key}/password/reset";
                var jsonContent = new StringContent(JsonConvert.SerializeObject(_input), Encoding.UTF8, "application/json");

        
                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, jsonContent.ToString(), fullUri, "Modification du mot de passe d'un utilisateur");


                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = jsonContent
                };

                var response = await _httpClient.SendAsync(request);
                await UpdateDemandeligne(demandeligne, response);
             
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 200 });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<GeneraleRetour> VerifieCodePin(CodePinClientBodyDto _input, string token,string iddemande)
        {

            try

            {
                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/code-pin/verify";
                var jsonContent = new StringContent(JsonConvert.SerializeObject(_input), Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, "", fullUri, "Vérification du code PIN");


                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = jsonContent
                };

                var response = await _httpClient.SendAsync(request);

                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 200, detail = "Code pin verifié avec succès" });

            }
            catch {
                throw;
            }
        }

        public async Task<GeneraleRetour> SupprimerUser(string token,string iddemande)
        {

            try

            {
                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/supprimerclient";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, "", fullUri, "Suppression d'un utilisateur");


                var response = await _httpClient.GetAsync(fullUri);
                
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 204, detail = "Utilisateur Supprimé avec succes" });
            }
            catch
            {
                throw;
            }
        }


        public async Task<GeneraleRetour> RefereshToken( string token,string refresh_token, string iddemande)
        {

            try

            {
                string baseUrl = GetBaseUri();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                string requestUri = $"{baseUrl}/api/secure/client/refreshtokken?refreshToken={Uri.EscapeDataString(refresh_token)}";


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, "", requestUri, "Rafraîchissement de token");


                var response = await _httpClient.GetAsync(requestUri);

                response.EnsureSuccessStatusCode();
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                return (new GeneraleRetour { status = 200, detail = "Token rafraichi avec succès" ,data = jsonResponse });

            }
            catch
            {
                throw;
            }
        }


        private async Task UpdateDemandeligne(t_demande_ligne d, HttpResponseMessage response)
        {
            try
            {
                if (d != null)
                {
                    d.r_updatedon = DateTime.Now;
                    d.r_dateheure_rep = DateTime.Now;
                    d.Status = response.IsSuccessStatusCode ? Statut.SUCCES : Statut.ECHEC;
                    d.StatusCode = (int)response.StatusCode;
                    d.r_reponse = await response.Content.ReadAsStringAsync();

                    await _idemandeligneRepo.UpdateAsync(d);
                }


            }
            catch (Exception ex)
            {
                
            }
        }

        public async Task<GeneraleRetour> DefinirCodePin(CodePinClientBodyDto _input, string token, string iddemande)
        {

            try

            {
                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/code-pin";
                var jsonContent = new StringContent(JsonConvert.SerializeObject(_input), Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, "", fullUri, "Vérification du code PIN");


                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = jsonContent
                };

                var response = await _httpClient.SendAsync(request);

                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 200, detail = "Code pin verifié avec succès" });

            }
            catch
            {
                throw;
            }
        }


        public async Task<GeneraleRetour> ReinistialiserCodePinAsync(string token, string iddemande)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);



                string baseUrl = GetBaseUri();
                string fullUri = $"{baseUrl}/api/secure/client/code-pin/reset";


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_Secure, iddemande, "", fullUri, "Réinistialisation du code pin d'un utilisateur");

                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                };

                var response = await _httpClient.SendAsync(request);
                await UpdateDemandeligne(demandeligne, response);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    RetourSecureError err = JsonConvert.DeserializeObject<RetourSecureError>(error);
                    return (new GeneraleRetour { status = (int)response.StatusCode, detail = string.Join(Environment.NewLine, err.error) ?? "Une erreur est survenue pendant le traitement" });
                }

                return (new GeneraleRetour { status = 200 });

            }
            catch (Exception ex)
            {
                throw;
            }
        }


    }
}
