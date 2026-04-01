using System.Security.Authentication;
using System.Text;
using ask.Dtos.RequestToReceiveDto;
using ask.Dtos.RequestToSendDto;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;


namespace InteroperabiliteProject.ServicceAIP
{
    public class ServiceAIF
    {

        private readonly AIPDATA _aipdata;
        private readonly ILogger<ServiceAIF> _logger;
        private readonly HttpClient _httpClient;
        private readonly IreferenceRepo _ireferenceRepo;
        private readonly IdemandeLigneRepo _idemandeligneRepo;
        private readonly IdatasRepo _datarepo;
        private readonly string msg_error_systeme = "Une erreur inattendue s’est produite sur le serveur [AIF] lors du traitement de la demande";

        public ServiceAIF(IOptions<AIPDATA> aipdata, ILogger<ServiceAIF> logger, IreferenceRepo IreferenceRepo, IdemandeLigneRepo idemandeligneRepo, IdatasRepo datarepo, HttpClient httpClient)
        {
            _aipdata = aipdata.Value;
            _logger = logger;
            _ireferenceRepo = IreferenceRepo;
            _datarepo = datarepo;
            _idemandeligneRepo = idemandeligneRepo;
            _datarepo = datarepo;
            _httpClient = httpClient;

            var handler = new HttpClientHandler()
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Ssl2 | SslProtocols.Tls13
            };

            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }


