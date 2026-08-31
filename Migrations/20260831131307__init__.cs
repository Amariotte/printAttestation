using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _init__ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "t_scope",
                columns: new[] { "r_code", "r_description", "r_nom" },
                values: new object[,]
                {
                    { "motifs-annulation.create", "Permet de créer les motifs d'annulation", "Création des motifs d'annulation" },
                    { "motifs-annulation.delete", "Permet de supprimer les motifs d'annulation", "Suppression des motifs d'annulation" },
                    { "motifs-annulation.read", "Permet de consulter les motifs d'annulation", "Lecture des motifs d'annulation" },
                    { "motifs-annulation.update", "Permet de modifier les motifs d'annulation", "Modification des motifs d'annulation" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "motifs-annulation.create");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "motifs-annulation.delete");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "motifs-annulation.read");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "motifs-annulation.update");
        }
    }
}
