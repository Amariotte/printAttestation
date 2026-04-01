using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ask.Model
{
    public class t_base
    {
        [Key]
        public int r_id { get; set; }

        [JsonIgnore]
        public int? r_created_by { get; set; }
        [JsonIgnore]
        public DateTime? r_created_at { get; set; } = DateTime.Now;
        [JsonIgnore]
        public DateTime? r_updated_at { get; set; }
        [JsonIgnore]
        public int? r_updated_by { get; set; }
       
        public bool? r_is_active { get; set; } = true;
        [JsonIgnore]
        public bool? r_is_delete { get; set; } = false;
    }
}
