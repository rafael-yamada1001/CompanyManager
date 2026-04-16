using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianIsFullTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullTime",
                table: "Technicians",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFullTime",
                table: "Technicians");
        }
    }
}
