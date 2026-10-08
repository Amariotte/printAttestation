using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _typess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

         

            migrationBuilder.AddColumn<bool>(
                name: "r_file_atd_required",
                table: "t_motif_annulation",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "r_file_carte_grise_required",
                table: "t_motif_annulation",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "r_file_cpa_required",
                table: "t_motif_annulation",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "r_file_other_required",
                table: "t_motif_annulation",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

    
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            

           

            migrationBuilder.DropColumn(
                name: "r_file_atd_required",
                table: "t_motif_annulation");

            migrationBuilder.DropColumn(
                name: "r_file_carte_grise_required",
                table: "t_motif_annulation");

            migrationBuilder.DropColumn(
                name: "r_file_cpa_required",
                table: "t_motif_annulation");

            migrationBuilder.DropColumn(
                name: "r_file_other_required",
                table: "t_motif_annulation");

            migrationBuilder.CreateIndex(
                name: "IX_t_site_r_type_site_id_fk",
                table: "t_site",
                column: "r_type_site_id_fk");

        
        }
    }
}