        public async Task<GeneraleRetourService> GetClientDetails(string racineclient, string iddemande)
        {
            string _script = "GetClientDetails";
            try
            {
                string uri = $"{_aipdata.baseUriaifComplet}/ClientDetail/{racineclient}";

                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, "", uri, "Récuperer le détail d'un client");

                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                await UpdateDemandeligne(demandeligne, response);


                // Lire le contenu de la réponse
                string responseBody = await response.Content.ReadAsStringAsync();

                GeneralRetourAIF ret = JsonConvert.DeserializeObject<GeneralRetourAIF>(responseBody);
                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:

                        service.operationStatus = true;
                        service.data = ret.Message;

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        service.operationStatus = false;
                        service.erreur = ret.Description;

                        break;

                    default:

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }

                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }


        public async Task<GeneraleRetourService> GetClientListeCompte(string racineDuClient, string iddemande)
        {
            string _script = "GetClientListeCompte";
            try
            {
                string uri = $"{_aipdata.baseUriaifComplet}/CompteList";

                // Envoyer la requête POST
                var body = JsonConvert.SerializeObject(new
                {
                    racineClient = racineDuClient,

                });

                var content = new StringContent(body, Encoding.UTF8, "application/json");

                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "Récuperer la liste des comptes d'un client");


                _logger.LogInformation($"[{_script}] Données envoyées à AIF sur Uri :{uri} Data : {body}");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                await UpdateDemandeligne(demandeligne, response);

                string responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"[{_script}] Reponse de l'AIF : {responseBody}");

                //GeneralRetourAIF ret = JsonSerializer.<GeneralRetourAIF>(responseBody);
                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        RetourAifListeCompte ret = JsonConvert.DeserializeObject<RetourAifListeCompte>(responseBody);
                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Message);

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec ret_1 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);
                        service.operationStatus = false;
                        service.erreur = ret_1.Description;
                        break;

                    default:

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }

                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }

        public async Task<GeneraleRetourService> GetClientCompte(string numeroCompteOrIban, string? iddemande)
        {
            string _script = "GetClientCompte";

            try
            {
                // Sérialiser les données en JSON
                string uri = $"{_aipdata.baseUriaifComplet}/ClientCompte";
                string numCompte = Tools.Tools.TransformeIbanEnNumCompte(numeroCompteOrIban);

                var body = JsonConvert.SerializeObject(new { numeroCompte = numCompte });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                t_demande_ligne demandeligne = new t_demande_ligne();

                if (!string.IsNullOrEmpty(iddemande))
                    demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "Récuperer la liste des comptes d'un client");

                _logger.LogInformation($"[{_script}] Données envoyées à l'Uri :{uri} Data : {body}");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                if (!string.IsNullOrEmpty(iddemande))
                    await UpdateDemandeligne(demandeligne, response);

                string responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"[{_script}] Réponse retournée par l'AIF : {responseBody}");

                GeneraleRetourService service = new GeneraleRetourService();


                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:

                        RetourAifGnerale ret = JsonConvert.DeserializeObject<RetourAifGnerale>(responseBody);

                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Message);

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec retEchec = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);

                        service.operationStatus = false;
                        service.erreur = retEchec.Description;

                        break;

                    default:
                        RetourAifGneraleEchec ret_echec2 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }

                service.status = (int)response.StatusCode;
                return service;
            }
            catch (Exception e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }


        private async Task<GeneraleRetourService> ReservationDeFonds(ReservationFondsBodyDto _body, string iddemande)
        {

            string _script = "ReservationDeFonds";
            try
            {
                _body.numeroCompte = Tools.Tools.TransformeIbanEnNumCompte(_body.numeroCompte);


                if (string.IsNullOrEmpty(_body.identifiantTransaction))
                    _body.identifiantTransaction = Tools.Tools.GenerateAlphaNumeriquevalue(30);

                if (string.IsNullOrEmpty(_body.designationReserve))
                    _body.designationReserve = $"RESERVATION DE FONDS DE {_body.montantReserve} PI";

                //if (string.IsNullOrEmpty(_body.dateEcheance))
                //    _body.dateEcheance = DateTime.Now.ToString("yyyy-MM-dd");

                //-------------------------KARIM BLOC ---------------------------------------------------

                if (string.IsNullOrEmpty(_body.dateEcheance))
                {
                    DateTime now = DateTime.Now;
                    DateTime date;

                    // Vérifier si on est vendredi et après 22h
                    if (now.DayOfWeek == DayOfWeek.Friday && now.Hour >= 22)
                    {
                        // Passer au lundi suivant
                        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                        date = now.Date.AddDays(daysUntilMonday);
                    }
                    else
                    {
                        // Sinon, garder la date courante (avec gestion week-end comme avant)
                        date = now.Date;

                        if (date.DayOfWeek == DayOfWeek.Saturday)
                            date = date.AddDays(2);
                        else if (date.DayOfWeek == DayOfWeek.Sunday)
                            date = date.AddDays(1);
                    }

                    _body.dateEcheance = date.ToString("yyyy-MM-dd");
                }
                //-------------------------KARIM BLOC ---------------------------------------------------

                string body = JsonConvert.SerializeObject(_body);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                string uri = $"{_aipdata.baseUriaifComplet}/FondReservationCreate";


                _logger.LogInformation($"[{_script}] Données envoyées à AIF sur Uri :{uri} Data : {body}");


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "Réservation de fonds");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                await UpdateDemandeligne(demandeligne, response);

                string responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"[{_script}] Réponse retournée par l'AIF : {responseBody}");



                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        RetourAifReservation ret = JsonConvert.DeserializeObject<RetourAifReservation>(responseBody);
                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Message);

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec ret_1 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);
                        service.operationStatus = false;
                        service.erreur = ret_1.Description;
                        break;

                    default:

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }

                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }

        private async Task<GeneraleRetourService> TransfertInterne(TransfertInterneBodyDto _body, string iddemande)
        {
            string _script = "TransfertInterne";
            try
            {


                if (string.IsNullOrEmpty(_body.motif))
                {
                    _body.motif = "TRANSFERT INTERNE INTEROPERABLE";
                }


                _body.compteClientPayeur = Tools.Tools.TransformeIbanEnNumCompte(_body.compteClientPayeur);
                _body.compteClientPaye = Tools.Tools.TransformeIbanEnNumCompte(_body.compteClientPaye);

                string body = JsonConvert.SerializeObject(_body);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                string uri = $"{_aipdata.baseUriaifComplet}/InterneTransferCreate";


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "InterneTransferCreate");


                _logger.LogInformation($"[{_script}] Données envoyées à AIF sur Uri :{uri} Data : {body}");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);



                await UpdateDemandeligne(demandeligne, response);


                // Lire le contenu de la réponse
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"[{_script}] Reponse de l'AIF : {responseBody}");

                //GeneralRetourAIF ret = JsonSerializer.<GeneralRetourAIF>(responseBody);
                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        RetourAifTransfertInterne ret = JsonConvert.DeserializeObject<RetourAifTransfertInterne>(responseBody);
                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Message);

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec ret_1 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);
                        service.operationStatus = false;
                        service.erreur = ret_1.Description;
                        break;

                    default:

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }
                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }

        private async Task<GeneraleRetourService> TransfertExterne(OrdreDeDebitBodyDto _body, string iddemande)
        {
            string _script = "TransfertExterne";
            try
            {

                if (string.IsNullOrEmpty(_body.motif))
                    _body.motif = "TRANSFERT EXTERNE INTEROPERABLE EN EMMISSION ORDRE DE DEBIT";



                string body = JsonConvert.SerializeObject(_body);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                string uri = $"{_aipdata.baseUriaifComplet}/ExterneTransferSend";


                // Envoyer la requête POST

                _logger.LogInformation($"[{_script}] Données envoyées à AIF sur Uri :{uri} Data : {body}");
                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "Transfert externe en émission ordre de débit");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                await UpdateDemandeligne(demandeligne, response);



                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"[{_script}] Reponse de l'AIF : {responseBody}");

                //GeneralRetourAIF ret = JsonSerializer.<GeneralRetourAIF>(responseBody);
                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        RetourAifTransfertInterne ret = JsonConvert.DeserializeObject<RetourAifTransfertInterne>(responseBody);
                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Message);

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec ret_1 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);
                        service.operationStatus = false;
                        service.erreur = ret_1.Description;
                        break;

                    default:

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }
                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }

        private async Task<GeneraleRetourService> RecevoirTransfertExterne(OrdreDeCreditBodyDto _body, string iddemande)
        {

            string _script = "RecevoirTransfertExterne";
            try
            {


                // Envoyer la requête POST

                if (string.IsNullOrEmpty(_body.motif))
                {
                    _body.motif = "RECEPTION DE TRANSFERT EXTERNE INTEROPERABLE ORDE DE CREDIT";
                }


                string body = JsonConvert.SerializeObject(_body);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                string uri = $"{_aipdata.baseUriaifComplet}/ExterneTransferReceive";


                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "Réception d'un transfert externe appel ordre de credit");

                _logger.LogInformation($"[{_script}] Données envoyées à AIF sur Uri :{uri} Data : {body}");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                await UpdateDemandeligne(demandeligne, response);


                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"[{_script}] Reponse de l'AIF {responseBody}");



                //GeneralRetourAIF ret = JsonSerializer.<GeneralRetourAIF>(responseBody);
                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        RetourAifTransfertInterne ret = JsonConvert.DeserializeObject<RetourAifTransfertInterne>(responseBody);
                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Message);

                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec ret_1 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);
                        service.operationStatus = false;
                        service.erreur = ret_1.Description;
                        break;

                    default:

                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }

                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }

        public async Task<GeneraleRetourService> FondReservationCancel(CancelReservationBody _body, string iddemande)
        {
            string _script = "FondReservationCancel";
            try
            {
                string uri = $"{_aipdata.baseUriaifComplet}/FondReservationCancel";

                var body = JsonConvert.SerializeObject(new
                {
                    codeAgenceTransaction = _body.codeAgenceTransaction,
                    codeOperation = _body.codeOperation,
                    numeroEvenement = _body.numEvenement,
                });

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                t_demande_ligne demandeligne = await _idemandeligneRepo.AddDemandeLigne(sensRequete.Req_Vers_AIF, iddemande, body, uri, "Annulation d'une réservation");

                _logger.LogInformation($"[{_script}] Données envoyées à AIF sur Uri :{uri} Data : {body}");

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                await UpdateDemandeligne(demandeligne, response);


                // Lire le contenu de la réponse
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"[{_script}] Reponse de l'AIF {responseBody}");

                GeneraleRetourService service = new GeneraleRetourService();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        RetourAifReservationCancel ret = JsonConvert.DeserializeObject<RetourAifReservationCancel>(responseBody);
                        service.operationStatus = true;
                        service.data = JsonConvert.SerializeObject(ret.Description);
                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.NoContent:
                    case System.Net.HttpStatusCode.NotFound:
                        RetourAifGneraleEchec ret_1 = JsonConvert.DeserializeObject<RetourAifGneraleEchec>(responseBody);
                        service.operationStatus = false;
                        service.erreur = ret_1.Description;
                        break;
                    default:
                        service.operationStatus = false;
                        service.erreur = msg_error_systeme;
                        break;
                }
                service.status = (int)response.StatusCode;
                return service;
            }
            catch (HttpRequestException e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }


        public async Task<GeneraleRetourService> GetEquivalenceClientCompte(Message data)
        {
            string _script = "GetEquivalenceClientCompte";

            try
            {

                GeneraleRetourService service = new GeneraleRetourService();


                List<string> tab_error = new List<string> { };

                string _iban = data.compte.iban.Replace(" ", "");

                if (string.IsNullOrEmpty(_iban))
                    tab_error.Add("iban");

                string _genre = null;
                string _code_syspiece = null;
                string _pays_naiss = null;



                ///// Vérification de la rubrique comptable du comptable

                List<ItemRubComptable> rub_comptables_autorisees = await _datarepo.getDataInListByCode<ItemRubComptable>(code_datas.RUBRIQUE_COMPTABLE_AUTORISEE.ToString());


                var rubriqueCorrespondante = rub_comptables_autorisees
                    .Where(r => r.Code == data.compte.rubriqueComptable)
                    .FirstOrDefault();

                if (rubriqueCorrespondante == null) // Non trouvé
                {
                    service.operationStatus = false;
                    service.erreur = "Le compte n'est pas autorisé";
                    service.status = 403;
                    return service;
                }

                string _type_client = null;

                (bool, string) ret_code_type_client = await _ireferenceRepo.EquivalenceAIF("TYPE_CLIENT", data.client.typeClient);
                if (!ret_code_type_client.Item1)
                {
                    tab_error.Add("Type client");
                }
                else
                {

                    /// 
                    _type_client = ret_code_type_client.Item2;


                    if (new[] { "J", "K", "L" }.Contains(data.client.typeClient) && data.compte.rubriqueComptable == "251165" && new[] { "2402", "2401" }.Contains(data.client.codeAgencEconomique))
                    {
                        _type_client = "C";
                    }


                    /// Informations obligatoires pour les personnes physiques
                    if (_type_client == "P" || _type_client == "C")

                    {
                        (bool, string) ret_genre = await _ireferenceRepo.EquivalenceAIF("GENRE", data.client.genreClient);
                        if (!ret_genre.Item1)
                            tab_error.Add("Genre");
                        else
                            _genre = ret_genre.Item2;


                        if (string.IsNullOrEmpty(data.client.dateNaissanceClient))
                            tab_error.Add("Date de naissance");

                        if (string.IsNullOrEmpty(data.client.villeNaissanceClient))
                            tab_error.Add("Ville de naissance");


                        (bool, string) ret_code_type_piece = await _ireferenceRepo.EquivalenceAIF("TYPE_PIECE", data.client.codePieceClient);
                        if (!ret_code_type_piece.Item1)
                            tab_error.Add("Pièce client");
                        else
                            _code_syspiece = ret_code_type_piece.Item2;


                        (bool, string) ret_pays_naiss = await _ireferenceRepo.EquivalenceAIF("PAYS", data.client.paysNaissanceClient);
                        if (!ret_pays_naiss.Item1)
                            tab_error.Add("Pays de naissance");
                        else
                            _pays_naiss = ret_pays_naiss.Item2;

                    }
                    else
                    {
                        _code_syspiece = "TXID";
                    }

                }



                (bool, string) ret_nationalite = await _ireferenceRepo.EquivalenceAIF("NATIONALITE", data.client.nationaliteClient);
                if (!ret_nationalite.Item1)
                {
                    tab_error.Add("Nationalité");
                }

                (bool, string) ret_pays_resi = await _ireferenceRepo.EquivalenceAIF("PAYS", data.client.paysResidenceClient);
                if (!ret_pays_resi.Item1)
                {
                    tab_error.Add("Pays de résidence");
                }


                string numtel = Tools.Tools.SupprimerEspaces(data.client.telephoneClient);

                if (string.IsNullOrEmpty(numtel))
                    tab_error.Add("Numéro de téléphone");


                string _date_naiss = null;

                if (!string.IsNullOrEmpty(data.client.dateNaissanceClient))
                    _date_naiss = data.client.dateNaissanceClient.Substring(0, 10);


                if (tab_error.Count() != 0)
                {
                    service.operationStatus = false;
                    if (tab_error.Count() == 1) service.erreur = "La donnée bancaire " + string.Join(", ", tab_error) + " n'est pas conforme";
                    else service.erreur = "Les données bancaires " + string.Join(", ", tab_error) + " ne sont pas conformes";

                    service.status = 400;
                    return service;
                }


                ReponseAUneDemandeDeVerificationAIF data_cpte = new ReponseAUneDemandeDeVerificationAIF();

                data_cpte.typeClient = _type_client;
                data_cpte.nomClient = data.client.nomClientArestituer;
                data_cpte.nationaliteClient = ret_nationalite.Item2;
                data_cpte.paysResidence = ret_pays_resi.Item2;
                data_cpte.paysNaissance = _pays_naiss;
                data_cpte.telephoneClient = numtel;
                data_cpte.villeNaissance = data.client.villeNaissanceClient;
                data_cpte.villeClient = data.client.villeResidenceClient;
                data_cpte.dateNaissance = _date_naiss;
                data_cpte.dateOuvertureCompte = data.compte.dateOuverture.Substring(0, 10);
                data_cpte.ibanClient = _iban;
                data_cpte.devise = data.compte.deviseCompte;
                data_cpte.adresseComplete = data.client.adresseClient;
                data_cpte.sigleClient = data.client.sigleClient;
                data_cpte.agence = data.compte.codeAgence;
                data_cpte.compte = data.compte.numeroCompte;
                data_cpte.codeMembreParticipant = _aipdata.codemembre;

                data_cpte.typeCompte = rubriqueCorrespondante.TypeCompte;
                data_cpte.genreClient = _genre;
                data_cpte.numeroIdentification = data.client.identificationNationaleClient;
                data_cpte.systemeIdentification = _code_syspiece;

                if (data_cpte.typeClient == "B" || data_cpte.typeClient == "G")
                {
                    data_cpte.systemeIdentification = "TXID";
                    data_cpte.numeroIdentification = data.client.numeroRCCMClient;
                }

                if (data_cpte.typeClient == "C")
                    data_cpte.numeroRCCMClient = data.client.numeroRCCMClient;

                service.operationStatus = true;
                service.data = JsonConvert.SerializeObject(data_cpte);

                _logger.LogInformation($"[{_script}] Équivalence retournée : {service.data}");

                service.status = 200;
                return service;
            }
            catch (Exception e)
            {

                _logger.LogError($"[{_script}] Exception : {e.Message}");

                GeneraleRetourService service = new GeneraleRetourService
                {
                    operationStatus = false,
                    status = 500,
                    erreur = msg_error_systeme,
                    data = e.ToString()
                };
                return service;
            }
        }

        public async Task<GeneraleRetour> LeveeUneReservationDeFonds(CancelReservationBody _body, string iddemande)
        {
            string _script = "SERVICE DE LEVEE DE RESERVATION DE FONDS";

            try
            {

                var resReservationCancel = await FondReservationCancel(_body, iddemande);

                _logger.LogInformation($"[{_script}] Levée de la reservation de fonds : {JsonConvert.SerializeObject(resReservationCancel)}");

                if (resReservationCancel == null)
                    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });

                return (new GeneraleRetour { status = resReservationCancel.status, detail = resReservationCancel.erreur });
               
            }

            catch (Exception ex)
            {
                _logger.LogError($"[{_script}] Exception : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }

        public async Task<GeneraleRetour> FaireUneReservationDeFonds(ReservationFondsBodyDto _body, string iddemande)
        {
            string _script = "SERVICE RESERVATION DE FONDS";

            try
            {


                var resReservation = await ReservationDeFonds(_body, iddemande);

                _logger.LogInformation($"[{_script}] Retour demande reservation de fonds : {JsonConvert.SerializeObject(resReservation)}");

                if (resReservation == null)
                    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });

                if (!resReservation.operationStatus)
                    return (new GeneraleRetour { status = resReservation.status, detail = resReservation.erreur });

                var dataReservation = JsonConvert.DeserializeObject<ReservationDto_AIF>(resReservation.data);

                if (dataReservation == null)
                    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });

                switch (dataReservation.statutReserve)
                {
                    case "ACCP":
                        return (new GeneraleRetour { status = 200, detail = "Réservation de fonds acceptée", data = JsonConvert.SerializeObject(dataReservation) });
                    case "PDNG":
                        return (new GeneraleRetour { status = 403, detail = "Opération echouée", data = JsonConvert.SerializeObject(dataReservation) });
                    case "ACTC":
                        return (new GeneraleRetour { status = 403, detail = "Opération non traitée dans le délai imparti", data = JsonConvert.SerializeObject(dataReservation) });
                        
                    default:
                        return (new GeneraleRetour { status = 403, detail = "Opération échouée" });
                }
            }


            catch (Exception ex)
            {
                _logger.LogError($"[{_script}] Exception : {ex.Message}");
                return (new GeneraleRetour { status = 500 });
            }
        }

        public async Task<GeneraleRetour> OrdreDeTransfertInterne(TransfertInterneBodyDto t, string iddemande)
        {
            string _script = "SERVICE TRANSFERT INTERNE ORDRE DE TRANFERT INTERNE";

            try
            {


                var res_trans_interne = await TransfertInterne(t, iddemande);
                if (res_trans_interne == null)
                    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });

                if (!res_trans_interne.operationStatus)
                    return (new GeneraleRetour { status = res_trans_interne.status, detail = res_trans_interne.erreur });

                var datatrans_interne = JsonConvert.DeserializeObject<TransfertDto_AIF>(res_trans_interne.data);

                switch (datatrans_interne.statutTransaction)
                {
                    case "ACSC":
                    case "ACSP":
                    case "ACCP":

                        return (new GeneraleRetour { status = 200, detail = "Transfert effectué avec succès" });

                    case "PDNG":
                        return (new GeneraleRetour { status = 403, detail = "Opération echouée" });

                    case "ACTC":
                        return (new GeneraleRetour { status = 403, detail = "Opération non traitée dans le délai imparti" });

                    default:
                        return (new GeneraleRetour { status = 403, detail = !string.IsNullOrEmpty(datatrans_interne.motif) ? datatrans_interne.motif : "Transfert echoué", data = datatrans_interne.codeRaison });

                }

            }


            catch (Exception ex)
            {
                _logger.LogError($"[{_script}] Exception : {ex.Message}");
                return (new GeneraleRetour { status = 500, detail = msg_error_systeme });
            }
        }

        public async Task<GeneraleRetour> OrdreDeDebit(OrdreDeDebitBodyDto _body, string iddemande)
        {
            string _script = "ORDRE DE DEBIT TRANSFERT EXTERNE";

            try
            {
                
             
                //var res_levee = await LeveeUneReservationDeFonds(_bodyCancelReservation, iddemande);
                //if (res_levee == null)
                //    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });

                //if (!Tools.Tools.RetourIsSucces(res_levee.status))
                //    return (new GeneraleRetour { status = res_levee.status, detail = res_levee.detail });


                var res_trans_externe = await TransfertExterne(_body, iddemande);
                if (res_trans_externe == null)
                    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });


                if (!res_trans_externe.operationStatus)
                    return (new GeneraleRetour { status = res_trans_externe.status, detail = res_trans_externe.erreur });


                var datatrans_externe = JsonConvert.DeserializeObject<TransfertDto_AIF>(res_trans_externe.data);


                switch (datatrans_externe.statutTransaction)
                {
                    case "ACSC":
                    case "ACSP":
                    case "ACCP":

                        return (new GeneraleRetour { status = 200, detail = "Transfert effectué avec succès" });

                    case "PDNG":
                        return (new GeneraleRetour { status = 403, detail = "Opération echouée" });

                    case "ACTC":
                        return (new GeneraleRetour { status = 403, detail = "Opération non traitée dans le délai imparti" });

                    case "RJCT":
                        return (new GeneraleRetour { status = 403, detail = !string.IsNullOrEmpty(datatrans_externe.motif) ? datatrans_externe.motif : "Opération échouée", data = datatrans_externe.codeRaison });
                    default:
                        return (new GeneraleRetour { status = 403, detail = !string.IsNullOrEmpty(datatrans_externe.motif) ? datatrans_externe.motif : "Opération échouée", data = datatrans_externe.codeRaison });

                }

                }


            catch (Exception ex)
            {
                _logger.LogError($"[{_script}] Exception : {ex.Message}");
                return (new GeneraleRetour { status = 500, detail = msg_error_systeme });
            }
        }

        public async Task<GeneraleRetour> OrdreDeCredit(OrdreDeCreditBodyDto _body, string iddemande)
        {
            string _script = "RECEVOIR UN TRANSFERT EXTERNE ORDRE DE CREDIT";

            try
            {


                var res_trans_externe = await RecevoirTransfertExterne(_body, iddemande);
                if (res_trans_externe == null)
                    return (new GeneraleRetour { status = 500, detail = "Une erreur est survenue pendant le traitement" });

                if (!res_trans_externe.operationStatus)
                    return (new GeneraleRetour { status = res_trans_externe.status, detail = res_trans_externe.erreur });

                var datatrans_externe = JsonConvert.DeserializeObject<TransfertDto_AIF>(res_trans_externe.data);

                switch (datatrans_externe.statutTransaction)
                {
                    case "ACSC":
                    case "ACSP":
                    case "ACCP":

                        return (new GeneraleRetour { status = 200, detail = "Transfert effectué avec succès" });

                    case "PDNG":
                        return (new GeneraleRetour { status = 403, detail = "Opération echouée" });

                    case "ACTC":
                        return (new GeneraleRetour { status = 403, detail = "Opération non traitée dans le délai imparti" });

                    case "RJCT":
                        return (new GeneraleRetour { status = 403, detail = !string.IsNullOrEmpty(datatrans_externe.motif) ? datatrans_externe.motif : "Opération échouée", data = datatrans_externe.codeRaison });
                    default:
                        return (new GeneraleRetour { status = 403, detail = !string.IsNullOrEmpty(datatrans_externe.motif) ? datatrans_externe.motif : "Opération échouée", data = datatrans_externe.codeRaison });

                }

            }


            catch (Exception ex)
            {
                _logger.LogError($"[{_script}] Exception : {ex.Message}");

                return (new GeneraleRetour { status = 500, detail = msg_error_systeme });
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
                    ;

                    await _idemandeligneRepo.UpdateAsync(d);
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"[UpdateDemandeligne] Exception : {ex.Message}");

            }
        }

    }



    public class GetDetailCompteInput
    {
        public string codeAgence { get; set; }
        public string deviseCompte { get; set; }
        public string numeroCompte { get; set; }
    }



}
