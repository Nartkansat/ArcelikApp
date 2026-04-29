using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcelikExcelApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFileDataToUploadedFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "UploadedFiles",
                type: "longblob",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "UploadedFiles");
        }
    }
}
