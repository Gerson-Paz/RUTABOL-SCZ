using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RutaBol.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTarifas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tarifas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "BOB"),
                    Vigente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAdministradorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarifas", x => x.Id);
                    table.CheckConstraint("CK_Tarifas_Monto", "\"Monto\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tarifas_Nombre_Vigente",
                table: "Tarifas",
                columns: new[] { "Nombre", "Vigente" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tarifas");
        }
    }
}
