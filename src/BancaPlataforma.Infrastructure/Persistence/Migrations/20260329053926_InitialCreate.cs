using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaPlataforma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    razao_social = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    agencia = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    imagem_documento_base64 = table.Column<string>(type: "text", nullable: false),
                    saldo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    conta_destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transacoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_transacoes_contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contas_cnpj",
                table: "contas",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_conta_id",
                table: "transacoes",
                column: "conta_id");

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_idempotency_key",
                table: "transacoes",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transacoes");

            migrationBuilder.DropTable(
                name: "contas");
        }
    }
}
