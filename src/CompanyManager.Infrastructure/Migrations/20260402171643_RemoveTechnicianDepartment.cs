using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTechnicianDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Technicians_DepartmentId",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Technicians");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Technicians",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_DepartmentId",
                table: "Technicians",
                column: "DepartmentId");
        }
    }
}
