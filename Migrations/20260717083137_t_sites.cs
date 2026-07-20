using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class t_sites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            

           
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_user_sites");

            migrationBuilder.DropTable(
                name: "t_site");
        }
    }
}
