using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFenceSegmentAndGatePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FenceSegments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParcelId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FenceTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GeoJsonGeometry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LengthInFeet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LengthInMeters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsSnappedToBoundary = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerifiedOnsite = table.Column<bool>(type: "bit", nullable: false),
                    OnsiteVerifiedLengthInFeet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FenceSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FenceSegments_FenceTypes_FenceTypeId",
                        column: x => x.FenceTypeId,
                        principalTable: "FenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FenceSegments_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FenceSegments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FenceSegments_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GatePositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FenceSegmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GateTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GeoJsonLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PositionAlongSegment = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerifiedOnsite = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatePositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatePositions_FenceSegments_FenceSegmentId",
                        column: x => x.FenceSegmentId,
                        principalTable: "FenceSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GatePositions_GateTypes_GateTypeId",
                        column: x => x.GateTypeId,
                        principalTable: "GateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GatePositions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FenceSegments_FenceTypeId",
                table: "FenceSegments",
                column: "FenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FenceSegments_JobId",
                table: "FenceSegments",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_FenceSegments_OrganizationId",
                table: "FenceSegments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FenceSegments_ParcelId",
                table: "FenceSegments",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePositions_FenceSegmentId",
                table: "GatePositions",
                column: "FenceSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePositions_GateTypeId",
                table: "GatePositions",
                column: "GateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePositions_OrganizationId",
                table: "GatePositions",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GatePositions");

            migrationBuilder.DropTable(
                name: "FenceSegments");
        }
    }
}
