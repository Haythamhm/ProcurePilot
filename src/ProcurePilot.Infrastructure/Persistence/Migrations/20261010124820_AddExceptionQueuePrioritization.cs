using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionQueuePrioritization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "ExceptionCases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "ExceptionCases",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantId_AssignedToUserId_Status",
                table: "ExceptionCases",
                columns: new[] { "TenantId", "AssignedToUserId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExceptionCases_Severity",
                table: "ExceptionCases",
                sql: "\"Severity\" IN ('Low', 'Medium', 'High', 'Critical')");

            migrationBuilder.AddForeignKey(
                name: "FK_ExceptionCases_AspNetUsers_TenantId_AssignedToUserId",
                table: "ExceptionCases",
                columns: new[] { "TenantId", "AssignedToUserId" },
                principalTable: "AspNetUsers",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExceptionCases_AspNetUsers_TenantId_AssignedToUserId",
                table: "ExceptionCases");

            migrationBuilder.DropIndex(
                name: "IX_ExceptionCases_TenantId_AssignedToUserId_Status",
                table: "ExceptionCases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExceptionCases_Severity",
                table: "ExceptionCases");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "ExceptionCases");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "ExceptionCases");
        }
    }
}
