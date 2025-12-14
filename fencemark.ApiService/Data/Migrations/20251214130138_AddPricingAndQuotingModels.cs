using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndQuotingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PricingConfigs
            migrationBuilder.CreateTable(
                name: "PricingConfigs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    LaborRatePerHour = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    HoursPerLinearMeter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ContingencyPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    ProfitMarginPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingConfigs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingConfigs_OrganizationId",
                table: "PricingConfigs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingConfigs_OrganizationId_IsDefault",
                table: "PricingConfigs",
                columns: new[] { "OrganizationId", "IsDefault" });

            // HeightTiers
            migrationBuilder.CreateTable(
                name: "HeightTiers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PricingConfigId = table.Column<string>(type: "TEXT", nullable: false),
                    MinHeightInMeters = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MaxHeightInMeters = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Multiplier = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeightTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeightTiers_PricingConfigs_PricingConfigId",
                        column: x => x.PricingConfigId,
                        principalTable: "PricingConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeightTiers_PricingConfigId",
                table: "HeightTiers",
                column: "PricingConfigId");

            // Quotes
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    PricingConfigId = table.Column<string>(type: "TEXT", nullable: true),
                    QuoteNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MaterialsCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ContingencyAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ProfitAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Terms = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotes_PricingConfigs_PricingConfigId",
                        column: x => x.PricingConfigId,
                        principalTable: "PricingConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_JobId",
                table: "Quotes",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_OrganizationId",
                table: "Quotes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteNumber",
                table: "Quotes",
                column: "QuoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Status",
                table: "Quotes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_PricingConfigId",
                table: "Quotes",
                column: "PricingConfigId");

            // QuoteVersions
            migrationBuilder.CreateTable(
                name: "QuoteVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    QuoteId = table.Column<string>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangeSummary = table.Column<string>(type: "TEXT", nullable: true),
                    MaterialsCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ContingencyAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ProfitAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteVersions_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteVersions_QuoteId_VersionNumber",
                table: "QuoteVersions",
                columns: new[] { "QuoteId", "VersionNumber" },
                unique: true);

            // BillOfMaterialsItems
            migrationBuilder.CreateTable(
                name: "BillOfMaterialsItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    QuoteId = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterialsItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialsItems_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialsItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialsItems_QuoteId",
                table: "BillOfMaterialsItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialsItems_Category",
                table: "BillOfMaterialsItems",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillOfMaterialsItems");

            migrationBuilder.DropTable(
                name: "HeightTiers");

            migrationBuilder.DropTable(
                name: "QuoteVersions");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "PricingConfigs");
        }
    }
}
