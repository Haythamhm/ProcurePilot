using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionCaseEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExceptionCaseEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExceptionCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchingRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionCaseEvidences", x => x.Id);
                    table.UniqueConstraint("AK_ExceptionCaseEvidences_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_ExceptionCaseEvidences_Type", "\"Type\" IN ('InvoicePrice', 'PurchaseOrderPrice', 'PriceVariance', 'RuleTolerance')");
                    table.CheckConstraint("CK_ExceptionCaseEvidences_TypedReference", "(\"Type\" = 'InvoicePrice' AND \"InvoiceLineId\" IS NOT NULL AND \"PurchaseOrderLineId\" IS NULL AND \"MatchResultId\" IS NULL AND \"MatchingRuleId\" IS NULL) OR (\"Type\" = 'PurchaseOrderPrice' AND \"PurchaseOrderLineId\" IS NOT NULL AND \"InvoiceLineId\" IS NULL AND \"MatchResultId\" IS NULL AND \"MatchingRuleId\" IS NULL) OR (\"Type\" = 'PriceVariance' AND \"MatchResultId\" IS NOT NULL AND \"InvoiceLineId\" IS NULL AND \"PurchaseOrderLineId\" IS NULL AND \"MatchingRuleId\" IS NULL) OR (\"Type\" = 'RuleTolerance' AND \"MatchingRuleId\" IS NOT NULL AND \"InvoiceLineId\" IS NULL AND \"PurchaseOrderLineId\" IS NULL AND \"MatchResultId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_ExceptionCaseEvidences_ExceptionCases_TenantId_ExceptionCas~",
                        columns: x => new { x.TenantId, x.ExceptionCaseId },
                        principalTable: "ExceptionCases",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExceptionCaseEvidences_InvoiceLines_TenantId_InvoiceLineId",
                        columns: x => new { x.TenantId, x.InvoiceLineId },
                        principalTable: "InvoiceLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCaseEvidences_MatchResults_TenantId_MatchResultId",
                        columns: x => new { x.TenantId, x.MatchResultId },
                        principalTable: "MatchResults",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCaseEvidences_MatchingRules_TenantId_MatchingRuleId",
                        columns: x => new { x.TenantId, x.MatchingRuleId },
                        principalTable: "MatchingRules",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCaseEvidences_PurchaseOrderLines_TenantId_Purchase~",
                        columns: x => new { x.TenantId, x.PurchaseOrderLineId },
                        principalTable: "PurchaseOrderLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCaseEvidences_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCaseEvidences_TenantId_ExceptionCaseId_Type",
                table: "ExceptionCaseEvidences",
                columns: new[] { "TenantId", "ExceptionCaseId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCaseEvidences_TenantId_InvoiceLineId",
                table: "ExceptionCaseEvidences",
                columns: new[] { "TenantId", "InvoiceLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCaseEvidences_TenantId_MatchingRuleId",
                table: "ExceptionCaseEvidences",
                columns: new[] { "TenantId", "MatchingRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCaseEvidences_TenantId_MatchResultId",
                table: "ExceptionCaseEvidences",
                columns: new[] { "TenantId", "MatchResultId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCaseEvidences_TenantId_PurchaseOrderLineId",
                table: "ExceptionCaseEvidences",
                columns: new[] { "TenantId", "PurchaseOrderLineId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExceptionCaseEvidences");
        }
    }
}
