using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _init : Migration
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
                    r_code = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_nom = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_scope", x => x.r_code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_site",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
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
                    table.PrimaryKey("PK_t_site", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_role_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_scope_code_fk = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                        name: "FK_t_role_scope_t_scope_r_scope_code_fk",
                        column: x => x.r_scope_code_fk,
                        principalTable: "t_scope",
                        principalColumn: "r_code",
                        onDelete: ReferentialAction.Cascade);
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
                    r_telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_prenom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_password_change_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_statut = table.Column<int>(type: "int", nullable: false),
                    r_date_last_statut = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_type = table.Column<int>(type: "int", nullable: false),
                    r_site_id_fk = table.Column<int>(type: "int", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_t_user_t_site_r_site_id_fk",
                        column: x => x.r_site_id_fk,
                        principalTable: "t_site",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Restrict);
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
                    r_num_immatriculation = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "t_job",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_job_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_status = table.Column<int>(type: "int", nullable: true),
                    r_file_name = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_file_path = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_total = table.Column<int>(type: "int", nullable: false),
                    r_success = table.Column<int>(type: "int", nullable: false),
                    r_errors = table.Column<int>(type: "int", nullable: false),
                    r_completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_attestations = table.Column<string>(type: "longtext", nullable: false)
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
                    table.PrimaryKey("PK_t_job", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_job_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
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
                    r_description = table.Column<string>(type: "longtext", nullable: true)
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
                name: "t_user_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_scope_code_fk = table.Column<string>(type: "varchar(255)", nullable: false)
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
                    table.PrimaryKey("PK_t_user_scope", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_user_scope_t_scope_r_scope_code_fk",
                        column: x => x.r_scope_code_fk,
                        principalTable: "t_scope",
                        principalColumn: "r_code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_scope_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_job_details",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_attestation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_desc_error = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_success = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    r_job_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_job_details", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_job_details_t_job_r_job_id_fk",
                        column: x => x.r_job_id_fk,
                        principalTable: "t_job",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "t_scope",
                columns: new[] { "r_code", "r_description", "r_nom" },
                values: new object[,]
                {
                    { "attestations.read", "Permet de consulter les attestations", "Lecture des attestations" },
                    { "attestations.read.all", "Permet de consulter les attestations de tous les intermediaires", "Lecture des attestations de tous les intermediaires" },
                    { "audits.acces.read", "Permet de consulter les audits liés aux connexions", "Lecture des audits connexions" },
                    { "audits.acces.read.all", "Permet de consulter les audits liés aux connexions de tous les utilisateurs", "Lecture des audits connexions de tous les utilisateurs" },
                    { "audits.acces.read.site", "Permet de consulter les audits liés aux connexions de mon site", "Lecture des audits connexions de mon site" },
                    { "audits.actions.read", "Permet de consulter les audits liés aux actions", "Lecture des audits actions" },
                    { "audits.actions.read.all", "Permet de consulter les audits liés aux actions de tous les utilisateurs", "Lecture des audits actions de tous les utilisateurs" },
                    { "audits.actions.read.site", "Permet de consulter les audits liés aux actions de mon site", "Lecture des audits actions de mon site" },
                    { "demandes-annulations.read", "Permet de consulter les demandes d'annulation", "Lecture des demandes d'annulation" },
                    { "demandes-annulations.read.all", "Permet de consulter les demandes d'annulation de tous les intermediaires", "Lecture des demandes d'annulation de tous les intermediaires" },
                    { "demandes-annulations.read.site", "Permet de consulter les demandes d'annulation de tous les intermediaires", "Lecture des demandes d'annulation de mon intérmediaire" },
                    { "taches.read", "Permet de consulter les taches", "Lecture des taches" },
                    { "taches.read.all", "Permet de consulter les taches de tous les utilisateurs", "Lecture des taches de tous les utilisateurs" },
                    { "taches.read.site", "Permet de consulter les taches de mon site", "Lecture des taches de mon site" },
                    { "taches.update", "Permet d'annuler les taches", "Annulation des taches" },
                    { "taches.update.all", "Permet d'annuler les taches de tous les utilisateurs", "Annulation des taches de tous les utilisateurs" },
                    { "taches.update.site", "Permet d'annuler les taches de mon site", "Annulation des taches de mon site" },
                    { "users.create", "Permet de créer des utilisateurs", "Création des utilisateurs" },
                    { "users.delete", "Permet de supprimer les utilisateurs", "Suppression des utilisateurs" },
                    { "users.read", "Permet de consulter les utilisateurs", "Lecture des utilisateurs" },
                    { "users.update", "Permet de modifier les utilisateurs", "Modification des utilisateurs" }
                });

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
                name: "IX_Job_JobId",
                table: "t_job",
                column: "r_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_Job_UserId",
                table: "t_job",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_r_created_at",
                table: "t_job",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_r_job_id",
                table: "t_job",
                column: "r_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_r_user_id_fk",
                table: "t_job",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_Job_JobIdFk",
                table: "t_job_details",
                column: "r_job_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_details_r_created_at",
                table: "t_job_details",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_details_r_job_id_fk",
                table: "t_job_details",
                column: "r_job_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_motif_annulation_r_libelle",
                table: "t_motif_annulation",
                column: "r_libelle");

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
                name: "IX_t_role_scope_r_role_id_fk",
                table: "t_role_scope",
                column: "r_role_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_role_scope_r_scope_code_fk",
                table: "t_role_scope",
                column: "r_scope_code_fk");

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
                name: "IX_Site_Code",
                table: "t_site",
                column: "r_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Site_Nom",
                table: "t_site",
                column: "r_nom");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_created_at",
                table: "t_trace_action",
                column: "r_created_at");

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

            migrationBuilder.CreateIndex(
                name: "IX_t_user_r_site_id_fk",
                table: "t_user",
                column: "r_site_id_fk");

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
                name: "IX_t_user_role_r_role_id_fk",
                table: "t_user_role",
                column: "r_role_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_role_r_user_id_fk",
                table: "t_user_role",
                column: "r_user_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scope_r_scope_code_fk",
                table: "t_user_scope",
                column: "r_scope_code_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_scope_r_user_id_fk",
                table: "t_user_scope",
                column: "r_user_id_fk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_demande_annulation");

            migrationBuilder.DropTable(
                name: "t_histo_email");

            migrationBuilder.DropTable(
                name: "t_histo_sms");

            migrationBuilder.DropTable(
                name: "t_job_details");

            migrationBuilder.DropTable(
                name: "t_modele");

            migrationBuilder.DropTable(
                name: "t_refresh_token");

            migrationBuilder.DropTable(
                name: "t_role_scope");

            migrationBuilder.DropTable(
                name: "t_session");

            migrationBuilder.DropTable(
                name: "t_trace_action");

            migrationBuilder.DropTable(
                name: "t_trace_connexion");

            migrationBuilder.DropTable(
                name: "t_user_role");

            migrationBuilder.DropTable(
                name: "t_user_scope");

            migrationBuilder.DropTable(
                name: "t_motif_annulation");

            migrationBuilder.DropTable(
                name: "t_job");

            migrationBuilder.DropTable(
                name: "t_role");

            migrationBuilder.DropTable(
                name: "t_scope");

            migrationBuilder.DropTable(
                name: "t_user");

            migrationBuilder.DropTable(
                name: "t_site");
        }
    }
}
