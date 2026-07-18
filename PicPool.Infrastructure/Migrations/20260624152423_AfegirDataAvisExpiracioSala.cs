using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicPool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AfegirDataAvisExpiracioSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataAvisExpiracioEnviat",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Activa_DataExpiracio",
                table: "Sales",
                columns: new[] { "Activa", "DataExpiracio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_Activa_DataExpiracio",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DataAvisExpiracioEnviat",
                table: "Sales");
        }
    }
}
