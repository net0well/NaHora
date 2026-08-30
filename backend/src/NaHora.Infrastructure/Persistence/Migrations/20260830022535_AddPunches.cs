using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaHora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPunches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nsr_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_nsr = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_nsr_counters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "punches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nsr = table.Column<long>(type: "bigint", nullable: false),
                    direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    origin = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    company_offset = table.Column<TimeSpan>(type: "interval", nullable: false),
                    trust = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    clock_skew_seconds = table.Column<int>(type: "integer", nullable: false),
                    device_clock_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    location_was_mocked = table.Column<bool>(type: "boolean", nullable: false),
                    accuracy_meters = table.Column<double>(type: "double precision", nullable: false),
                    location_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_punches", x => x.id);
                    table.ForeignKey(
                        name: "f_k_punches_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_nsr_counters_company",
                table: "nsr_counters",
                column: "company_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_punches_employee_id",
                table: "punches",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_punches_company_client_id",
                table: "punches",
                columns: new[] { "company_id", "client_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_punches_company_employee_local_date",
                table: "punches",
                columns: new[] { "company_id", "employee_id", "local_date" });

            migrationBuilder.CreateIndex(
                name: "ix_punches_company_nsr",
                table: "punches",
                columns: new[] { "company_id", "nsr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_punches_company_trust_pending",
                table: "punches",
                columns: new[] { "company_id", "trust" },
                filter: "trust <> 'Trusted'");

            // Immutability enforced by the database, not by convention.
            //
            // Portaria 671 puts the original record in the signed AFD, so a
            // punch must never be updated or deleted — not by a bug, not by a
            // migration, not by someone with a psql prompt. Corrections are a
            // separate record that points at this one.
            //
            // Written as a trigger rather than trusted to the application
            // because the application is not the only thing that reaches this
            // table.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION punches_reject_mutation()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION
                        'punches is append-only: % rejected on NSR %',
                        TG_OP,
                        COALESCE(OLD.nsr, -1)
                        USING ERRCODE = 'restrict_violation';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER punches_no_update
                    BEFORE UPDATE ON punches
                    FOR EACH ROW EXECUTE FUNCTION punches_reject_mutation();

                CREATE TRIGGER punches_no_delete
                    BEFORE DELETE ON punches
                    FOR EACH ROW EXECUTE FUNCTION punches_reject_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nsr_counters");

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS punches_no_delete ON punches;
                DROP TRIGGER IF EXISTS punches_no_update ON punches;
                DROP FUNCTION IF EXISTS punches_reject_mutation();
                """);

            migrationBuilder.DropTable(
                name: "punches");
        }
    }
}
