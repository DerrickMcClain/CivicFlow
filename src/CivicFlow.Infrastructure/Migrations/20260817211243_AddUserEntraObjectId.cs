using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEntraObjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntraObjectId",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EntraObjectId",
                table: "Users",
                column: "EntraObjectId",
                unique: true,
                filter: "[EntraObjectId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EntraObjectId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EntraObjectId",
                table: "Users");
        }
    }
}
