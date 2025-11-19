using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportationBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeSlotSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Slot1Start = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot1End = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot2Start = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot2End = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot3Start = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot3End = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot4Start = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Slot4End = table.Column<TimeSpan>(type: "time(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeSlots");
        }
    }
}
