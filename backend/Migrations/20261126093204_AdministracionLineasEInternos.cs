using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RutaBol.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdministracionLineasEInternos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Micros",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRetiro",
                table: "Micros",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LineaId",
                table: "Micros",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lineas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Micros_LineaId",
                table: "Micros",
                column: "LineaId");

            migrationBuilder.CreateIndex(
                name: "IX_Lineas_Codigo",
                table: "Lineas",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Micros_Lineas_LineaId",
                table: "Micros",
                column: "LineaId",
                principalTable: "Lineas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Micros_Lineas_LineaId",
                table: "Micros");

            migrationBuilder.DropTable(
                name: "Lineas");

            migrationBuilder.DropIndex(
                name: "IX_Micros_LineaId",
                table: "Micros");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Micros");

            migrationBuilder.DropColumn(
                name: "FechaRetiro",
                table: "Micros");

            migrationBuilder.DropColumn(
                name: "LineaId",
                table: "Micros");
        }
    }
}
