using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senkora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WooStore_WpMediaCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WpAppPasswordEncrypted",
                table: "WooStores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WpUsername",
                table: "WooStores",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WpAppPasswordEncrypted",
                table: "WooStores");

            migrationBuilder.DropColumn(
                name: "WpUsername",
                table: "WooStores");
        }
    }
}
