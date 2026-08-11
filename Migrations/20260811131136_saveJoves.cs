using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class saveJoves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "r_attestations",
                table: "t_job",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "r_attestations",
                table: "t_job");
        }
    }
}
