using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IpiPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscrepancyResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Discrepancies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Discrepancies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Discrepancies");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Discrepancies");
        }
    }
}
