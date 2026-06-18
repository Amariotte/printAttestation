using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _user_maj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "r_failed_login_attempts",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "r_last_login_at",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "r_locked_until",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "r_photo",
                table: "t_user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "r_failed_login_attempts",
                table: "t_user",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "r_last_login_at",
                table: "t_user",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "r_locked_until",
                table: "t_user",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "r_photo",
                table: "t_user",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
