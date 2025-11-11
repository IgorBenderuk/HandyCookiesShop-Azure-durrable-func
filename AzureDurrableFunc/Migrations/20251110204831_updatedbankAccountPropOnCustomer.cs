using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace durrableShop.Migrations
{
    /// <inheritdoc />
    public partial class updatedbankAccountPropOnCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BanckAccount",
                table: "Customers",
                newName: "BankAccount");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 20, 48, 30, 445, DateTimeKind.Utc).AddTicks(9820));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 20, 48, 30, 445, DateTimeKind.Utc).AddTicks(9825));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 20, 48, 30, 445, DateTimeKind.Utc).AddTicks(9828));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BankAccount",
                table: "Customers",
                newName: "BanckAccount");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 17, 41, 2, 381, DateTimeKind.Utc).AddTicks(5419));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 17, 41, 2, 381, DateTimeKind.Utc).AddTicks(5423));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 17, 41, 2, 381, DateTimeKind.Utc).AddTicks(5426));
        }
    }
}
