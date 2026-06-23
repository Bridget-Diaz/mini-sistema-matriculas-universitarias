using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaMatriculas.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCiclos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CicloId",
                table: "Matriculas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Ciclo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVO"),
                    CreditosMaximos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ciclo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_CicloId",
                table: "Matriculas",
                column: "CicloId");

            migrationBuilder.CreateIndex(
                name: "IX_Ciclo_Nombre",
                table: "Ciclo",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_Ciclo_CicloId",
                table: "Matriculas",
                column: "CicloId",
                principalTable: "Ciclo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_Ciclo_CicloId",
                table: "Matriculas");

            migrationBuilder.DropTable(
                name: "Ciclo");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_CicloId",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "CicloId",
                table: "Matriculas");
        }
    }
}
