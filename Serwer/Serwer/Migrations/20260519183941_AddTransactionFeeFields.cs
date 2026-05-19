using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serwer.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionFeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Transactions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FeeCurrency",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Fee", table: "Transactions");
            migrationBuilder.DropColumn(name: "FeeCurrency", table: "Transactions");
        }
    }
}
