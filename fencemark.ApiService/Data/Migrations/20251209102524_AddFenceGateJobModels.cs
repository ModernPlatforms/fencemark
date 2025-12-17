using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFenceGateJobModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    Dimensions = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Components_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FenceTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    HeightInFeet = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    Style = table.Column<string>(type: "TEXT", nullable: true),
                    PricePerLinearFoot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FenceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FenceTypes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GateTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    WidthInFeet = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    HeightInFeet = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    Style = table.Column<string>(type: "TEXT", nullable: true),
                    BasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateTypes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerPhone = table.Column<string>(type: "TEXT", nullable: true),
                    InstallationAddress = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalLinearFeet = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MaterialsCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstimatedStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FenceComponents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FenceTypeId = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<string>(type: "TEXT", nullable: false),
                    QuantityPerLinearFoot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FenceComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FenceComponents_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FenceComponents_FenceTypes_FenceTypeId",
                        column: x => x.FenceTypeId,
                        principalTable: "FenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GateComponents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    GateTypeId = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<string>(type: "TEXT", nullable: false),
                    QuantityPerGate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateComponents_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GateComponents_GateTypes_GateTypeId",
                        column: x => x.GateTypeId,
                        principalTable: "GateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobLineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    FenceTypeId = table.Column<string>(type: "TEXT", nullable: true),
                    GateTypeId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobLineItems_FenceTypes_FenceTypeId",
                        column: x => x.FenceTypeId,
                        principalTable: "FenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobLineItems_GateTypes_GateTypeId",
                        column: x => x.GateTypeId,
                        principalTable: "GateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobLineItems_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_Category",
                table: "Components",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Components_OrganizationId",
                table: "Components",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FenceComponents_ComponentId",
                table: "FenceComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_FenceComponents_FenceTypeId_ComponentId",
                table: "FenceComponents",
                columns: new[] { "FenceTypeId", "ComponentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FenceTypes_OrganizationId",
                table: "FenceTypes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_GateComponents_ComponentId",
                table: "GateComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_GateComponents_GateTypeId_ComponentId",
                table: "GateComponents",
                columns: new[] { "GateTypeId", "ComponentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GateTypes_OrganizationId",
                table: "GateTypes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLineItems_FenceTypeId",
                table: "JobLineItems",
                column: "FenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLineItems_GateTypeId",
                table: "JobLineItems",
                column: "GateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLineItems_JobId",
                table: "JobLineItems",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_OrganizationId",
                table: "Jobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status",
                table: "Jobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FenceComponents");

            migrationBuilder.DropTable(
                name: "GateComponents");

            migrationBuilder.DropTable(
                name: "JobLineItems");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "FenceTypes");

            migrationBuilder.DropTable(
                name: "GateTypes");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
