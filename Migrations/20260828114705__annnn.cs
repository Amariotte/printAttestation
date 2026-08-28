using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _annnn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "t_roler_id",
                table: "t_session",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_scoper_id",
                table: "t_session",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_roler_id",
                table: "t_refresh_token",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_scoper_id",
                table: "t_refresh_token",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_roler_id",
                table: "t_job",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_scoper_id",
                table: "t_job",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "t_role",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_ordre = table.Column<int>(type: "int", nullable: true),
                    r_nom = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_code = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_description = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_t_role", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_nom = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_code = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_description = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_t_scope", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_user_role",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_role_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_user_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_user_role", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_user_role_t_role_r_role_id_fk",
                        column: x => x.r_role_id_fk,
                        principalTable: "t_role",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_role_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_role_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_scope_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_role_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_role_scope", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_role_scope_t_role_r_role_id_fk",
                        column: x => x.r_role_id_fk,
                        principalTable: "t_role",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_role_scope_t_scope_r_scope_id_fk",
                        column: x => x.r_scope_id_fk,
                        principalTable: "t_scope",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_user_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_scope_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_user_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_user_scope", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_user_scope_t_scope_r_scope_id_fk",
                        column: x => x.r_scope_id_fk,
                        principalTable: "t_scope",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_scope_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_t_session_t_roler_id",
                table: "t_session",
                column: "t_roler_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_session_t_scoper_id",
                table: "t_session",
                column: "t_scoper_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_refresh_token_t_roler_id",
                table: "t_refresh_token",
                column: "t_roler_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_refresh_token_t_scoper_id",
                table: "t_refresh_token",
                column: "t_scoper_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_t_roler_id",
                table: "t_job",
                column: "t_roler_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_t_scoper_id",
                table: "t_job",
                column: "t_scoper_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_role_scope_r_role_id_fk",
                table: "t_role_scope",
                column: "r_role_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_role_scope_r_scope_id_fk",
                table: "t_role_scope",
                column: "r_scope_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_role_r_role_id_fk",
                table: "t_user_role",
                column: "r_role_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_role_r_user_id_fk",
                table: "t_user_role",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scope_r_scope_id_fk",
                table: "t_user_scope",
                column: "r_scope_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scope_r_user_id_fk",
                table: "t_user_scope",
                column: "r_user_id_fk");

            migrationBuilder.AddForeignKey(
                name: "FK_t_job_t_role_t_roler_id",
                table: "t_job",
                column: "t_roler_id",
                principalTable: "t_role",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_job_t_scope_t_scoper_id",
                table: "t_job",
                column: "t_scoper_id",
                principalTable: "t_scope",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_refresh_token_t_role_t_roler_id",
                table: "t_refresh_token",
                column: "t_roler_id",
                principalTable: "t_role",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_refresh_token_t_scope_t_scoper_id",
                table: "t_refresh_token",
                column: "t_scoper_id",
                principalTable: "t_scope",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_session_t_role_t_roler_id",
                table: "t_session",
                column: "t_roler_id",
                principalTable: "t_role",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_session_t_scope_t_scoper_id",
                table: "t_session",
                column: "t_scoper_id",
                principalTable: "t_scope",
                principalColumn: "r_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_job_t_role_t_roler_id",
                table: "t_job");

            migrationBuilder.DropForeignKey(
                name: "FK_t_job_t_scope_t_scoper_id",
                table: "t_job");

            migrationBuilder.DropForeignKey(
                name: "FK_t_refresh_token_t_role_t_roler_id",
                table: "t_refresh_token");

            migrationBuilder.DropForeignKey(
                name: "FK_t_refresh_token_t_scope_t_scoper_id",
                table: "t_refresh_token");

            migrationBuilder.DropForeignKey(
                name: "FK_t_session_t_role_t_roler_id",
                table: "t_session");

            migrationBuilder.DropForeignKey(
                name: "FK_t_session_t_scope_t_scoper_id",
                table: "t_session");

            migrationBuilder.DropTable(
                name: "t_role_scope");

            migrationBuilder.DropTable(
                name: "t_user_role");

            migrationBuilder.DropTable(
                name: "t_user_scope");

            migrationBuilder.DropTable(
                name: "t_role");

            migrationBuilder.DropTable(
                name: "t_scope");

            migrationBuilder.DropIndex(
                name: "IX_t_session_t_roler_id",
                table: "t_session");

            migrationBuilder.DropIndex(
                name: "IX_t_session_t_scoper_id",
                table: "t_session");

            migrationBuilder.DropIndex(
                name: "IX_t_refresh_token_t_roler_id",
                table: "t_refresh_token");

            migrationBuilder.DropIndex(
                name: "IX_t_refresh_token_t_scoper_id",
                table: "t_refresh_token");

            migrationBuilder.DropIndex(
                name: "IX_t_job_t_roler_id",
                table: "t_job");

            migrationBuilder.DropIndex(
                name: "IX_t_job_t_scoper_id",
                table: "t_job");

            migrationBuilder.DropColumn(
                name: "t_roler_id",
                table: "t_session");

            migrationBuilder.DropColumn(
                name: "t_scoper_id",
                table: "t_session");

            migrationBuilder.DropColumn(
                name: "t_roler_id",
                table: "t_refresh_token");

            migrationBuilder.DropColumn(
                name: "t_scoper_id",
                table: "t_refresh_token");

            migrationBuilder.DropColumn(
                name: "t_roler_id",
                table: "t_job");

            migrationBuilder.DropColumn(
                name: "t_scoper_id",
                table: "t_job");
        }
    }
}
