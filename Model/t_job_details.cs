using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace print_attestation.Model
{
    [Index(nameof(r_job_id_fk), Name = "IX_Job_JobIdFk")]
    public class t_job_details : t_base
    {
        [Required]
       

        public string? r_attestation { get; set; }
        public string? r_type { get; set; }

        public string? r_desc_error { get; set; }
        public bool? r_success { get; set; }
       

        [Required]
        [ForeignKey(nameof(r_job))]
        public int r_job_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers l'utilisateur
        /// </summary>
        public t_job? r_job { get; set; }
    
    }


}
