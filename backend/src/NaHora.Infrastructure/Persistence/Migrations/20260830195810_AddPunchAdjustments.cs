using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaHora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPunchAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "punch_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    punch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    original_occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    adjusted_occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    justification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attachment_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_punch_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_punch_adjustments_adjustment_justifications_justification_id",
                        column: x => x.justification_id,
                        principalTable: "adjustment_justifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_punch_adjustments_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_punch_adjustments_punches_punch_id",
                        column: x => x.punch_id,
                        principalTable: "punches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_punch_adjustments_justification_id",
                table: "punch_adjustments",
                column: "justification_id");

            migrationBuilder.CreateIndex(
                name: "i_x_punch_adjustments_punch_id",
                table: "punch_adjustments",
                column: "punch_id");

            migrationBuilder.CreateIndex(
                name: "ix_punch_adjustments_company_status",
                table: "punch_adjustments",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_punch_adjustments_employee_requested_at",
                table: "punch_adjustments",
                columns: new[] { "employee_id", "requested_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "punch_adjustments");
        }
    }
}
