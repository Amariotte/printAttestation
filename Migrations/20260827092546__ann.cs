using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _ann : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_motif_annulation",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_libelle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
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
                    table.PrimaryKey("PK_t_motif_annulation", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_demande_annulation",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_status = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    r_num_police = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_num_attestation = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_num_atd = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_motif_rejet = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_date_traitement = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_site_id_fk = table.Column<int>(type: "int", nullable: true),
                    r_user_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_motif_annulation_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_demande_annulation", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_demande_annulation_t_motif_annulation_r_motif_annulation_i~",
                        column: x => x.r_motif_annulation_id_fk,
                        principalTable: "t_motif_annulation",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_demande_annulation_t_site_r_site_id_fk",
                        column: x => x.r_site_id_fk,
                        principalTable: "t_site",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_t_demande_annulation_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_r_created_at",
                table: "t_demande_annulation",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_r_motif_annulation_id_fk",
                table: "t_demande_annulation",
                column: "r_motif_annulation_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_r_site_id_fk",
                table: "t_demande_annulation",
                column: "r_site_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_r_status",
                table: "t_demande_annulation",
                column: "r_status");

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_annulation_r_user_id_fk",
                table: "t_demande_annulation",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_motif_annulation_r_libelle",
                table: "t_motif_annulation",
                column: "r_libelle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_demande_annulation");

            migrationBuilder.DropTable(
                name: "t_motif_annulation");
        }
    }
}
