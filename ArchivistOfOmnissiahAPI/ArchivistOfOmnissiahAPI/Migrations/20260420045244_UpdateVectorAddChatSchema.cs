using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchivistOfOmnissiahAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVectorAddChatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                newName: "chat_messages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_chat_messages",
                table: "chat_messages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SessionId",
                table: "chat_messages",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_chat_messages",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "IX_chat_messages_SessionId",
                table: "chat_messages");

            migrationBuilder.RenameTable(
                name: "chat_messages",
                newName: "ChatMessages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages",
                column: "Id");
        }
    }
}
