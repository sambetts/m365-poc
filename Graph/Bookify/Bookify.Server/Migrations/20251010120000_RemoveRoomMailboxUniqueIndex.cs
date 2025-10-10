using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookify.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoomMailboxUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop unique index on MailboxUpn
            migrationBuilder.DropIndex(
                name: "IX_Rooms_MailboxUpn",
                table: "Rooms");

            // Update seed data to use shared placeholder UPN (now allowed because uniqueness removed)
            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "1",
                column: "MailboxUpn",
                value: "shared@placeholder");
            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "2",
                column: "MailboxUpn",
                value: "shared@placeholder");
            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "3",
                column: "MailboxUpn",
                value: "shared@placeholder");
            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "4",
                column: "MailboxUpn",
                value: "shared@placeholder");
            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "5",
                column: "MailboxUpn",
                value: "shared@placeholder");
            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: "6",
                column: "MailboxUpn",
                value: "shared@placeholder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert seed data back to distinct values (required for unique index recreation)
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

            // Recreate unique index
            migrationBuilder.CreateIndex(
                name: "IX_Rooms_MailboxUpn",
                table: "Rooms",
                column: "MailboxUpn",
                unique: true);
        }
    }
}
