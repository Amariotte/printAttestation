using ask.Dtos.General;
using ask.Dtos.Request.auth;
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
        private readonly SecureService _secureService;
        private readonly IUserRepo _userRepo;
        private readonly IMapper _imapper;
        private readonly SecurityConfig _securityconfig;
        private readonly ParamMessage _paramdata;

        public ServiceAuth(
            ILogger<ServiceAuth> logger,
            IMapper imapper,
            SecureService secureService,
            IOptions<SecurityConfig> securityConfig,
            ServiceMessagerie serviceMessagerie,
            IOptions<ParamMessage> paramdata,
            IUserRepo userRepo)
        {
            _logger = logger;
            _imapper = imapper;
            _secureService = secureService;
            _securityconfig = securityConfig.Value;
            _serviceMessagerie = serviceMessagerie;
            _paramdata = paramdata.Value;
            _userRepo = userRepo;
        }

        public async Task<GeneraleRetour> Register(t_user user, string? IdDemande)
        {
            try
            {
                // Vérification de l'existence de l'utilisateur
                var existingUsers = await _userRepo.GetAllAsync();
                var existingUser = existingUsers.data?
                    .FirstOrDefault(u => u.r_email == user.r_email);

                if (existingUser != null)
                    return new GeneraleRetour { status = 403, detail = "L'utilisateur existe déjà dans le système" };

                // Création selon la méthode de sécurité configurée
                GeneraleRetour b;
                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        var clientDto = new ClientSecureDto
                        {
                            username = user.r_email ?? string.Empty,
                            password = user.r_password ?? string.Empty,
                            nom = user.r_nom ?? string.Empty,
                            prenom = user.r_prenom ?? string.Empty,
                            email = user.r_email ?? string.Empty,
                            telephone = user.r_telephone ?? string.Empty,
                            racine = user.r_code ?? user.r_email ?? string.Empty,
                            nomcomplet = $"{user.r_nom} {user.r_prenom}".Trim()
                        };

                        // Authentification admin pour créer le client
                        var authAdmin = await _secureService.AuthenticateUserAsync(
                            _securityconfig.Secure.user_admin,
                            _securityconfig.Secure.pwd_admin,
                            IdDemande);

                        if (!Tools.Tools.RetourIsSucces(authAdmin.status))
                            return authAdmin;

                        var adminAuth = JsonConvert.DeserializeObject<AuthResponseSecureDto>(authAdmin.data);
                        b = await _secureService.CreateClientAsync(clientDto, adminAuth.data.token, IdDemande);
                        break;
                    default:
                        b = new GeneraleRetour { status = 500, detail = "Méthode de sécurité non supportée" };
                        break;
                }

                if (!Tools.Tools.RetourIsSucces(b.status))
                    return b;

                var d = JsonConvert.DeserializeObject<UserSecurityData>(b.data);

                // Mise à jour de l'utilisateur avec les données de sécurité
                user.r_code = d.racine ?? d.username;
                await _userRepo.AddAsync(user);

                return new GeneraleRetour { status = 200, detail = "Inscription effectuée avec succès" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inscription échouée");
                return new GeneraleRetour { status = 500, detail = "Une erreur est survenue lors de l'inscription" };
            }
        }


        public async Task<GeneraleRetour> ModificationMotPasse(t_user dataUser, UpdatePasswordClientDto _body, string token)
        {
            try
            {
                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        return await _secureService.UpdateMotPasseAsync(_body, token, null);
                    default:
                        return new GeneraleRetour { status = 500, detail = "Méthode de sécurité non supportée" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Modification de mot de passe] ===============================>{ex}");
                throw;
            }
        }


        public async Task<GeneraleRetour> AuthentificationUserClient(string username, string password, string IdDemande)
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

                        return new GeneraleRetour
                        {
                            status = 200,
                            detail = "Authentification effectuée avec succès",
                            data = JsonConvert.SerializeObject(data_auth)
                        };

                    default:
                        return new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus d'authentification" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AuthentificationUserClient] ===============================>{ex}");
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

                        return new GeneraleRetour
                        {
                            status = 200,
                            data = JsonConvert.SerializeObject(data_auth)
                        };

                    default:
                        return new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus d'authentification" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AuthentificationUserApplication] ===============================>{ex}");
                throw;
            }
        }

        public async Task<GeneraleRetour> SupprimerUtilisateurSecure(string token, string IdDemande)
        {
            try
            {
                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        await _secureService.SupprimerClientAsync(token, IdDemande);
                        return new GeneraleRetour { status = 204, detail = "Utilisateur supprimé avec succès" };
                    default:
                        return new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de suppression" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SupprimerUtilisateurSecure] ===============================>{ex}");
                throw;
            }
        }

        public async Task<GeneraleRetour> RefreshToken(string refresh_token, string token, string IdDemande)
        {
            try
            {
                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":

                        GeneraleRetour e = await _secureService.RefereshToken(token, refresh_token, IdDemande);
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

                        return new GeneraleRetour
                        {
                            status = 200,
                            detail = "Rafraîchissement effectué avec succès",
                            data = JsonConvert.SerializeObject(data_res)
                        };

                    default:
                        return new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de rafraîchissement du token" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RefreshToken] ===============================>{ex}");
                throw;
            }
        }

        public async Task<GeneraleRetour> RéinistialiserCodePin(string token, string IdDemande)
        {
            try
            {
                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":
                        return await _secureService.ReinistialiserCodePinAsync(token, IdDemande);
                    default:
                        return new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de réinitialisation du code pin" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RéinistialiserCodePin] ===============================>{ex}");
                throw;
            }
        }

        public async Task<GeneraleRetour> ModificationMotPasseParInitialisation(t_user dataUser, string new_password, string IdDemande)
        {
            try
            {
                var _body = new UpdateSecurePasswordClientByResetDto
                {
                    username = dataUser.r_email,
                    new_password = new_password
                };

                _logger.LogInformation("[ModificationMotPasseParInitialisation] Utilisateur ===================>{Username}", _body.username);

                switch (_securityconfig.secure_method.ToUpper())
                {
                    case "SECURE":

                        // Authentification du compte administrateur
                        GeneraleRetour e = await _secureService.AuthenticateUserAsync(
                            _securityconfig.Secure.user_admin,
                            _securityconfig.Secure.pwd_admin,
                            IdDemande);

                        if (!Tools.Tools.RetourIsSucces(e.status))
                            return e;

                        AuthResponseSecureDto res_auth = JsonConvert.DeserializeObject<AuthResponseSecureDto>(e.data);
                        string token_admin = res_auth.data.token;

                        return await _secureService.UpdateMotPasseByResetAsync(_body, token_admin, IdDemande);

                    default:
                        return new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le processus de modification du mot de passe" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ModificationMotPasseParInitialisation] ===============================>{ex}");
                throw;
            }
        }
    }
}
