using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaHora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAndEmployeeProfileFields : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// The defaults on the required columns are placeholders, not data. An
        /// empty string is what EF generates, and the value converters reject
        /// it on read — a company row would become unloadable. These parse.
        ///
        /// In practice nothing reads them: no deployment predates this
        /// migration, so no row exists to be backfilled.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "complement",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_type",
                table: "employees",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Clt");

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "district",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "employees",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "number",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "employees",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "employees",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "street",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "companies",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Não informado");

            migrationBuilder.AddColumn<string>(
                name: "complement",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "district",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "founded_on",
                table: "companies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "municipal_registration",
                table: "companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "number",
                table: "companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "S/N");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "companies",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "companies",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "00000000");

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "companies",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "SP");

            migrationBuilder.AddColumn<string>(
                name: "state_registration",
                table: "companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "street",
                table: "companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Não informado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "city",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "complement",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "contract_type",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "department",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "district",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "email",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "number",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "state",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "street",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "city",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "complement",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "district",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "founded_on",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "municipal_registration",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "number",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "state",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "state_registration",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "street",
                table: "companies");
        }
    }
}
