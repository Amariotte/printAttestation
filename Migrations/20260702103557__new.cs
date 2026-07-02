using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "r_date_last_statut",
                table: "t_user",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "r_date_last_statut",
                table: "t_user");
        }
    }
}
