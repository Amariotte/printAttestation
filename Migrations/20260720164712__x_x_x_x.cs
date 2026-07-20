using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace print_attestation.Migrations
{
    /// <inheritdoc />
    public partial class _x_x_x_x : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_t_trace_action_r_entite_r_entite_id",
                table: "t_trace_action");

            migrationBuilder.DropColumn(
                name: "r_entite",
                table: "t_trace_action");

            migrationBuilder.DropColumn(
                name: "r_entite_id",
                table: "t_trace_action");

            migrationBuilder.RenameColumn(
                name: "r_description_action",
                table: "t_trace_action",
                newName: "r_description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "r_description",
                table: "t_trace_action",
                newName: "r_description_action");

            migrationBuilder.AddColumn<string>(
                name: "r_entite",
                table: "t_trace_action",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "r_entite_id",
                table: "t_trace_action",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_t_trace_action_r_entite_r_entite_id",
                table: "t_trace_action",
                columns: new[] { "r_entite", "r_entite_id" });
        }
    }
}
