using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsGroup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlightId",
                table: "Parcels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flights_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_FlightId",
                table: "Parcels",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DriverId",
                table: "Flights",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_VehicleId",
                table: "Flights",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Flights_FlightId",
                table: "Parcels",
                column: "FlightId",
                principalTable: "Flights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Flights_FlightId",
                table: "Parcels");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_FlightId",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "FlightId",
                table: "Parcels");
        }
    }
}
