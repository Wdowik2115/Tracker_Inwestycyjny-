using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serwer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithWalletSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalletShares",
                columns: table => new
                {
                    SharedWalletsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletShares", x => new { x.SharedWalletsId, x.SharedWithId });
                    table.ForeignKey(
                        name: "FK_WalletShares_Users_SharedWithId",
                        column: x => x.SharedWithId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WalletShares_Wallets_SharedWalletsId",
                        column: x => x.SharedWalletsId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletShares_SharedWithId",
                table: "WalletShares",
                column: "SharedWithId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletShares");
        }
    }
}
