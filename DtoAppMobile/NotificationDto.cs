using InteroperabiliteProject.Model;
using System.Text.Json;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class NotificationDto
    {
        public string? id { get; set; }
        public string? type { get; set; }
        public string? idObject { get; set; }

        public DateTime? dateAction { get; set; }
        public DateTime? dateLecture { get; set; }
        public bool? estCliquable { get; set; }
        public JsonDocument? details { get; set; }

    }
}


