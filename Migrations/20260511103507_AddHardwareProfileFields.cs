using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcelikExcelApp.Migrations
{
    /// <inheritdoc />
    public partial class AddHardwareProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HardwareCpuId",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HardwareDiskId",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HardwareMotherboardId",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardwareCpuId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HardwareDiskId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HardwareMotherboardId",
                table: "Users");
        }
    }
}
