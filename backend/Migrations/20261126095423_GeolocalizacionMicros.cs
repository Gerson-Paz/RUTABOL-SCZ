using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaBol.Api.Migrations
{
    /// <inheritdoc />
    public partial class GeolocalizacionMicros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UbicacionesMicro",
                columns: table => new
                {
                    MicroId = table.Column<int>(type: "integer", nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: false),
                    Longitud = table.Column<double>(type: "double precision", nullable: false),
                    PrecisionMetros = table.Column<double>(type: "double precision", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConductorUsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbicacionesMicro", x => x.MicroId);
                    table.ForeignKey(
                        name: "FK_UbicacionesMicro_Micros_MicroId",
                        column: x => x.MicroId,
                        principalTable: "Micros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UbicacionesMicro_Usuarios_ConductorUsuarioId",
                        column: x => x.ConductorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesMicro_ConductorUsuarioId",
                table: "UbicacionesMicro",
                column: "ConductorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UbicacionesMicro");
        }
    }
}
