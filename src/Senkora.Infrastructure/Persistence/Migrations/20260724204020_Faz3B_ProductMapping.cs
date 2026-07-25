using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senkora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Faz3B_ProductMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductMappings_TenantId_LogoItemRef",
                table: "ProductMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductMappings_TenantId_WooProductId",
                table: "ProductMappings");

            migrationBuilder.RenameColumn(
                name: "LastSyncAt",
                table: "ProductMappings",
                newName: "LastSyncedAt");

            migrationBuilder.AlterColumn<string>(
                name: "WooSku",
                table: "ProductMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "WooProductName",
                table: "ProductMappings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<long>(
                name: "WooProductId",
                table: "ProductMappings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "LogoItemRef",
                table: "ProductMappings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "LogoItemCode",
                table: "ProductMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "EnrichmentJson",
                table: "ProductMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoAuxDesc",
                table: "ProductMappings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LogoCardType",
                table: "ProductMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LogoDescription",
                table: "ProductMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoGroupCode",
                table: "ProductMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogoLastFetched",
                table: "ProductMappings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "LogoMarkRef",
                table: "ProductMappings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LogoSellPrice",
                table: "ProductMappings",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LogoSellPrice2",
                table: "ProductMappings",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LogoSpecode",
                table: "ProductMappings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LogoStock",
                table: "ProductMappings",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LogoUnitCode",
                table: "ProductMappings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LogoVatRate",
                table: "ProductMappings",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LogoWeight",
                table: "ProductMappings",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WooProductUrl",
                table: "ProductMappings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappings_IsDeleted",
                table: "ProductMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappings_TenantId_LogoItemRef_WooStoreId",
                table: "ProductMappings",
                columns: new[] { "TenantId", "LogoItemRef", "WooStoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappings_TenantId_WooProductId",
                table: "ProductMappings",
                columns: new[] { "TenantId", "WooProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductMappings_IsDeleted",
                table: "ProductMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductMappings_TenantId_LogoItemRef_WooStoreId",
                table: "ProductMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductMappings_TenantId_WooProductId",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "EnrichmentJson",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoAuxDesc",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoCardType",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoDescription",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoGroupCode",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoLastFetched",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoMarkRef",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoSellPrice",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoSellPrice2",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoSpecode",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoStock",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoUnitCode",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoVatRate",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "LogoWeight",
                table: "ProductMappings");

            migrationBuilder.DropColumn(
                name: "WooProductUrl",
                table: "ProductMappings");

            migrationBuilder.RenameColumn(
                name: "LastSyncedAt",
                table: "ProductMappings",
                newName: "LastSyncAt");

            migrationBuilder.AlterColumn<string>(
                name: "WooSku",
                table: "ProductMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WooProductName",
                table: "ProductMappings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "WooProductId",
                table: "ProductMappings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LogoItemRef",
                table: "ProductMappings",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "LogoItemCode",
                table: "ProductMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappings_TenantId_LogoItemRef",
                table: "ProductMappings",
                columns: new[] { "TenantId", "LogoItemRef" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappings_TenantId_WooProductId",
                table: "ProductMappings",
                columns: new[] { "TenantId", "WooProductId" },
                unique: true);
        }
    }
}
