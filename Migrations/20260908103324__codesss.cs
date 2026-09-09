using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _codesss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "r_user_traite_id_fk",
                table: "t_demande_annulation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "r_user_traiter_id",
                table: "t_demande_annulation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_r_user_traiter_id",
                table: "t_demande_annulation",
                column: "r_user_traiter_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_demande_annulation_t_user_r_user_traiter_id",
                table: "t_demande_annulation",
                column: "r_user_traiter_id",
                principalTable: "t_user",
                principalColumn: "r_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_demande_annulation_t_user_r_user_traiter_id",
                table: "t_demande_annulation");

            migrationBuilder.DropIndex(
                name: "IX_t_demande_annulation_r_user_traiter_id",
                table: "t_demande_annulation");

            migrationBuilder.DropColumn(
                name: "r_user_traite_id_fk",
                table: "t_demande_annulation");

            migrationBuilder.DropColumn(
                name: "r_user_traiter_id",
                table: "t_demande_annulation");
        }
    }
}
