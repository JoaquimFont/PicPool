using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicPool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LimitEmmagatzematgeBytes = table.Column<long>(type: "bigint", nullable: false),
                    LimitSales = table.Column<int>(type: "int", nullable: false),
                    LimitImatges = table.Column<int>(type: "int", nullable: false),
                    Preu = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Actiu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlaPK);
                });

            migrationBuilder.CreateTable(
                name: "Usuaris",
                columns: table => new
                {
                    UsuariPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataAlta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actiu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuaris", x => x.UsuariPK);
                });

            migrationBuilder.CreateTable(
                name: "Imatges",
                columns: table => new
                {
                    ImatgePK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NomOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RutaStorage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MidaBytes = table.Column<long>(type: "bigint", nullable: false),
                    TipusMime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsuariPujadorPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataPujada = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imatges", x => x.ImatgePK);
                    table.ForeignKey(
                        name: "FK_Imatges_Usuaris_UsuariPujadorPK",
                        column: x => x.UsuariPujadorPK,
                        principalTable: "Usuaris",
                        principalColumn: "UsuariPK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    SalaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TokenAcces = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UsuariCreadorPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataCreacio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataExpiracio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.SalaPK);
                    table.ForeignKey(
                        name: "FK_Sales_Usuaris_UsuariCreadorPK",
                        column: x => x.UsuariCreadorPK,
                        principalTable: "Usuaris",
                        principalColumn: "UsuariPK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuariPlans",
                columns: table => new
                {
                    UsuariPlaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsuariPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataInici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Actiu = table.Column<bool>(type: "bit", nullable: false),
                    EspaiConsumitBytes = table.Column<long>(type: "bigint", nullable: false),
                    SalesCreades = table.Column<int>(type: "int", nullable: false),
                    ImatgesPujades = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariPlans", x => x.UsuariPlaPK);
                    table.ForeignKey(
                        name: "FK_UsuariPlans_Plans_PlaPK",
                        column: x => x.PlaPK,
                        principalTable: "Plans",
                        principalColumn: "PlaPK",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuariPlans_Usuaris_UsuariPK",
                        column: x => x.UsuariPK,
                        principalTable: "Usuaris",
                        principalColumn: "UsuariPK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaImatges",
                columns: table => new
                {
                    ImatgeSalaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImatgePK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaImatges", x => x.ImatgeSalaPK);
                    table.ForeignKey(
                        name: "FK_SalaImatges_Imatges_ImatgePK",
                        column: x => x.ImatgePK,
                        principalTable: "Imatges",
                        principalColumn: "ImatgePK",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaImatges_Sales_SalaPK",
                        column: x => x.SalaPK,
                        principalTable: "Sales",
                        principalColumn: "SalaPK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaUsuaris",
                columns: table => new
                {
                    SalaUsuariPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalaPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsuariPK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PotVeure = table.Column<bool>(type: "bit", nullable: false),
                    PotPujar = table.Column<bool>(type: "bit", nullable: false),
                    PotDescarregar = table.Column<bool>(type: "bit", nullable: false),
                    PotEliminarPropies = table.Column<bool>(type: "bit", nullable: false),
                    PotEliminarQualsevol = table.Column<bool>(type: "bit", nullable: false),
                    PotGestionarSala = table.Column<bool>(type: "bit", nullable: false),
                    DataUnio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaUsuaris", x => x.SalaUsuariPK);
                    table.ForeignKey(
                        name: "FK_SalaUsuaris_Sales_SalaPK",
                        column: x => x.SalaPK,
                        principalTable: "Sales",
                        principalColumn: "SalaPK",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaUsuaris_Usuaris_UsuariPK",
                        column: x => x.UsuariPK,
                        principalTable: "Usuaris",
                        principalColumn: "UsuariPK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Imatges_UsuariPujadorPK",
                table: "Imatges",
                column: "UsuariPujadorPK");

            migrationBuilder.CreateIndex(
                name: "IX_SalaImatges_ImatgePK",
                table: "SalaImatges",
                column: "ImatgePK");

            migrationBuilder.CreateIndex(
                name: "IX_SalaImatges_SalaPK_ImatgePK",
                table: "SalaImatges",
                columns: new[] { "SalaPK", "ImatgePK" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaUsuaris_SalaPK_UsuariPK",
                table: "SalaUsuaris",
                columns: new[] { "SalaPK", "UsuariPK" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaUsuaris_UsuariPK",
                table: "SalaUsuaris",
                column: "UsuariPK");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TokenAcces",
                table: "Sales",
                column: "TokenAcces",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UsuariCreadorPK",
                table: "Sales",
                column: "UsuariCreadorPK");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariPlans_PlaPK",
                table: "UsuariPlans",
                column: "PlaPK");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariPlans_UsuariPK",
                table: "UsuariPlans",
                column: "UsuariPK");

            migrationBuilder.CreateIndex(
                name: "IX_Usuaris_Email",
                table: "Usuaris",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalaImatges");

            migrationBuilder.DropTable(
                name: "SalaUsuaris");

            migrationBuilder.DropTable(
                name: "UsuariPlans");

            migrationBuilder.DropTable(
                name: "Imatges");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Usuaris");
        }
    }
}
