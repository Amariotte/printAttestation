using Newtonsoft.Json;

namespace InteroperabiliteProject.Dtos
{
    public class QuerySmsDto
    {
        public string? identify { get; set; }
        public string? fromad { get; set; }
        public string? toad { get; set; } 
        public string? msgid { get; set; }
        public string? text { get; set; }

        public string? srvce { get; set; }

        [JsonProperty("class")]
        public string class_ { get; set; } = "ASK";

        public string? pwd { get; set; }
       
    }
}