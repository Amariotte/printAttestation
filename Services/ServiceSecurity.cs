using ask.Dtos.RequestToReceiveDto;
using ask.Dtos.RequestToSendDto;
using AutoMapper;
using InteroperabiliteProject.DtoAppMobile.Securite;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.ServicesKeycloack;
using InteroperabiliteProject.ServicesKeycloack.Dtos;
using InteroperabiliteProject.ServicesSecure;
using InteroperabiliteProject.ServicesSecure.Dtos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace InteroperabiliteProject.ServicceAIP
{
    public class ServiceSecurity
    {

        private readonly ILogger<ServiceSecurity> _logger;
        private readonly ServiceAIF _serviceAIF;
        private readonly ServiceMessagerie _serviceMessagerie;
   
        private readonly KeycloackService _keycloackService;
        private readonly SecureService _secureService;
       private readonly IclientRepo _clientRepo;
        private readonly IcompteRepo _compteRepo;
        private readonly IMapper _imapper;
        private readonly SecurityConfig _securityconfig;

        public ServiceSecurity(ILogger<ServiceSecurity> logger, IMapper imapper, SecureService SecureService, KeycloackService KeycloackService,IOptions<SecurityConfig> securityConfig, ServiceAIF serviceAIF, ServiceMessagerie serviceMessagerie, IOptions<PARAM_MESSAGE> paramdata, IclientRepo clientRepo, IdemandeLigneRepo demandeLigneRepo, IcompteRepo compteRepo )
        {
            _logger = logger;
            _serviceAIF = serviceAIF;
            _serviceMessagerie = serviceMessagerie;
            _keycloackService = KeycloackService;
            _secureService = SecureService;
            _securityconfig = securityConfig.Value;
            _clientRepo = clientRepo;
            _compteRepo = compteRepo;
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


        public async Task<GeneraleRetour> Register_keycloack(t_register_plus r,string? IdDemande)
        {
            try
            {


                // Authentification du compte administrateur

                GeneraleRetour b = await _keycloackService.AuthenticateAsync(_securityconfig.keycloack.user_admin, _securityconfig.keycloack.pwd_admin, IdDemande);
               
                if (!Tools.Tools.RetourIsSucces(b.status))
                    return b;


                AuthRetourDto res_auth = JsonConvert.DeserializeObject<AuthRetourDto>(b.data);

                string token_admin = res_auth.access_token;

                string email_user = r.email;
                if (string.IsNullOrEmpty(email_user))
                    email_user = "defaut@defaut.com";


                CreationCompteDto datacompte_user = new CreationCompteDto
                {
                    username = r.identifiant,
                    firstName = r.nom,
                    lastName = r.nom,
                    email = email_user,
                    enabled = true,
                    emailVerified = false,
                    attributes = new Attributes
                    {
                        racine_client = r.racine,
                        numero_tel = r.telephone
                    }
                };

                GeneraleRetour f = await _keycloackService.CreateUserAsync(datacompte_user,r.password, token_admin, IdDemande);
               
                if (!Tools.Tools.RetourIsSucces(f.status))
                    return f;


                 List<UserDto> ret = JsonConvert.DeserializeObject<List<UserDto>>(f.data);

                UserSecurityData d = new UserSecurityData
                {
                    username = ret[0].username,
                    id = ret[0].id,
                    email = ret[0].email,
                    nomcomplet = ret[0].firstName + ' ' + ret[0].lastName,
                    nom = ret[0].firstName,
                    prenom = ret[0].lastName,
                    racine = ret[0].username
                };


            


                return (new GeneraleRetour { status = 200, data = JsonConvert.SerializeObject(d) });

            }
            catch (Exception ex)
            {
                _logger.LogError($"[CreerCompteUser_keycloack] ===============================>{ex.ToString()}");
                throw;
            }
        }

        public async Task<GeneraleRetour> ModificationCodePin(Model.t_client data_client, UpdateCodePinClientDto _body, string token, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        return await _secureService.UpdateCodePinAsync(_body, token, IdDemande);
                    case "KEYCLOAK":

                        GeneraleRetour b =  await _keycloackService.AuthenticateAsync(_securityconfig.keycloack.user_admin, _securityconfig.keycloack.pwd_admin, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(b.status))
                            return b;

                        AuthRetourDto res_auth = JsonConvert.DeserializeObject<AuthRetourDto>(b.data);

                       
                        string token_admin = res_auth.access_token;
                        string email_user = data_client.email;
                        if (string.IsNullOrEmpty(email_user))
                            email_user = "defaut@defaut.com";


                        UpdateCompteDto datacompte_user = new UpdateCompteDto
                        {
                            id = data_client.security_user_id,
                            username = data_client.code,
                            firstName = data_client.nom,
                            lastName = data_client.prenom,
                            email = email_user,
                            enabled = true,
                            emailVerified = false,
                            attributes = new Attributes
                            {
                                racine_client = data_client.code,
                                numero_tel = data_client.telephone,
                                pin_code = _body.new_pin
                            }
                        };

                        return await _keycloackService.ModifieUser(datacompte_user, token_admin);
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de modification du code pin" });

                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"[Modification de code pin] ===============================>{ex.ToString()}");
                throw;
            }
        }



        public async Task<GeneraleRetour> ModificationMotPasse(Model.t_client data_client, UpdatePasswordClientDto _body, string token, string IdDemande)
        {
            try
            {

                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        return await _secureService.UpdateMotPasseAsync(_body, token, IdDemande);
                    case "KEYCLOAK":

                        GeneraleRetour b = await _keycloackService.AuthenticateAsync(_securityconfig.keycloack.user_admin, _securityconfig.keycloack.pwd_admin, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(b.status))
                            return b;

                        AuthRetourDto res_auth = JsonConvert.DeserializeObject<AuthRetourDto>(b.data);

                        string token_admin = res_auth.access_token;
                      

                        return await _keycloackService.SetPassword(data_client.security_user_id, _body.new_password, res_auth.access_token);
                    default:
                        return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de modification du code pin" });

                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"[Modification de code pin] ===============================>{ex.ToString()}");
                throw;
            }
        }


        public async Task<GeneraleRetour> Register_Secure(Model.t_register_plus r,string? IdDemande)
        {
            try
            {

                // Authentification du compte administrateur
                GeneraleRetour e = await _secureService.AuthenticateUserAsync(_securityconfig.Secure.user_admin, _securityconfig.Secure.pwd_admin, IdDemande);

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return e;

                AuthResponseSecureDto res_auth = JsonConvert.DeserializeObject<AuthResponseSecureDto>(e.data);

                string token_admin = res_auth.data.token;

             
                ClientSecureDto dataClient = new ClientSecureDto
                {
                    username = r.identifiant,
                    nomcomplet = r.nom,
                    password = r.password,
                    telephone = r.telephone,
                    nom = r.nom,
                    prenom = "",
                    email = r.email,
                    racine = r.racine,
                };

                GeneraleRetour res = await _secureService.CreateClientAsync(dataClient, token_admin, IdDemande);

                if (!Tools.Tools.RetourIsSucces(e.status))
                    return res;

                return (new GeneraleRetour { status = 200 , data = res.data });

            }
            catch (Exception ex)
            {
                _logger.LogError($"[Register_Secure] ===============================>{ex.ToString()}");
                return (new GeneraleRetour { status = 500, detail = "Une erreur interne est survenue" });
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
                    case "KEYCLOAK":
                        GeneraleRetour b = await _keycloackService.AuthenticateAsync(username, password, IdDemande);
                        if (!Tools.Tools.RetourIsSucces(b.status))
                            return b;

                        AuthRetourDto res_auth = JsonConvert.DeserializeObject<AuthRetourDto>(b.data);

                        AuthSecurityRetourDto eKey = new AuthSecurityRetourDto
                        {
                            access_token = res_auth.access_token,
                            expires_in = res_auth.expires_in,
                            refresh_expires_in = res_auth.refresh_expires_in,
                            refresh_token = res_auth.refresh_token,
                            token_type = res_auth.token_type
                        };

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


