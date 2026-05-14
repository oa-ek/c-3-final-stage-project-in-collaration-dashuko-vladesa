using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsGroup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueMessageToFlight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssueMessage",
                table: "Flights",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssueMessage",
                table: "Flights");
        }
    }
}
