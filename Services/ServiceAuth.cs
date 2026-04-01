using ask.Dtos.RequestToReceiveDto;
using ask.Dtos.RequestToSendDto;
using ask.Interface;
using ask.Model;
using AutoMapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ask.Services
{
    public class ServiceAuth
    {

        private readonly ILogger<ServiceAuth> _logger;
        private readonly ServiceMessagerie _serviceMessagerie;
         private readonly IUserRepo _userRepo;
        private readonly IMapper _imapper;
        private readonly SecurityConfig _securityconfig;

        public ServiceAuth(ILogger<ServiceAuth> logger, IMapper imapper, SecureService SecureService,IOptions<SecurityConfig> securityConfig, ServiceAIF serviceAIF, ServiceMessagerie serviceMessagerie, IOptions<PARAM_MESSAGE> paramdata, IuserRepo userRepo )
        {
            _logger = logger;
            _serviceMessagerie = serviceMessagerie;
            _securityconfig = securityConfig.Value;
            _userRepo = userRepo;
             _imapper = imapper;
        }

        public async Task<GeneraleRetour> Register(t_register t, string? IdDemande)

        {

            try
            {

                var retReqAIF = await _serviceAIF.GetClientCompte(t.numerocompte, IdDemande);

                if (!retReqAIF.operationStatus)
                    return (new GeneraleRetour { status = retReqAIF.status, detail = retReqAIF.erreur });

                var ret_AIF = JsonConvert.DeserializeObject<Message>(retReqAIF.data);

                var retReqAIP = await _serviceAIF.GetEquivalenceClientCompte(ret_AIF);

                if (!retReqAIP.operationStatus)
                    return (new GeneraleRetour { status = retReqAIP.status, detail = retReqAIP.erreur });

                var ret_AIP = JsonConvert.DeserializeObject<ReponseAUneDemandeDeVerificationAIF>(retReqAIP.data);

              
                // Vérification du client
                Model.t_client data_client = await _clientRepo.SearchClientByCodeClient(ret_AIF.client.codeClient);

                if (data_client != null)
                    return (new GeneraleRetour { status = 403, detail = "Le client existe dejà dans le système" });


                t_register_plus _body = new t_register_plus {
                    nom = t.nom,
                    email = t.email,
                    telephone = t.telephone,
                    password = t.password,
                    racine = ret_AIF.client.codeClient,
                    identifiant = ret_AIF.client.codeClient
                };


                GeneraleRetour b = new GeneraleRetour();
                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        b = await Register_Secure(_body, IdDemande);
                        break;
                    case "KEYCLOAK":
                        b = await Register_keycloack(_body, IdDemande);
                        break;
                    default:
                        b = new GeneraleRetour { status = 500 };
                        break;
                }


                if (!Tools.Tools.RetourIsSucces(b.status))
                    return b;


                UserSecurityData d = JsonConvert.DeserializeObject<UserSecurityData>(b.data);


                // Création du client si il n'existe pas dans le système

                t_client cli = new t_client {
                    numerocompte_register = t.numerocompte,
                    telephone = _body.telephone,
                    security_user_id = d.id,
                    security_username = d.username,
                    nom = d.nom,
                    prenom = d.prenom,
                    email = d.email,
                    code = ret_AIF.client.codeClient
                };
             
                await _clientRepo.AddAsync(cli);


                 t_compte  data_compte = _imapper.Map<t_compte>(ret_AIF.compte);
                 data_compte.r_client_id = cli.Id;
                 data_compte.ibanOrOther = ret_AIF.compte.iban;
                 data_compte.type = type.Iban;
                 await _compteRepo.AddAsync(data_compte);

                /// Envoi du message
                await _serviceMessagerie.sendMessageAuClient(type_modele.INSCRIPTION, _body.telephone, cli,null);

                return (new GeneraleRetour { status = 200 });
 
            }

            catch (Exception ex)
            {
                _logger.LogError("Inscription", ex.Message,ex);
                return (new GeneraleRetour { status = 500 });
            }

        }




        public async Task<GeneraleRetour> ModificationMotPasse(t_user dataUser, UpdatePasswordClientDto _body, string token)
        {
            try
            {

                dataUser.r_password = _body.new_password; 

              
            }

            catch (Exception ex)
            {
                _logger.LogError($"[Modification de code pin] ===============================>{ex.ToString()}");
                throw;
            }
        }


     
        public async Task<GeneraleRetour> AuthentificationUserClient(string username,string password,string IdDemande)
        {
            try
            {

             switch (_securityconfig.secure_method.ToUpper())
                {
                
                    case "SECURE":

                        GeneraleRetour e = await _secureService.AuthenticateClientAsync(username, password, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(e.status))
                            return e;

                       AuthResponseSecureDto ret_secure = JsonConvert.DeserializeObject<AuthResponseSecureDto>(e.data);

                       AuthSecurityRetourDto data_auth = new AuthSecurityRetourDto
                        {
                            access_token = ret_secure.data.token,
                            expires_in = ret_secure.data.duree_token,
                            refresh_expires_in = ret_secure.data.duree_refresh,
                            refresh_token = ret_secure.data.refresh_token,
                            token_type = ret_secure.data.type,
                            is_pin_created = ret_secure.data.is_pin_created
                       };

                       return (new GeneraleRetour { status = 200,data  = JsonConvert.SerializeObject(data_auth) }); ;
                        break;
                   

                        return (new GeneraleRetour { status = 200, detail = "Authentification effectuée avec succès", data = JsonConvert.SerializeObject(eKey) }); ;
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus d'authentification"}); ;

                }



            }
            catch (Exception ex)
            {
                _logger.LogError($"[AuthentificationUserClient] ===============================>{ex.ToString()}");

                throw;
            }
        }
        public async Task<GeneraleRetour> AuthentificationUserApplication(string username, string password, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {

                    case "SECURE":

                        GeneraleRetour e = await _secureService.AuthenticateUserAsync(username, password, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(e.status))
                            return e;

                        AuthResponseSecureDto ret_secure = JsonConvert.DeserializeObject<AuthResponseSecureDto>(e.data);

                        AuthSecurityRetourDto data_auth = new AuthSecurityRetourDto
                        {
                            access_token = ret_secure.data.token,
                            expires_in = ret_secure.data.duree_token,
                            refresh_expires_in = ret_secure.data.duree_refresh,
                            refresh_token = ret_secure.data.refresh_token,
                            token_type = ret_secure.data.type,
                            is_pin_created = ret_secure.data.is_pin_created
                        };

                        return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(data_auth) }); ;
                        break;
                    case "KEYCLOAK":

                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus d'authentification" }); ;

                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus d'authentification" }); ;

                }



            }
            catch (Exception ex)
            {
                _logger.LogError($"[AuthentificationUserApplication] ===============================>{ex.ToString()}");

                throw;
            }
        }

        public async Task<GeneraleRetour> SupprimerUtilisateurSecure(string tokken, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {

                    case "SECURE":
                        await _secureService.SupprimerClientAsync(tokken, IdDemande);
                        return (new GeneraleRetour { status = 204, detail = "Utilisateur supprimer avec succes" });
                    //case "KEYCLOAK":

                    //(bool bResKey, AuthRetourDto res_auth) = await _keycloackService.AuthenticateAsync(_securityconfig.keycloack.user_admin, _securityconfig.keycloack.pwd_admin);

                    //if (bResKey == false)
                    //    return (new GeneraleRetour { status = 403, detail = "Une erreur est survenue pendant le processus de vérification du code pin" });

                    //var res_keyloack = await _keycloackService.VerifieCodePin(iduser, codepin, res_auth.access_token);

                    //break;
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de vérification du code pin" });

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"[VerificationCodePIN] ===============================>{ex.ToString()}");
                throw;
            }
        }

        public async Task<GeneraleRetour> VerificationCodePIN(CodePinClientBodyDto _bodyPIN, string iduser, string token, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {

                    case "SECURE":
                        return await _secureService.VerifieCodePin(_bodyPIN, token, IdDemande);
                    case "KEYCLOAK":


                        GeneraleRetour b = await _keycloackService.AuthenticateAsync(_securityconfig.keycloack.user_admin, _securityconfig.keycloack.pwd_admin, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(b.status))
                            return b;

                        AuthRetourDto res_auth = JsonConvert.DeserializeObject<AuthRetourDto>(b.data);

                        var res_keyloack = await _keycloackService.VerifieCodePin(iduser, _bodyPIN, res_auth.access_token);
                        return (new GeneraleRetour { status = 200, detail = "Code pin verifié avec succès" });
                     
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de vérification du code pin" });

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"[VerificationCodePIN] ===============================>{ex.ToString()}");
                throw;
            }
        }


        public async Task<GeneraleRetour> DefinirCodePIN(CodePinClientBodyDto _bodyPIN,string token, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {

                    case "SECURE":
                        return await _secureService.DefinirCodePin(_bodyPIN, token, IdDemande);
                    case "KEYCLOAK":
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de vérification du code pin" });
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de vérification du code pin" });

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"[VerificationCodePIN] ===============================>{ex.ToString()}");
                throw;
            }
        }

        public async Task<GeneraleRetour> RefreshToken(string resfresh_token, string token ,string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {

                    case "SECURE":

                        GeneraleRetour e = await _secureService.RefereshToken(token, resfresh_token, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(e.status))
                            return e;

                        AuthResponseSecureDto ret_secure = JsonConvert.DeserializeObject<AuthResponseSecureDto>(e.data);


                        AuthSecurityRetourDto data_res = new AuthSecurityRetourDto
                                             {
                            access_token = ret_secure.data.token,
                            expires_in = ret_secure.data.duree_token,
                            refresh_expires_in = ret_secure.data.duree_refresh,
                            refresh_token = ret_secure.data.refresh_token,
                            token_type = ret_secure.data.type,
                            is_pin_created = ret_secure.data.is_pin_created
                        };

                        return (new GeneraleRetour { status = 200, detail = "Rafraîchissement effectué avec succès", data = JsonConvert.SerializeObject(data_res) }); ;

                    case "KEYCLOAK":

                       GeneraleRetour b = await _keycloackService.refreshtokenAsync( resfresh_token, IdDemande);
                     
                        if (!Tools.Tools.RetourIsSucces(b.status))
                            return b;

                        AuthRetourDto res_auth = JsonConvert.DeserializeObject<AuthRetourDto>(b.data);

                        AuthSecurityRetourDto data_res_key = new AuthSecurityRetourDto
                        {
                            access_token = res_auth.access_token,
                            expires_in = res_auth.expires_in,
                            refresh_expires_in = res_auth.refresh_expires_in,
                            refresh_token = res_auth.refresh_token,
                            token_type = res_auth.token_type,
                            is_pin_created = res_auth.is_pin_created

                        };

                        return (new GeneraleRetour { status = 200, detail = "Rafraîchissement effectué avec succès", data = JsonConvert.SerializeObject(data_res_key) }); ;
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de rafraîchissement du token" });

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"[RefreshToken] ===============================>{ex.ToString()}");
                throw;
            }
        }

        public async Task<GeneraleRetour> RéinistialiserCodePin( string token, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        return await _secureService.ReinistialiserCodePinAsync(token, IdDemande);
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de modification du code pin" });

                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"[Réinistialisation de code pin] ===============================>{ex.ToString()}");
                throw;
            }
        }

        public async Task<GeneraleRetour> ModificationMotPasseParInitialisation(Model.t_client data_client, string new_password,string IdDemande)
        {
            try
            {
                UpdateSecurePasswordClientByResetDto _body = new UpdateSecurePasswordClientByResetDto
                {
                    username = data_client.security_username,
                    new_password = new_password
                };


                _logger.LogError($"[ModificationMotPasseParInitialisation] Utilisateur ===================>{_body.username}");


                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":


                        // Authentification du compte administrateur
                        GeneraleRetour e = await _secureService.AuthenticateUserAsync(_securityconfig.Secure.user_admin, _securityconfig.Secure.pwd_admin, IdDemande);

                        if (!Tools.Tools.RetourIsSucces(e.status))
                            return e;

                        AuthResponseSecureDto res_auth = JsonConvert.DeserializeObject<AuthResponseSecureDto>(e.data);

                        string token_admin = res_auth.data.token;


                        return await _secureService.UpdateMotPasseByResetAsync(_body, token_admin, IdDemande);
                
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de modification du code pin" });

                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"[Modification de mot de passe par Initialisation] ===============================>{ex.ToString()}");
                throw;
            }
        }




    }


}


