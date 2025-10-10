using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomMailboxUpn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MailboxUpn",
                table: "Rooms",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "1",
                column: "MailboxUpn",
                value: "pixelpalace@contoso.com");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "2",
                column: "MailboxUpn",
                value: "8bitboardroom@contoso.com");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "3",
                column: "MailboxUpn",
                value: "retroretreat@contoso.com");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "4",
                column: "MailboxUpn",
                value: "arcadearena@contoso.com");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "5",
                column: "MailboxUpn",
                value: "spritesummit@contoso.com");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "6",
                column: "MailboxUpn",
                value: "consolechamber@contoso.com");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_MailboxUpn",
                table: "Rooms",
                column: "MailboxUpn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_MailboxUpn",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MailboxUpn",
                table: "Rooms");
        }
    }
}
