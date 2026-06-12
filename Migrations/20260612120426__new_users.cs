using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _new_users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "t_usersr_id",
                table: "t_user_scopes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_usersr_id",
                table: "t_session",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "t_usersr_id",
                table: "t_refresh_token",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "t_User_s",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_photo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_prenom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_last_login_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_failed_login_attempts = table.Column<int>(type: "int", nullable: false),
                    r_password_change_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_locked_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_statut = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_User_s", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scopes_t_usersr_id",
                table: "t_user_scopes",
                column: "t_usersr_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_session_t_usersr_id",
                table: "t_session",
                column: "t_usersr_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_refresh_token_t_usersr_id",
                table: "t_refresh_token",
                column: "t_usersr_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email1",
                table: "t_User_s",
                column: "r_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Telephone1",
                table: "t_User_s",
                column: "r_telephone");

            migrationBuilder.AddForeignKey(
                name: "FK_t_refresh_token_t_User_s_t_usersr_id",
                table: "t_refresh_token",
                column: "t_usersr_id",
                principalTable: "t_User_s",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_session_t_User_s_t_usersr_id",
                table: "t_session",
                column: "t_usersr_id",
                principalTable: "t_User_s",
                principalColumn: "r_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_user_scopes_t_User_s_t_usersr_id",
                table: "t_user_scopes",
                column: "t_usersr_id",
                principalTable: "t_User_s",
                principalColumn: "r_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_refresh_token_t_User_s_t_usersr_id",
                table: "t_refresh_token");

            migrationBuilder.DropForeignKey(
                name: "FK_t_session_t_User_s_t_usersr_id",
                table: "t_session");

            migrationBuilder.DropForeignKey(
                name: "FK_t_user_scopes_t_User_s_t_usersr_id",
                table: "t_user_scopes");

            migrationBuilder.DropTable(
                name: "t_User_s");

            migrationBuilder.DropIndex(
                name: "IX_t_user_scopes_t_usersr_id",
                table: "t_user_scopes");

            migrationBuilder.DropIndex(
                name: "IX_t_session_t_usersr_id",
                table: "t_session");

            migrationBuilder.DropIndex(
                name: "IX_t_refresh_token_t_usersr_id",
                table: "t_refresh_token");

            migrationBuilder.DropColumn(
                name: "t_usersr_id",
                table: "t_user_scopes");

            migrationBuilder.DropColumn(
                name: "t_usersr_id",
                table: "t_session");

            migrationBuilder.DropColumn(
                name: "t_usersr_id",
                table: "t_refresh_token");
        }
    }
}
