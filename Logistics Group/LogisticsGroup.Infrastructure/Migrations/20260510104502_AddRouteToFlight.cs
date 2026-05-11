using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsGroup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteToFlight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Flights",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_RouteId",
                table: "Flights",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_Routes_RouteId",
                table: "Flights",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flights_Routes_RouteId",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_RouteId",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Flights");
        }
    }
}
