using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flight_Management_Company.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedUtcToMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Maintenances");

            migrationBuilder.RenameColumn(
                name: "MaintenanceDate",
                table: "Maintenances",
                newName: "ScheduledUtc");

            migrationBuilder.RenameColumn(
                name: "MaintenanceId",
                table: "Maintenances",
                newName: "AircraftMaintenanceId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Maintenances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedUtc",
                table: "Maintenances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GroundsAircraft",
                table: "Maintenances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WorkType",
                table: "Maintenances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedUtc",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "GroundsAircraft",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "WorkType",
                table: "Maintenances");

            migrationBuilder.RenameColumn(
                name: "ScheduledUtc",
                table: "Maintenances",
                newName: "MaintenanceDate");

            migrationBuilder.RenameColumn(
                name: "AircraftMaintenanceId",
                table: "Maintenances",
                newName: "MaintenanceId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Maintenances",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Maintenances",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
