using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class FixHoursPerLinearMetreSpelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HoursPerLinearMeter",
                table: "PricingConfigs",
                newName: "HoursPerLinearMetre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HoursPerLinearMetre",
                table: "PricingConfigs",
                newName: "HoursPerLinearMeter");
        }
    }
}
