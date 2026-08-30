using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NaHora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustmentJustificationCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adjustment_justifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    requires_notes = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_adjustment_justifications", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "adjustment_justifications",
                columns: new[] { "id", "code", "description", "display_order", "requires_notes" },
                values: new object[,]
                {
                    { new Guid("0195f3a1-0001-7000-8000-000000000001"), "FORGOT_TO_PUNCH", "Esquecimento de marcação", 1, false },
                    { new Guid("0195f3a1-0002-7000-8000-000000000002"), "DEVICE_FAILURE", "Falha no aparelho ou bateria", 2, false },
                    { new Guid("0195f3a1-0003-7000-8000-000000000003"), "NO_CONNECTIVITY", "Sem conexão no momento da marcação", 3, false },
                    { new Guid("0195f3a1-0004-7000-8000-000000000004"), "MEDICAL_LEAVE", "Atestado médico", 4, false },
                    { new Guid("0195f3a1-0005-7000-8000-000000000005"), "WORK_TRAVEL", "Viagem a serviço", 5, false },
                    { new Guid("0195f3a1-0006-7000-8000-000000000006"), "EXTERNAL_WORK", "Trabalho externo sem cobertura", 6, false },
                    { new Guid("0195f3a1-0007-7000-8000-000000000007"), "COMPENSATORY_TIME", "Compensação de horas", 7, false },
                    { new Guid("0195f3a1-0008-7000-8000-000000000008"), "SYSTEM_ERROR", "Erro do sistema", 8, false },
                    { new Guid("0195f3a1-0009-7000-8000-000000000009"), "MANAGER_CORRECTION", "Correção administrativa", 9, false },
                    { new Guid("0195f3a1-0010-7000-8000-000000000010"), "OTHER", "Outro", 10, true }
                });

            migrationBuilder.CreateIndex(
                name: "i_x_adjustment_justifications_code",
                table: "adjustment_justifications",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adjustment_justifications");
        }
    }
}
