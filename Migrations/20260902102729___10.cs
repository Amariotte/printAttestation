using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class __10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_role_scope");

            migrationBuilder.DropTable(
                name: "t_user_role");

            migrationBuilder.DropTable(
                name: "t_scope");

            migrationBuilder.DropTable(
                name: "t_role");

            migrationBuilder.AddColumn<int>(
                name: "r_type",
                table: "t_user",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "r_type",
                table: "t_user");

            migrationBuilder.CreateTable(
                name: "t_role",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_code = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_nom = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_ordre = table.Column<int>(type: "int", nullable: true),
                    r_sites_types = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true)
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
                    r_description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_nom = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_scope", x => x.r_code);
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
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true)
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
                    r_role_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_scope_code_fk = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.InsertData(
                table: "t_scope",
                columns: new[] { "r_code", "r_description", "r_nom" },
                values: new object[,]
                {
                    { "attestations.read", "Permet de consulter les attestations", "Lecture des attestations" },
                    { "attestations.read.all", "Permet de consulter les attestations de tous les intermediaires", "Lecture des attestations de tous les intermediaires" },
                    { "audits.acces.read", "Permet de consulter les audits liés aux connexions", "Lecture des audits connexions" },
                    { "audits.acces.read.site", "Permet de consulter les audits liés aux connexions de mon site", "Lecture des audits connexions de mon site" },
                    { "audits.actions.read", "Permet de consulter les audits liés aux actions", "Lecture des audits actions" },
                    { "audits.actions.read.site", "Permet de consulter les audits liés aux actions de mon site", "Lecture des audits actions de mon site" },
                    { "demandes-annulations.read", "Permet de consulter les demandes d'annulation", "Lecture des demandes d'annulation" },
                    { "demandes-annulations.read.site", "Permet de consulter les demandes d'annulation de tous les intermediaires", "Lecture des demandes d'annulation de mon intérmediaire" },
                    { "motifs-annulation.create", "Permet de créer les motifs d'annulation", "Création des motifs d'annulation" },
                    { "motifs-annulation.delete", "Permet de supprimer les motifs d'annulation", "Suppression des motifs d'annulation" },
                    { "motifs-annulation.read", "Permet de consulter les motifs d'annulation", "Lecture des motifs d'annulation" },
                    { "motifs-annulation.update", "Permet de modifier les motifs d'annulation", "Modification des motifs d'annulation" },
                    { "roles.create", "Permet de créer les profils d'utilisateurs", "Création des profils d'utilisateurs" },
                    { "roles.delete", "Permet de supprimer les profils d'utilisateurs", "Suppression des profils d'utilisateurs" },
                    { "roles.read", "Permet de consulter les profils d'utilisateurs", "Lecture des profils d'utilisateurs" },
                    { "roles.update", "Permet de modifier les profils d'utilisateurs", "Modification des profils d'utilisateurs" },
                    { "sites.create", "Permet de créer des intermédiaires", "Création des intermédiaires" },
                    { "sites.delete", "Permet de supprimer les intermédiaires", "Suppression des intermédiaires" },
                    { "sites.read", "Permet de consulter les intermédiaires", "Lecture des intermédiaires" },
                    { "sites.update", "Permet de modifier les intermédiaires", "Modification des intermédiaires" },
                    { "sites.upload", "Permet de téléverser les intermédiaires", "Téléversement des intermédiaires" },
                    { "taches.read", "Permet de consulter les taches", "Lecture des taches" },
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
                name: "IX_t_role_scope_r_role_id_fk",
                table: "t_role_scope",
                column: "r_role_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_role_scope_r_scope_code_fk",
                table: "t_role_scope",
                column: "r_scope_code_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_role_r_role_id_fk",
                table: "t_user_role",
                column: "r_role_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_role_r_user_id_fk",
                table: "t_user_role",
                column: "r_user_id_fk");
        }
    }
}
