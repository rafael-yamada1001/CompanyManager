using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleClientStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Client",
                table: "TechnicianSchedules",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TechnicianSchedules",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "confirmado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Client",
                table: "TechnicianSchedules");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TechnicianSchedules");
        }
    }
}
