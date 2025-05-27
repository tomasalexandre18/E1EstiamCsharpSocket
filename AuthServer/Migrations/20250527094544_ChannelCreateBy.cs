using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class ChannelCreateBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateAt",
                table: "Channels",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateById",
                table: "Channels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_CreateById",
                table: "Channels",
                column: "CreateById");

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_Users_CreateById",
                table: "Channels",
                column: "CreateById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_Users_CreateById",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_Channels_CreateById",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "CreateAt",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "CreateById",
                table: "Channels");
        }
    }
}
