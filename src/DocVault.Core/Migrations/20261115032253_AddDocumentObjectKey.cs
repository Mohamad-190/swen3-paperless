using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocVault.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentObjectKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "Documents");
        }
    }
}
