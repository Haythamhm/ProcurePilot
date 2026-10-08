using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionCaseStateMachine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExceptionCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionCases", x => x.Id);
                    table.UniqueConstraint("AK_ExceptionCases_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_ExceptionCases_Resolution", "(\"Status\" IN ('Resolved', 'Closed') AND \"ResolvedAtUtc\" IS NOT NULL) OR (\"Status\" NOT IN ('Resolved', 'Closed') AND \"ResolvedAtUtc\" IS NULL)");
                    table.CheckConstraint("CK_ExceptionCases_ResolvedAt", "\"ResolvedAtUtc\" IS NULL OR \"ResolvedAtUtc\" >= \"OpenedAtUtc\"");
                    table.ForeignKey(
                        name: "FK_ExceptionCases_Invoices_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "Invoices",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCases_MatchResults_TenantId_MatchResultId",
                        columns: x => new { x.TenantId, x.MatchResultId },
                        principalTable: "MatchResults",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCases_PurchaseOrders_TenantId_PurchaseOrderId",
                        columns: x => new { x.TenantId, x.PurchaseOrderId },
                        principalTable: "PurchaseOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionCases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantId_InvoiceId",
                table: "ExceptionCases",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantId_MatchResultId",
                table: "ExceptionCases",
                columns: new[] { "TenantId", "MatchResultId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantId_PurchaseOrderId",
                table: "ExceptionCases",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantId_Status_OpenedAtUtc",
                table: "ExceptionCases",
                columns: new[] { "TenantId", "Status", "OpenedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExceptionCases");
        }
    }
}
