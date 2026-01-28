using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class FixHeightTierMetresSpelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinHeightInMeters",
                table: "HeightTiers",
                newName: "MinHeightInMetres");

            migrationBuilder.RenameColumn(
                name: "MaxHeightInMeters",
                table: "HeightTiers",
                newName: "MaxHeightInMetres");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinHeightInMetres",
                table: "HeightTiers",
                newName: "MinHeightInMeters");

            migrationBuilder.RenameColumn(
                name: "MaxHeightInMetres",
                table: "HeightTiers",
                newName: "MaxHeightInMeters");
        }
    }
}
