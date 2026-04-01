using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteroperabiliteProject.ServicceAIP
{
    public class GeneraleRetourService
    {
        public bool operationStatus { get; set; }
        public string data { get; set; }
        public string erreur { get; set; }
        public int status { get; set; }
    }

     
    public class ProblemsDetails
    {
       
        public string type { get; set; }             // URI qui identifie le type de problème
        public string title { get; set; }            // Résumé du problème
        public int status { get; set; }              // Code HTTP
        public string detail { get; set; }           // Description spécifique
        public string instance { get; set; }         // URI de la requête
        public List<InvalidParam> invalidParams { get; set; } = new(); // Paramètres invalides
    }


    public enum sensFlux
    {
        ENTRANT = 1,
        SORTANT = 2,
        INTERNE = 3
    }

    public class GeneraleRetour : ProblemsDetails
    {
        public string? data { get; set; }
        public string? code { get; set; }

        public static ProblemsDetails BuildProblemResponse(GeneraleRetour r , string instance = null)
        {
            var defaultDetail = r.status switch
            {
                400 => "La requête est mal formée",
                401 => "Autorisations insuffisantes",
                403 => "Interdiction d'effectuer cette action",
                404 => "La ressource demandée est introuvable",
                405 => "Les données envoyées sont invalides",
                429 => "Le quota de requêtes a été dépassé",
                500 => "Une erreur inattendue s’est produite sur le serveur",
                502 => "Une erreur inattendue s’est produite sur le serveur",
                503 => "Une erreur inattendue s’est produite sur le serveur",
                _ => "Succès"
            };

            var title = r.status switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                409 => "Conflict",
                429 => "Too Many Requests",
                500 => "Internal Server Error",
                502 => "Internal Server Error",
                503 => "Internal Server Error",
                _ => "OK"
            };

            var type = $"https://example.com/probs/{r.status}";

            
            return new ProblemsDetails
            {
                status = r.status,
                type = type,
                title = title,
                detail = string.IsNullOrWhiteSpace(r.detail) ? defaultDetail : r.detail,
                instance = string.IsNullOrWhiteSpace(instance) ? r.instance : instance,
                invalidParams = r.invalidParams
            };
        }

        public static ProblemsDetails BuildProblemResponse500(string detail = null,string instance = null)
        {
            return BuildProblemResponse( new GeneraleRetour { status = 500 ,detail = detail }, instance);
        }

        public static ProblemsDetails BuildNotFound(string detail,string instance )
        {
            return BuildProblemResponse(new GeneraleRetour { status = 404,detail = detail }, instance);
        }

        public static ProblemsDetails BuildBadRequest(string detail, string instance , List<InvalidParam> invalidParams = null)
        {
            return BuildProblemResponse(new GeneraleRetour { status = 400, detail = detail , invalidParams = invalidParams }, instance);
        }

        public static ProblemsDetails BuildForbid(string detail, string instance , List<InvalidParam> invalidParams = null)
        {
            return BuildProblemResponse(new GeneraleRetour { status = 403, detail = detail, invalidParams = invalidParams }, instance);
        }

        public static ProblemsDetails BuildUnauthorized(string detail, string instance , List<InvalidParam> invalidParams = null)
        {
            return BuildProblemResponse(new GeneraleRetour { status = 401, detail = detail, invalidParams = invalidParams }, instance);
        }


        public static ProblemsDetails BuildProblemResponse429(string detail = null, string instance = null)
        {
            return BuildProblemResponse(new GeneraleRetour { status = 429, detail = detail }, instance);
        }



        public static ProblemsDetails BuildProblemResponse503( string instance)
        {
            return BuildProblemResponse(new GeneraleRetour { status = 503 }, instance);

        }
    }


    public class InvalidParam
    {
        public string name { get; set; }
        public string reason { get; set; }
    }


    public class GeneralMeta
    {
        public int length { get; set; }
        public int? page { get; set; }
        public int? limit { get; set; }
    }


    public class GeneralBusinessMeta
    {
        public int? total { get; set; }
        public int? size { get; set; }
        public string? page { get; set; }
        public string? next { get; set; }
        public string? prev { get; set; }
    }

}
