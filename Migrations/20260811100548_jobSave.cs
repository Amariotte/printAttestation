using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class jobSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_job",
                columns: table => new
                {
                    r_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    r_job_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    r_user_id = table.Column<int>(type: "int", nullable: true),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Job_JobId",
                table: "t_job",
                column: "r_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_Job_UserId",
                table: "t_job",
                column: "r_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_r_created_at",
                table: "t_job",
                column: "r_created_at");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_r_job_id",
                table: "t_job",
                column: "r_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_job_r_user_id",
                table: "t_job",
                column: "r_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_job");
        }
    }
}
