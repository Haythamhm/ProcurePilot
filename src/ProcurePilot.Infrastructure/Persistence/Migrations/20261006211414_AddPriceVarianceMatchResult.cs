using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceVarianceMatchResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_PurchaseOrderLines_TenantId_Id",
                table: "PurchaseOrderLines",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_InvoiceLines_TenantId_Id",
                table: "InvoiceLines",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchingRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchingRuleVersion = table.Column<int>(type: "integer", nullable: false),
                    InvoiceUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseOrderUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowedTolerancePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    VariancePercent = table.Column<decimal>(type: "numeric(29,4)", precision: 29, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExceptionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.UniqueConstraint("AK_MatchResults_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_MatchResults_AllowedTolerancePercent", "\"AllowedTolerancePercent\" >= 0 AND \"AllowedTolerancePercent\" <= 100");
                    table.CheckConstraint("CK_MatchResults_MatchingRuleVersion", "\"MatchingRuleVersion\" > 0");
                    table.CheckConstraint("CK_MatchResults_Outcome", "(\"Status\" = 'Passed' AND \"ExceptionCode\" IS NULL) OR (\"Status\" = 'Failed' AND \"ExceptionCode\" = 'PriceVariance')");
                    table.CheckConstraint("CK_MatchResults_Prices", "\"InvoiceUnitPrice\" >= 0 AND \"PurchaseOrderUnitPrice\" > 0");
                    table.ForeignKey(
                        name: "FK_MatchResults_InvoiceLines_TenantId_InvoiceLineId",
                        columns: x => new { x.TenantId, x.InvoiceLineId },
                        principalTable: "InvoiceLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchResults_MatchingRules_TenantId_MatchingRuleId",
                        columns: x => new { x.TenantId, x.MatchingRuleId },
                        principalTable: "MatchingRules",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchResults_PurchaseOrderLines_TenantId_PurchaseOrderLineId",
                        columns: x => new { x.TenantId, x.PurchaseOrderLineId },
                        principalTable: "PurchaseOrderLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchResults_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_TenantId_InvoiceLineId",
                table: "MatchResults",
                columns: new[] { "TenantId", "InvoiceLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_TenantId_MatchingRuleId",
                table: "MatchResults",
                columns: new[] { "TenantId", "MatchingRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_TenantId_PurchaseOrderLineId",
                table: "MatchResults",
                columns: new[] { "TenantId", "PurchaseOrderLineId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PurchaseOrderLines_TenantId_Id",
                table: "PurchaseOrderLines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_InvoiceLines_TenantId_Id",
                table: "InvoiceLines");
        }
    }
}
