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
    public class ServiceMessagerie
    {

        private readonly ParamMessage _param_data;
        private readonly ILogger<ServiceMessagerie> _logger;
        private static readonly HttpClient _client = new HttpClient();
        private readonly IHistoSmsRepo _HistoSmsRepo;
        private readonly IHistoEmailRepo _HistoMailRepo;
        private readonly ImodeleRepo _modeleRepo;

        public ServiceMessagerie(IOptions<ParamMessage> param_data, ILogger<ServiceMessagerie> logger, ImodeleRepo modeleRepo, IHistoSmsRepo HistoSmsRepo, IHistoEmailRepo HistoMailRepo)
        {
            _param_data = param_data.Value;
            _logger = logger;
            _HistoSmsRepo = HistoSmsRepo;
            _HistoMailRepo = HistoMailRepo;
            _modeleRepo = modeleRepo;
        }

        public async Task<GeneraleRetour> sendSms(string msgid, string sender,string dest, string text)
        {
            try
            {

                string tel_dest = dest;
                if (tel_dest.StartsWith("+"))
                {
                    tel_dest = tel_dest.TrimStart('+');
                }


                QuerySmsDto _body = new QuerySmsDto
                {
                    fromad = sender,
                    toad = tel_dest,
                    identify = _param_data.sms.login,
                    text = text,
                    pwd = _param_data.sms.pwd,
                    class_ = "ASK",
                    srvce = "OTP",
                    msgid = msgid

                };


                // Forcer TLS 1.2
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Handler avec acceptation temporaire des certificats non valides (test uniquement)
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using var client = new HttpClient(handler);

                string jsonData = JsonConvert.SerializeObject(_body);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                string uri = $"{_param_data.sms.baseUri}";
                HttpResponseMessage response = await client.PostAsync(uri, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("responseBody " + responseBody);


                GeneraleRetour service = new GeneraleRetour();

                switch (responseBody)
                {
                    case "1":
                        service.status = 200;
                        service.detail = "Message accepté [Traitement en cours]";
                        break;

                    case "2":
                        service.status = 200;
                        service.detail = "Message accepté par Mail & Par SMS [Traitement en cours]";
                        break;

                    case "3":
                        service.status = 200;
                        service.detail = "Message accepté par Mail [Traitement en cours]";
                        break;

                    case "-2":
                        service.status = 400;
                        service.detail = "Identifiant vide ou invalide";
                        break;

                    case "-4":
                        service.status = 404;
                        service.detail = "Identifiant inconnu";
                        break;

                    case "-5":
                        service.status = 400;
                        service.detail = "Message vide";
                        break;

                    case "-6":
                        service.status = 403;
                        service.detail = "Crédit insuffisant";
                        break;

                    case "-7":
                        service.status = 403;
                        service.detail = "Destinataire invalide";
                        break;

                    case "-8":
                        service.status = 403;
                        service.detail = "Expéditeur non autorisé";
                        break;

                    case "-9":
                        service.status = 403;
                        service.detail = "Destination non permise";
                        break;

                    case "-108":
                        service.status = 403;
                        service.detail = "Référence dupliquée pour le contact demandé";
                        break;

                    case "-109":
                        service.status = 403;
                        service.detail = "Destination inconnue";
                        break;

                    case "-110":
                        service.status = 403;
                        service.detail = "Identifiant désactivé";
                        break;

                    case "-112":
                        service.status = 500;
                        service.detail = "Erreur interne, réessayez plus tard";
                        break;

                    case "-116":
                        service.status = 500;
                        service.detail = "Impossible de se connecter au carrier";
                        break;

                    case "-515":
                    case "-516":
                        service.status = 500;
                        service.detail = "Échec de transmission";
                        break;

                    case "-512":
                        service.status = 500;
                        service.detail = "Erreur interne lors de la transmission";
                        break;

                    default:
                        service.status = 500;
                        service.detail = "Erreur Système, Veuillez contacter l'administrateur";
                        break;
                }

                _logger.LogInformation("Résultat lors de l'envoi du SMS " + service.detail);
                return service;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception lors de l'envoi du SMS " + e.Message);
                GeneraleRetour service = new GeneraleRetour
                {
                    status = 500,
                    detail = "Erreur Systeme, Veuillez contacter l'administrateur",
                };
                return service;
            }
        }



        public async Task<GeneraleRetour> sendEmail(string toEmail, string subject, string bodyHtml)
        {
            GeneraleRetour service = new GeneraleRetour();

            try
            {
                using var smtp = new SmtpClient
                {
                    Host = _param_data.smtp.server,
                    Port = _param_data.smtp.port,
                    EnableSsl = _param_data.smtp.enable_ssl,
                    Credentials = new NetworkCredential(_param_data.smtp.user, _param_data.smtp.password)
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_param_data.smtp.sender_email, _param_data.smtp.sender_name),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                await smtp.SendMailAsync(mail);

                service.status = 200;
                service.detail = "Email envoyé avec succès.";
            }
            catch (SmtpException ex)
            {
                service.status = 500;
                service.detail = $"Erreur SMTP : {ex.Message}";
            }
            catch (Exception ex)
            {
                service.status = 500;
                service.detail = $"Erreur Système, Veuillez contacter l'administrateur.{ex.Message}";
                // Optionnel : log ou console
                Console.WriteLine($"[sendEmail][Exception] {ex.Message}");
            }

            return service;
        }

       
        
      
    public async Task<GeneraleRetour> saveSms( string dest, string text)
        {
            try
            {

                GeneraleRetour res = new GeneraleRetour();

                t_histo_sms sms = new t_histo_sms
                {
                    r_sender = _param_data.sms.sender,
                    r_text = text,
                    r_recipient = dest,
                    r_statut = STATUT_SMS.ATTENTE
                };

                await _HistoSmsRepo.AddAsync(sms);

                res = await sendSms(sms.r_id.ToString(),sms.r_sender, sms.r_recipient, sms.r_text);

                if (Tools.Tools.RetourIsSucces(res.status))
                {
                    sms.r_statut = STATUT_SMS.ENVOYE;
                }
                else
                {
                    sms.r_statut = STATUT_SMS.ECHOUE;
                    sms.r_raison_echec = res.detail;
                }

                await _HistoSmsRepo.UpdateAsync(sms);

                return res;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erreur lors du traitement d'envoi de sms");

                throw;
               
            }
        }


        public async Task<GeneraleRetour> saveEmail(string dest,string subject, string text)
        {
            try
            {

     
                GeneraleRetour res = new GeneraleRetour();

                t_histo_email email = new t_histo_email
                {
                    r_sender_email = _param_data.smtp.sender_email,
                    r_sender_name = _param_data.smtp.sender_name,
                    r_body = text,
                    r_subject = subject,
                    r_recipients = dest,
                    r_statut = STATUT_EMAIL.ATTENTE
                };
                
                await _HistoMailRepo.AddAsync(email);

                res = await sendEmail(email.r_recipients, email.r_subject, email.r_body);

                if (Tools.Tools.RetourIsSucces(res.status))
                {
                    email.r_statut = STATUT_EMAIL.ENVOYE;
                }
                else
                {
                    email.r_statut = STATUT_EMAIL.ECHOUE;
                    email.r_raison_echec = res.detail;
                }

                await _HistoMailRepo.UpdateAsync(email);

                return res;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erreur lors du traitement d'envoi d' EMAIL");

                throw;

            }
        }

        private string ReplacePlaceholders(string? text, Dictionary<string, string> placeholders)
        {

            if (text == null)
                return "";

            foreach (var kvp in placeholders)
            {
                text = text.Replace(kvp.Key, kvp.Value ?? "");
            }
            return text;
        }

        public async Task<GeneraleRetour> sendMessageALUtilisateur(TYPE_MODELE type,t_user? user,string? pass)
        {
            try
            {


                var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    
                    { "{{PrenomNomUtilisateur}}", $"{user?.r_nom ?? ""} {user?.r_prenom ?? ""}".Trim()},
                    { "{{NomUtilisateur}}", user?.r_nom  ?? string.Empty},
                    { "{{PrenomUtilisateur}}", user?.r_prenom ?? string.Empty },
                    { "{{TelephoneUtilisateur}}", user?.r_telephone ?? string.Empty},
                    { "{{EmailUtilisateur}}", user?.r_email  ?? string.Empty},
                    { "{{MotDePasse}}", pass ?? string.Empty},
                };

                List<t_modele> modeles = await _modeleRepo.GetModelesByType(type);

                foreach (var modele in modeles)
                {

                    string subject = ReplacePlaceholders(modele.r_subject, placeholders);
                    string body = ReplacePlaceholders(modele.r_body, placeholders);


                    if (modele.r_plateforme == PLATEFORME_MESSAGERIE.SMS )
                       await saveSms(user.r_telephone, body);

                    if (modele.r_plateforme == PLATEFORME_MESSAGERIE.EMAIL)
                        await saveEmail(user.r_email, subject, body);

                }

  
                return new GeneraleRetour{ status = 200 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erreur lors du traitement d'envoi de message");
                throw;
            }
        }


    }




}
