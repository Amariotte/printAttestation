using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Controllers;
using InteroperabiliteProject.Implementation;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Net;
using System.Text;

namespace InteroperabiliteProject.Middleware
{
    public class TraceMidleware
    {
        public readonly RequestDelegate _next;
        private readonly ILogger<TraceMidleware> _logger;
        private readonly IDemandeRepo _demandeRepo;
        private readonly ITraceRepo _traceRepo;


        public TraceMidleware(RequestDelegate next, ILogger<TraceMidleware> logger, IDemandeRepo demandeRepo, ITraceRepo traceRepo)
        {
            _next = next;
            _logger = logger;
            _demandeRepo = demandeRepo;
            _traceRepo = traceRepo;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Sauvegarder le corps de la réponse originale
                var originalBodyStream = context.Response.Body;



                
    // Récupérer la description de la route
    var routeDescription = $"{context.Request.Method} {context.Request.Path}";

                // Cloner le corps de la requête
                var requestBodyStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);
                string requestBodyText = await new StreamReader(requestBodyStream).ReadToEndAsync();
                requestBodyStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = requestBodyStream;

                RouteValueDictionary ContainMobile = context.Request.RouteValues;
                string routePath = context.Request.Path.Value;

                var controllerBoll = ContainMobile.TryGetValue("controller", out var controller);
                var actionBoll = ContainMobile.TryGetValue("action", out var action);

                SENS_REQUETE_TRACE result = ContainMobile switch
                {
                    _ when controller?.ToString().Contains("Mobile") == true => SENS_REQUETE_TRACE.ReqMobileVersBack,
                    _ when controller?.ToString().Contains("Business") == true => SENS_REQUETE_TRACE.ReqbusinessVersBack,
                    _ when controller?.ToString().Contains("Reception") == true => SENS_REQUETE_TRACE.ReqFromAIP,
                    _ => SENS_REQUETE_TRACE.ReqFromAIP
                };

                var ret = await _traceRepo.AddTrace(result, routePath, requestBodyText);
                _logger.LogInformation("TraceMilledware---------------------------->AddTrace Ok");

                switch (result)
                {
                    case SENS_REQUETE_TRACE.ReqMobileVersBack:
                        context.Request.Headers["id-request-header"] = ret.Item2;
                        var ret_dmd = await _demandeRepo.AddDemande(controller?.ToString(), action?.ToString(), requestBodyText, action?.ToString(), "Description de la route à intégrer après");
                        _logger.LogInformation("TraceMilledware---------------------------->AddDemande Ok");
                        context.Request.Headers["id-dmd-header"] = ret_dmd.Item2;
                        break;
                    case SENS_REQUETE_TRACE.ReqFromAIP:
                        _logger.LogInformation($"TraceMilledware---------------------------->Body requête : {requestBodyText}");
                        break;
                        // Ajouter d'autres cas si nécessaire
                }

                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    await _next(context);

                    // Revenir au début du MemoryStream
                    responseBody.Seek(0, SeekOrigin.Begin);

                    // Lire le contenu de la réponse
                    var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();

                    responseBody.Seek(0, SeekOrigin.Begin);
                    var ret1 = await _traceRepo.UpdateResTrace(Convert.ToInt32(context.Request.Headers["id-request-header"]), responseBodyText);

                    int statusCode = context.Response.StatusCode;
                    int Iddemande = Convert.ToInt32(context.Request.Headers["id-dmd-header"]);


                  
                        if (Convert.ToInt32(statusCode.ToString().Substring(0, 1)) == 2)
                        {
                            await _demandeRepo.UpdateDemandeById(Iddemande, responseBodyText, Statut.SUCCES);
                        }
                        else
                        {
                            await _demandeRepo.UpdateDemandeById(Iddemande, responseBodyText, Statut.ECHEC);
                        }

                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"TraceMilledware---------------------------->Erreur ===================>{ex.Message}");
                throw;
            }
        }

      



       

   

    }
}