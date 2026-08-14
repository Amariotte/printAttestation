using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    [Index(nameof(r_job_id), Name = "IX_Job_JobId")]
    [Index(nameof(r_user_id_fk), Name = "IX_Job_UserId")]
    public class t_job : t_base
    {
        [Required]
        [MaxLength(100)]
        public string r_job_id { get; set; } = string.Empty;

        /// <summary>
        /// Utilisateur qui a lancé la tâche (référence à t_user.r_id)
        /// </summary>

        public STATUT_JOB? r_status { get; set; }

        [MaxLength(500)]
        public string? r_file_name { get; set; }

        [MaxLength(2000)]
        public string? r_file_path { get; set; }
        [MaxLength(50)]
        public string? r_type { get; set; }

        public int r_total { get; set; }

        public int r_success { get; set; }

        public int r_errors { get; set; }

        public DateTime? r_completed_at { get; set; }

        public List<string>r_attestations { get; set; } = new();


        [Required]
        [ForeignKey(nameof(r_user))]
        public int r_user_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers l'utilisateur
        /// </summary>
        public t_user? r_user { get; set; }

        public virtual ICollection<t_job_details>? r_job_details { get; set; } = new List<t_job_details>();


        [NotMapped]
    public Channel<object> Events { get; set; }
            = Channel.CreateUnbounded<object>();

        // Permet d'annuler l'exécution en arrière-plan
        [NotMapped]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Threading.CancellationTokenSource? CancellationTokenSource { get; set; }

        public void Stop()
        {
            try
            {
                CancellationTokenSource?.Cancel();
            }
            catch { }
        }
    }


}
