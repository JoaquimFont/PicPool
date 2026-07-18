using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicPool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AfegirPesTotalITotalImatgesSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PesTotal",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalImatges",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PesTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TotalImatges",
                table: "Sales");
        }
    }
}
