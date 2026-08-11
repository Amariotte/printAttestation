
using ask.Dtos.Request.auth;
using ask.Dtos.Response.auth;

namespace ask.Dtos.Reponses
{
    public class jobReponseDto
    {
        public int? id { get; set; }
        public string? jobId { get; set; }
        public int? userId { get; set; }
        public DateTime? completedAt { get; set; }
        public DateTime? createdAt { get; set; }
        public string? fileName { get; set; } = null;

        public string? type { get; set; }
        public int? nbTotal { get; set; }
        public int? nbSuccess { get; set; }
        public int? nbErrors { get; set; }
        public STATUT_JOB? status { get; set; }

        public UserResponseDto? user { get; set; }
    }
}
