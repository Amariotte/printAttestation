using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _ms_bd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "r_chemin_fichier",
                table: "t_demande_annulation_fichier");

            migrationBuilder.AddColumn<string>(
                name: "r_nom_fichier_save",
                table: "t_demande_annulation_fichier",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "r_nom_fichier_save",
                table: "t_demande_annulation_fichier");

            migrationBuilder.AddColumn<string>(
                name: "r_chemin_fichier",
                table: "t_demande_annulation_fichier",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
