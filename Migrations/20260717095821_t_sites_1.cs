using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class t_sites_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_user_sites");

            migrationBuilder.AddColumn<int>(
                name: "r_site_id_fk",
                table: "t_user",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_user_r_site_id_fk",
                table: "t_user",
                column: "r_site_id_fk");

            migrationBuilder.AddForeignKey(
                name: "FK_t_user_t_site_r_site_id_fk",
                table: "t_user",
                column: "r_site_id_fk",
                principalTable: "t_site",
                principalColumn: "r_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_user_t_site_r_site_id_fk",
                table: "t_user");

            migrationBuilder.DropIndex(
                name: "IX_t_user_r_site_id_fk",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "r_site_id_fk",
                table: "t_user");

            migrationBuilder.CreateTable(
                name: "t_user_sites",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_site_id_fk = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_t_user_sites", x => x.r_id);
                    table.ForeignKey(
                        name: "FK_t_user_sites_t_site_r_site_id_fk",
                        column: x => x.r_site_id_fk,
                        principalTable: "t_site",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_user_sites_t_user_r_user_id_fk",
                        column: x => x.r_user_id_fk,
                        principalTable: "t_user",
                        principalColumn: "r_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_sites_r_site_id_fk",
                table: "t_user_sites",
                column: "r_site_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_UserSite_UserId_SiteId",
                table: "t_user_sites",
                columns: new[] { "r_user_id_fk", "r_site_id_fk" },
                unique: true);
        }
    }
}
