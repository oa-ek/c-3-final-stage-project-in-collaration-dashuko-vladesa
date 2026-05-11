using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsGroup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Branches",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Branches",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Branches");
        }
    }
}
