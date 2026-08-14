using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _init_data_02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_t_job_details_r_created_at",
                table: "t_job_details",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_details_r_job_id_fk",
                table: "t_job_details",
                column: "r_job_id_fk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_t_job_details_r_created_at",
                table: "t_job_details");

            migrationBuilder.DropIndex(
                name: "IX_t_job_details_r_job_id_fk",
                table: "t_job_details");
        }
    }
}
