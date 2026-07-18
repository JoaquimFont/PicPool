using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicPool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AfegirSourceUrlAImatge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Imatges",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Imatges");
        }
    }
}
