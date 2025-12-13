using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndQuotingModelsMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HoursPerLinearFoot",
                table: "PricingConfigs",
                newName: "HoursPerLinearMeter");

            migrationBuilder.RenameColumn(
                name: "MinHeightInFeet",
                table: "HeightTiers",
                newName: "MinHeightInMeters");

            migrationBuilder.RenameColumn(
                name: "MaxHeightInFeet",
                table: "HeightTiers",
                newName: "MaxHeightInMeters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HoursPerLinearMeter",
                table: "PricingConfigs",
                newName: "HoursPerLinearFoot");

            migrationBuilder.RenameColumn(
                name: "MinHeightInMeters",
                table: "HeightTiers",
                newName: "MinHeightInFeet");

            migrationBuilder.RenameColumn(
                name: "MaxHeightInMeters",
                table: "HeightTiers",
                newName: "MaxHeightInFeet");
        }
    }
}
