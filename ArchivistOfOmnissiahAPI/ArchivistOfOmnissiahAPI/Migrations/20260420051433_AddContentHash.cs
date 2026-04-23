using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchivistOfOmnissiahAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "lore_entries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "lore_entries");
        }
    }
}
