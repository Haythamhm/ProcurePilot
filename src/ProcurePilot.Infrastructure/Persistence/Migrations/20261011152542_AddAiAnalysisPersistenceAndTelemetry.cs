using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAnalysisPersistenceAndTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExceptionCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RootCauseCategory = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RecommendedAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RiskAssessment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: true),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: true),
                    TotalTokens = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RawPayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalyses", x => x.Id);
                    table.UniqueConstraint("AK_AiAnalyses_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_AiAnalyses_ConfidenceScore", "\"ConfidenceScore\" IS NULL OR (\"ConfidenceScore\" >= 0 AND \"ConfidenceScore\" <= 1)");
                    table.CheckConstraint("CK_AiAnalyses_DurationMs", "\"DurationMs\" >= 0");
                    table.CheckConstraint("CK_AiAnalyses_Status", "\"Status\" IN ('Success', 'Failed', 'Unavailable')");
                    table.ForeignKey(
                        name: "FK_AiAnalyses_ExceptionCases_TenantId_ExceptionCaseId",
                        columns: x => new { x.TenantId, x.ExceptionCaseId },
                        principalTable: "ExceptionCases",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiAnalyses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_TenantId_ExceptionCaseId_CreatedAtUtc",
                table: "AiAnalyses",
                columns: new[] { "TenantId", "ExceptionCaseId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAnalyses");
        }
    }
}
