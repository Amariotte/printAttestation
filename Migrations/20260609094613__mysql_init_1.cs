using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _mysql_init_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_histo_email",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_sender_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_sender_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_recipients = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_statut = table.Column<int>(type: "int", nullable: false),
                    r_raison_echec = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_cc = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_bcc = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_is_html = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_histo_email", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_histo_sms",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_sender = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_text = table.Column<string>(type: "varchar(1600)", maxLength: 1600, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_recipient = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_statut = table.Column<int>(type: "int", nullable: false),
                    r_raison_echec = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_provider_message_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
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
                    table.PrimaryKey("PK_t_histo_sms", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_modele",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_subject = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_body = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_plateforme = table.Column<int>(type: "int", nullable: true),
                    r_type = table.Column<int>(type: "int", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_modele", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
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
                name: "t_user",
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
                    table.PrimaryKey("PK_t_user", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_refresh_token",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_token = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_jti = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    r_is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_replaced_by = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_t_refresh_token", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_refresh_token_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_session",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_token_jti = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_login_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    r_logout_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_user_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_session", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_session_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_user_scopes",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_user_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_scope_id_fk = table.Column<int>(type: "int", nullable: false),
                    t_scoper_id = table.Column<int>(type: "int", nullable: false),
                    t_scoper_id1 = table.Column<int>(type: "int", nullable: false),
                    t_userr_id = table.Column<int>(type: "int", nullable: false),
                    t_userr_id1 = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_user_scopes", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_user_scopes_t_scope_r_scope_id_fk",
                        column: x => x.r_scope_id_fk,
                        principalTable: "t_scope",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_scopes_t_scope_t_scoper_id1",
                        column: x => x.t_scoper_id1,
                        principalTable: "t_scope",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_scopes_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_scopes_t_user_t_userr_id1",
                        column: x => x.t_userr_id1,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HistoEmail_CreatedAt",
                table: "t_histo_email",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_HistoEmail_Recipients",
                table: "t_histo_email",
                column: "r_recipients");

            migrationBuilder.CreateIndex(
                name: "IX_HistoEmail_Statut",
                table: "t_histo_email",
                column: "r_statut");

            migrationBuilder.CreateIndex(
                name: "IX_HistoSms_CreatedAt",
                table: "t_histo_sms",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_HistoSms_Recipient",
                table: "t_histo_sms",
                column: "r_recipient");

            migrationBuilder.CreateIndex(
                name: "IX_HistoSms_Statut",
                table: "t_histo_sms",
                column: "r_statut");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ExpiresAt",
                table: "t_refresh_token",
                column: "r_expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ExpiresAt_IsDelete",
                table: "t_refresh_token",
                columns: new[] { "r_expires_at", "r_is_delete" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_Jti",
                table: "t_refresh_token",
                column: "r_jti");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_Token",
                table: "t_refresh_token",
                column: "r_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "t_refresh_token",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_IsRevoked_ExpiresAt",
                table: "t_refresh_token",
                columns: new[] { "r_user_id_fk", "r_is_revoked", "r_expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Scope_Nom",
                table: "t_scope",
                column: "r_nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Session_IsActive",
                table: "t_session",
                column: "r_is_active");

            migrationBuilder.CreateIndex(
                name: "IX_Session_LoginAt",
                table: "t_session",
                column: "r_login_at");

            migrationBuilder.CreateIndex(
                name: "IX_Session_TokenJti",
                table: "t_session",
                column: "r_token_jti");

            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId",
                table: "t_session",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId_IsActive_LoginAt",
                table: "t_session",
                columns: new[] { "r_user_id_fk", "r_is_active", "r_login_at" });

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "t_user",
                column: "r_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Telephone",
                table: "t_user",
                column: "r_telephone");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scopes_r_scope_id_fk",
                table: "t_user_scopes",
                column: "r_scope_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scopes_t_scoper_id1",
                table: "t_user_scopes",
                column: "t_scoper_id1");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scopes_t_userr_id1",
                table: "t_user_scopes",
                column: "t_userr_id1");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_UserId_ScopeId",
                table: "t_user_scopes",
                columns: new[] { "r_user_id_fk", "r_scope_id_fk" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_histo_email");

            migrationBuilder.DropTable(
                name: "t_histo_sms");

            migrationBuilder.DropTable(
                name: "t_modele");

            migrationBuilder.DropTable(
                name: "t_refresh_token");

            migrationBuilder.DropTable(
                name: "t_session");

            migrationBuilder.DropTable(
                name: "t_user_scopes");

            migrationBuilder.DropTable(
                name: "t_scope");

            migrationBuilder.DropTable(
                name: "t_user");
        }
    }
}
