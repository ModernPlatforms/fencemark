using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParcelsDrawingsTaxDiscountModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Quotes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountRuleId",
                table: "Quotes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxRegionId",
                table: "Quotes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTaxRegionId",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitSystem",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiscountRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MinimumOrderValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinimumLinearFeet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountRules_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parcels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParcelNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AreaUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Coordinates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parcels_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Parcels_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxRegions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRegions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRegions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Drawings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ParcelId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DrawingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drawings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drawings_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Drawings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Drawings_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_DiscountRuleId",
                table: "Quotes",
                column: "DiscountRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_TaxRegionId",
                table: "Quotes",
                column: "TaxRegionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_IsActive",
                table: "DiscountRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_OrganizationId",
                table: "DiscountRules",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_PromoCode",
                table: "DiscountRules",
                column: "PromoCode");

            migrationBuilder.CreateIndex(
                name: "IX_Drawings_JobId",
                table: "Drawings",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Drawings_OrganizationId",
                table: "Drawings",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Drawings_ParcelId",
                table: "Drawings",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_JobId",
                table: "Parcels",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_OrganizationId",
                table: "Parcels",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRegions_OrganizationId",
                table: "TaxRegions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRegions_OrganizationId_IsDefault",
                table: "TaxRegions",
                columns: new[] { "OrganizationId", "IsDefault" });

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_DiscountRules_DiscountRuleId",
                table: "Quotes",
                column: "DiscountRuleId",
                principalTable: "DiscountRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_TaxRegions_TaxRegionId",
                table: "Quotes",
                column: "TaxRegionId",
                principalTable: "TaxRegions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_DiscountRules_DiscountRuleId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_TaxRegions_TaxRegionId",
                table: "Quotes");

            migrationBuilder.DropTable(
                name: "DiscountRules");

            migrationBuilder.DropTable(
                name: "Drawings");

            migrationBuilder.DropTable(
                name: "TaxRegions");

            migrationBuilder.DropTable(
                name: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_DiscountRuleId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_TaxRegionId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DiscountRuleId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TaxRegionId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DefaultTaxRegionId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "UnitSystem",
                table: "Organizations");
        }
    }
}
