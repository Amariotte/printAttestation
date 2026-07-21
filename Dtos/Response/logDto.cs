using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ask.Dtos.Request.auth;
using ask.Dtos.Response.auth;
using ask.Model;

namespace ask.Dtos.Response
{
    public class logDto
    {
        public int? id { get; set; }
        public int? userId { get; set; }
        public string? userEmail { get; set; }
        public DateTime? date { get; set; }
        public string? typeAction { get; set; }
        public string? detailJson { get; set; }
        public string? ip { get; set; }
        public string? userAgent { get; set; }
        public string? httpMethod { get; set; }
        public string? endpoint { get; set; }
        public string? description { get; set; }
        public int? statusCode { get; set; }
        public long? durationMs { get; set; }

        public UserResponseDto? user { get; set; }
    }
}
