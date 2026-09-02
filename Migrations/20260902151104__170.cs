using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _170 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "r_joseph",
                table: "t_site",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_demande_annulation_fichier",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_demande_annulation_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_nom_fichier = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_chemin_fichier = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_demande_annulation_fichier", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_demande_annulation_fichier_t_demande_annulation_r_demande_~",
                        column: x => x.r_demande_annulation_id_fk,
                        principalTable: "t_demande_annulation",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DemandeAnnulationFichier_DemandeId",
                table: "t_demande_annulation_fichier",
                column: "r_demande_annulation_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_fichier_r_created_at",
                table: "t_demande_annulation_fichier",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_fichier_r_demande_annulation_id_fk",
                table: "t_demande_annulation_fichier",
                column: "r_demande_annulation_id_fk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_demande_annulation_fichier");

            migrationBuilder.DropColumn(
                name: "r_joseph",
                table: "t_site");
        }
    }
}
