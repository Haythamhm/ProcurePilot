using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceExtractionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceExtractionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPayload = table.Column<string>(type: "jsonb", maxLength: 100000, nullable: false),
                    ExtractedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedPayload = table.Column<string>(type: "jsonb", maxLength: 100000, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceExtractionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceExtractionSnapshots_AspNetUsers_TenantId_ReviewedByU~",
                        columns: x => new { x.TenantId, x.ReviewedByUserId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceExtractionSnapshots_Invoices_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "Invoices",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExtractionSnapshots_TenantId_InvoiceId",
                table: "InvoiceExtractionSnapshots",
                columns: new[] { "TenantId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExtractionSnapshots_TenantId_ReviewedByUserId",
                table: "InvoiceExtractionSnapshots",
                columns: new[] { "TenantId", "ReviewedByUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceExtractionSnapshots");
        }
    }
}
