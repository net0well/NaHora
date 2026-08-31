using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaHora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_work_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_company_work_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_work_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employee_work_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "company_work_schedule_days",
                columns: table => new
                {
                    day_of_week = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    company_work_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    lunch_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    lunch_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_company_work_schedule_days", x => new { x.company_work_schedule_id, x.day_of_week });
                    table.ForeignKey(
                        name: "f_k_company_work_schedule_days_company_work_schedules_company_w~",
                        column: x => x.company_work_schedule_id,
                        principalTable: "company_work_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_work_schedule_days",
                columns: table => new
                {
                    day_of_week = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    employee_work_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    lunch_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    lunch_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employee_work_schedule_days", x => new { x.employee_work_schedule_id, x.day_of_week });
                    table.ForeignKey(
                        name: "f_k_employee_work_schedule_days_employee_work_schedules_employe~",
                        column: x => x.employee_work_schedule_id,
                        principalTable: "employee_work_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_work_schedules_company",
                table: "company_work_schedules",
                column: "company_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_work_schedules_employee_effective_from",
                table: "employee_work_schedules",
                columns: new[] { "employee_id", "effective_from" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_work_schedule_days");

            migrationBuilder.DropTable(
                name: "employee_work_schedule_days");

            migrationBuilder.DropTable(
                name: "company_work_schedules");

            migrationBuilder.DropTable(
                name: "employee_work_schedules");
        }
    }
}
