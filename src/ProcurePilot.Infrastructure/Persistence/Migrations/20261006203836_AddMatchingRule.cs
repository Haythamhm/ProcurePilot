using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AllowedTolerancePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingRules", x => x.Id);
                    table.UniqueConstraint("AK_MatchingRules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_MatchingRules_AllowedTolerancePercent", "\"AllowedTolerancePercent\" >= 0 AND \"AllowedTolerancePercent\" <= 100");
                    table.CheckConstraint("CK_MatchingRules_EffectivePeriod", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
                    table.CheckConstraint("CK_MatchingRules_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_MatchingRules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchingRules_TenantId_Type",
                table: "MatchingRules",
                columns: new[] { "TenantId", "Type" },
                unique: true,
                filter: "\"EffectiveToUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingRules_TenantId_Type_Version",
                table: "MatchingRules",
                columns: new[] { "TenantId", "Type", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchingRules");
        }
    }
}
