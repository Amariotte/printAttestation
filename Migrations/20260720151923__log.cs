using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _log : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_trace_action",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_user_id = table.Column<int>(type: "int", nullable: true),
                    r_user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_type_action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_entite = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_entite_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_details_json = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_http_method = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_endpoint = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_description_action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_status_code = table.Column<int>(type: "int", nullable: true),
                    r_duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_trace_action", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_trace_action_t_user_r_user_id",
                        column: x => x.r_user_id,
                        principalTable: "t_user",
                        principalColumn: "r_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_trace_connexion",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_user_id = table.Column<int>(type: "int", nullable: true),
                    r_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_type_evenement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_succes = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_raison_echec = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_session_token_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_pays = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_ville = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_details_json = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    r_token_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_trace_connexion", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_trace_connexion_t_user_r_user_id",
                        column: x => x.r_user_id,
                        principalTable: "t_user",
                        principalColumn: "r_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_created_at",
                table: "t_trace_action",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_entite_r_entite_id",
                table: "t_trace_action",
                columns: new[] { "r_entite", "r_entite_id" });

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_type_action",
                table: "t_trace_action",
                column: "r_type_action");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_user_id",
                table: "t_trace_action",
                column: "r_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_user_id_r_created_at",
                table: "t_trace_action",
                columns: new[] { "r_user_id", "r_created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_connexion_r_created_at",
                table: "t_trace_connexion",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_connexion_r_email",
                table: "t_trace_connexion",
                column: "r_email");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_connexion_r_ip_address",
                table: "t_trace_connexion",
                column: "r_ip_address");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_connexion_r_succes_r_created_at",
                table: "t_trace_connexion",
                columns: new[] { "r_succes", "r_created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_connexion_r_user_id",
                table: "t_trace_connexion",
                column: "r_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_connexion_r_user_id_r_created_at",
                table: "t_trace_connexion",
                columns: new[] { "r_user_id", "r_created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_trace_action");

            migrationBuilder.DropTable(
                name: "t_trace_connexion");
        }
    }
}
