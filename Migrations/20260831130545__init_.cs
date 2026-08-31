using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _init_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "t_scope",
                columns: new[] { "r_code", "r_description", "r_nom" },
                values: new object[,]
                {
                    { "sites.create", "Permet de créer des intermédiaires", "Création des intermédiaires" },
                    { "sites.delete", "Permet de supprimer les intermédiaires", "Suppression des intermédiaires" },
                    { "sites.read", "Permet de consulter les intermédiaires", "Lecture des intermédiaires" },
                    { "sites.update", "Permet de modifier les intermédiaires", "Modification des intermédiaires" },
                    { "sites.upload", "Permet de téléverser les intermédiaires", "Téléversement des intermédiaires" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "sites.create");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "sites.delete");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "sites.read");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "sites.update");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "sites.upload");
        }
    }
}
