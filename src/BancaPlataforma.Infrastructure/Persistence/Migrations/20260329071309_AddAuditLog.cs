using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaPlataforma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValoresAntigos = table.Column<string>(type: "jsonb", nullable: true),
                    ValoresNovos = table.Column<string>(type: "jsonb", nullable: true),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntidadeId",
                table: "audit_logs",
                column: "EntidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_OcorridoEm",
                table: "audit_logs",
                column: "OcorridoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}
