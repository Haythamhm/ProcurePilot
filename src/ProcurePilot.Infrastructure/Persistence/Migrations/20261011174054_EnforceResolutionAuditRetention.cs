using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceResolutionAuditRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_ExceptionCases_TenantId_ExceptionCaseId",
                table: "AuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Resolutions_ExceptionCases_TenantId_ExceptionCaseId",
                table: "Resolutions");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_ExceptionCases_TenantId_ExceptionCaseId",
                table: "AuditEvents",
                columns: new[] { "TenantId", "ExceptionCaseId" },
                principalTable: "ExceptionCases",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resolutions_ExceptionCases_TenantId_ExceptionCaseId",
                table: "Resolutions",
                columns: new[] { "TenantId", "ExceptionCaseId" },
                principalTable: "ExceptionCases",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_ExceptionCases_TenantId_ExceptionCaseId",
                table: "AuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Resolutions_ExceptionCases_TenantId_ExceptionCaseId",
                table: "Resolutions");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_ExceptionCases_TenantId_ExceptionCaseId",
                table: "AuditEvents",
                columns: new[] { "TenantId", "ExceptionCaseId" },
                principalTable: "ExceptionCases",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resolutions_ExceptionCases_TenantId_ExceptionCaseId",
                table: "Resolutions",
                columns: new[] { "TenantId", "ExceptionCaseId" },
                principalTable: "ExceptionCases",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
