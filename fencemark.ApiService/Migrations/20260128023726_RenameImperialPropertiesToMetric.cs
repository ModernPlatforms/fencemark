using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class RenameImperialPropertiesToMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LengthInFeet",
                table: "FenceSegments");

            migrationBuilder.RenameColumn(
                name: "TotalLinearFeet",
                table: "Jobs",
                newName: "TotalLinearMetres");

            migrationBuilder.RenameColumn(
                name: "WidthInFeet",
                table: "GateTypes",
                newName: "WidthInMm");

            migrationBuilder.RenameColumn(
                name: "HeightInFeet",
                table: "GateTypes",
                newName: "HeightInMm");

            migrationBuilder.RenameColumn(
                name: "PricePerLinearFoot",
                table: "FenceTypes",
                newName: "PricePerLinearMetre");

            migrationBuilder.RenameColumn(
                name: "HeightInFeet",
                table: "FenceTypes",
                newName: "HeightInMm");

            migrationBuilder.RenameColumn(
                name: "OnsiteVerifiedLengthInFeet",
                table: "FenceSegments",
                newName: "OnsiteVerifiedLengthInMetres");

            migrationBuilder.RenameColumn(
                name: "LengthInMeters",
                table: "FenceSegments",
                newName: "LengthInMetres");

            migrationBuilder.RenameColumn(
                name: "QuantityPerLinearFoot",
                table: "FenceComponents",
                newName: "QuantityPerLinearMetre");

            migrationBuilder.RenameColumn(
                name: "MinimumLinearFeet",
                table: "DiscountRules",
                newName: "MinimumLinearMetres");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalLinearMetres",
                table: "Jobs",
                newName: "TotalLinearFeet");

            migrationBuilder.RenameColumn(
                name: "WidthInMm",
                table: "GateTypes",
                newName: "WidthInFeet");

            migrationBuilder.RenameColumn(
                name: "HeightInMm",
                table: "GateTypes",
                newName: "HeightInFeet");

            migrationBuilder.RenameColumn(
                name: "PricePerLinearMetre",
                table: "FenceTypes",
                newName: "PricePerLinearFoot");

            migrationBuilder.RenameColumn(
                name: "HeightInMm",
                table: "FenceTypes",
                newName: "HeightInFeet");

            migrationBuilder.RenameColumn(
                name: "OnsiteVerifiedLengthInMetres",
                table: "FenceSegments",
                newName: "OnsiteVerifiedLengthInFeet");

            migrationBuilder.RenameColumn(
                name: "LengthInMetres",
                table: "FenceSegments",
                newName: "LengthInMeters");

            migrationBuilder.RenameColumn(
                name: "QuantityPerLinearMetre",
                table: "FenceComponents",
                newName: "QuantityPerLinearFoot");

            migrationBuilder.RenameColumn(
                name: "MinimumLinearMetres",
                table: "DiscountRules",
                newName: "MinimumLinearFeet");

            migrationBuilder.AddColumn<decimal>(
                name: "LengthInFeet",
                table: "FenceSegments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
