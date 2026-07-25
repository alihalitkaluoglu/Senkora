using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senkora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WooStore_PriceCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceCostCenterCode",
                table: "WooStores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceProjectCode",
                table: "WooStores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceTradingGroupCode",
                table: "WooStores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCostCenterCode",
                table: "WooStores");

            migrationBuilder.DropColumn(
                name: "PriceProjectCode",
                table: "WooStores");

            migrationBuilder.DropColumn(
                name: "PriceTradingGroupCode",
                table: "WooStores");
        }
    }
}
