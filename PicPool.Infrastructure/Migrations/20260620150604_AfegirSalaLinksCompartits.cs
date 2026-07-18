using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicPool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AfegirSalaLinksCompartits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalaLinksCompartits",
                columns: table => new
                {
                    SalaLinkCompartitPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PotVeure = table.Column<bool>(type: "bit", nullable: false),
                    PotPujar = table.Column<bool>(type: "bit", nullable: false),
                    PotDescarregar = table.Column<bool>(type: "bit", nullable: false),
                    PotEliminarPropies = table.Column<bool>(type: "bit", nullable: false),
                    PotEliminarQualsevol = table.Column<bool>(type: "bit", nullable: false),
                    PotGestionarSala = table.Column<bool>(type: "bit", nullable: false),
                    Actiu = table.Column<bool>(type: "bit", nullable: false),
                    DataCreacio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataExpiracio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LimitUsos = table.Column<int>(type: "int", nullable: true),
                    UsosActuals = table.Column<int>(type: "int", nullable: false),
                    UsuariCreadorPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaLinksCompartits", x => x.SalaLinkCompartitPK);
                    table.ForeignKey(
                        name: "FK_SalaLinksCompartits_Sales_SalaPK",
                        column: x => x.SalaPK,
                        principalTable: "Sales",
                        principalColumn: "SalaPK",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaLinksCompartits_Usuaris_UsuariCreadorPK",
                        column: x => x.UsuariCreadorPK,
                        principalTable: "Usuaris",
                        principalColumn: "UsuariPK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaLinksCompartits_SalaPK",
                table: "SalaLinksCompartits",
                column: "SalaPK");

            migrationBuilder.CreateIndex(
                name: "IX_SalaLinksCompartits_Token",
                table: "SalaLinksCompartits",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaLinksCompartits_UsuariCreadorPK",
                table: "SalaLinksCompartits",
                column: "UsuariCreadorPK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalaLinksCompartits");
        }
    }
}
