using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _upd_second_01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "t_scope",
                columns: new[] { "r_code", "r_description", "r_nom" },
                values: new object[,]
                {
                    { "roles.create", "Permet de créer les profils d'utilisateurs", "Création des profils d'utilisateurs" },
                    { "roles.delete", "Permet de supprimer les profils d'utilisateurs", "Suppression des profils d'utilisateurs" },
                    { "roles.read", "Permet de consulter les profils d'utilisateurs", "Lecture des profils d'utilisateurs" },
                    { "roles.update", "Permet de modifier les profils d'utilisateurs", "Modification des profils d'utilisateurs" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "roles.create");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "roles.delete");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "roles.read");

            migrationBuilder.DeleteData(
                table: "t_scope",
                keyColumn: "r_code",
                keyValue: "roles.update");
        }
    }
}
