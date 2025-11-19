using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportationBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddQrAndPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DatePaid",
                table: "Book",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Book",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "QRCodeImage",
                table: "Book",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatePaid",
                table: "Book");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Book");

            migrationBuilder.DropColumn(
                name: "QRCodeImage",
                table: "Book");
        }
    }
}
