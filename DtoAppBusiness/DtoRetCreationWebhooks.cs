using System.Text.Json.Serialization;

namespace InteroperabiliteProject.DtoAppBusiness
{
    public class DtoRetCreationWebhooks
    {
        [JsonPropertyName("id")]
        public string id_data { get; set; }
        public string callbackUrl { get; set; }
        public string[] events { get; set; }
        public DateTime dateCreation { get; set; }
        public string? secret { get; set; }
        public string? alias { get; set; }
    }
}
