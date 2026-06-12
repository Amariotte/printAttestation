using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class maj_users_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_user_scopes");

            migrationBuilder.DropTable(
                name: "t_scope");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_scope",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_scope", x => x.r_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_user_scopes",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_scope_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_user_id_fk = table.Column<int>(type: "int", nullable: false),
                    r_created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_created_by = table.Column<int>(type: "int", nullable: true),
                    r_is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_is_delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    r_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    r_updated_by = table.Column<int>(type: "int", nullable: true),
                    t_scoper_id = table.Column<int>(type: "int", nullable: false),
                    t_scoper_id1 = table.Column<int>(type: "int", nullable: false),
                    t_userr_id = table.Column<int>(type: "int", nullable: false),
                    t_userr_id1 = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_Scope_Nom",
                table: "t_scope",
                column: "r_nom",
                unique: true);

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
    }
}
