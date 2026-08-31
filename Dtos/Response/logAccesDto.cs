
using print_attestation.Dtos.Response.auth;

namespace print_attestation.Dtos.Response
{
    public class logAccesDto
    {
        public int? id { get; set; }
        public int? userId { get; set; }
        public string? userEmail { get; set; }
        public DateTime? date { get; set; }
        public string? typeEvenement { get; set; }
        public string? detailJson { get; set; }
        public string? ip { get; set; }
        public string? userAgent { get; set; }
        public bool? success { get; set; }
        public string? raisonEchec { get; set; }

        public UserResponseDto? user { get; set; }
    }
}

