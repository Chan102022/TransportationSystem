using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportationBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationIdToPassenger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "Book",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "Book");
        }
    }
}
