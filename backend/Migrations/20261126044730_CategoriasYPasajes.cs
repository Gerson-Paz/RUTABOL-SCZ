using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RutaBol.Api.Migrations
{
    /// <inheritdoc />
    public partial class CategoriasYPasajes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pasajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    MicroId = table.Column<int>(type: "integer", nullable: false),
                    TarifaId = table.Column<int>(type: "integer", nullable: false),
                    CategoriaAplicada = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CodigoComprobante = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pasajes", x => x.Id);
                    table.CheckConstraint("CK_Pasajes_Monto", "\"Monto\" > 0");
                    table.ForeignKey(
                        name: "FK_Pasajes_Micros_MicroId",
                        column: x => x.MicroId,
                        principalTable: "Micros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pasajes_Tarifas_TarifaId",
                        column: x => x.TarifaId,
                        principalTable: "Tarifas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pasajes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCategoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorreoInstitucional = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NombreArchivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoArchivo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdministradorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCategoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesCategoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pasajes_CodigoComprobante",
                table: "Pasajes",
                column: "CodigoComprobante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pasajes_MicroId",
                table: "Pasajes",
                column: "MicroId");

            migrationBuilder.CreateIndex(
                name: "IX_Pasajes_TarifaId",
                table: "Pasajes",
                column: "TarifaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pasajes_UsuarioId",
                table: "Pasajes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCategoria_UsuarioId_Estado",
                table: "SolicitudesCategoria",
                columns: new[] { "UsuarioId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pasajes");

            migrationBuilder.DropTable(
                name: "SolicitudesCategoria");
        }
    }
}
