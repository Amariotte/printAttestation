using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Net.Mail;
using System.Net;
using ask.Interface;

namespace ask.Services
{
    public class ServiceMessagerie
    {

        private readonly PARAM_MESSAGE _param_data;
        private readonly ILogger<ServiceMessagerie> _logger;
        private static readonly HttpClient _client = new HttpClient();
        private readonly IHistoSmsRepo _HistoSmsRepo;
        private readonly IHistoEmailRepo _HistoMailRepo;
        private readonly ImodeleRepo _modeleRepo;
        private readonly IotpRepo _otpRepo;

        public ServiceMessagerie(IOptions<PARAM_MESSAGE> param_data, ILogger<ServiceMessagerie> logger, ImodeleRepo modeleRepo, IotpRepo otpRepo, IHistoSmsRepo HistoSmsRepo, IHistoEmailRepo HistoMailRepo)
        {
            _param_data = param_data.Value;
            _logger = logger;
            _HistoSmsRepo = HistoSmsRepo;
            _HistoMailRepo = HistoMailRepo;
            _modeleRepo = modeleRepo;
            _otpRepo = otpRepo;
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
                    sender = _param_data.sms.sender,
                    text = text,
                    recipient = dest,
                    statut = statut_sms.EN_ATTENTE
                };

                await _HistoSmsRepo.AddAsync(sms);

                res = await sendSms(sms.Id.ToString(),sms.sender, sms.recipient, sms.text);

                if (Tools.Tools.RetourIsSucces(res.status))
                    sms.statut = statut_sms.ENVOYE;
                else
                {
                    sms.statut = statut_sms.ECHOUE;
                    sms.raison_echec = res.detail;
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
                    sender_email = _param_data.smtp.sender_email,
                    sender_name = _param_data.smtp.sender_name,
                    body = text,
                    subject = subject,
                    recipients = dest,
                    statut = statut_email.EN_ATTENTE
                };
                
                await _HistoMailRepo.AddAsync(email);

                res = await sendEmail(email.recipients,email.subject, email.body);

                if (Tools.Tools.RetourIsSucces(res.status))
                    email.statut = statut_email.ENVOYE;
                else
                    email.statut = statut_email.ECHOUE;
                    email.raison_echec = res.detail;

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

        public async Task<GeneraleRetour> sendMessageAuClient(type_modele type,string? tel,t_client? cli,t_otp? otp)
        {
            try
            {


                var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    
                    { "{{PrenomNomUtilisateur}}", $"{cli?.nom ?? ""} {cli?.prenom ?? ""}".Trim()},
                    { "{{NomUtilisateur}}", cli?.nom  ?? string.Empty},
                    { "{{PrenomUtilisateur}}", cli?.prenom ?? string.Empty },
                    { "{{TelephoneUtilisateur}}", cli?.telephone ?? string.Empty},
                    { "{{EmailUtilisateur}}", cli?.email  ?? string.Empty},
                    { "{{Identifiant}}", cli?.security_username ?? string.Empty },
                    { "{{Otp}}", otp?.codeOtp  ?? string.Empty},
                    { "{{DureeOtp}}", otp?.dureeValidite.ToString() ?? string.Empty}
                };

                List<t_modele> modeles = await _modeleRepo.GetModelesByType(type);

                foreach (var modele in modeles)
                {

                    string subject = ReplacePlaceholders(modele.subject, placeholders);
                    string body = ReplacePlaceholders(modele.body, placeholders);


                    if (modele.plateforme == plateforme.SMS )
                    {
                        string dest = tel;
                        if ( string.IsNullOrEmpty(dest))
                            dest = cli.telephone;

                       await saveSms(dest, body);
                    }

                    if (modele.plateforme == plateforme.EMAIL)
                    {
                        await saveEmail(cli.email, subject, body);
                    }

                }

  
                return new GeneraleRetour{ status = 200 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erreur lors du traitement d'envoi de message");
                throw;
            }
        }



        public async Task<GeneraleRetour> sendMessageAuRegister(t_register? r, t_otp? otp)
        {
            try
            {


                var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {

                    { "{{NomUtilisateur}}", r?.nom  ?? string.Empty},
                    { "{{PrenomNomUtilisateur}}", r?.nom  ?? string.Empty},
                    { "{{PrenomUtilisateur}}", r?.nom ?? string.Empty },
                    { "{{TelephoneUtilisateur}}", r?.telephone ?? string.Empty},
                    { "{{EmailUtilisateur}}", r?.email  ?? string.Empty},
                    { "{{Otp}}", otp?.codeOtp  ?? string.Empty},
                    { "{{DureeOtp}}", otp?.dureeValidite.ToString() ?? string.Empty}
                };

                List<t_modele> modeles = await _modeleRepo.GetModelesByType(type_modele.CONFIRM_REGISTER);

                foreach (var modele in modeles)
                {

                    string subject = ReplacePlaceholders(modele.subject, placeholders);
                    string body = ReplacePlaceholders(modele.body, placeholders);


                    if (modele.plateforme == plateforme.SMS)
                    {
                        await saveSms(r.telephone, body);
                    }

                    if (modele.plateforme == plateforme.EMAIL)
                    {
                        await saveEmail(r.email, subject, body);
                    }

                }


                return new GeneraleRetour { status = 200 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Erreur lors du traitement d'envoi de message");
                throw;
            }
        }






    }




}
