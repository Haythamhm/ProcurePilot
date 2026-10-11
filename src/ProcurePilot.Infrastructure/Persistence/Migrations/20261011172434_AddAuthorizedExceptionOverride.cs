using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizedExceptionOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExceptionCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.UniqueConstraint("AK_AuditEvents_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_AuditEvents_Action", "\"Action\" = 'ExceptionOverride'");
                    table.CheckConstraint("CK_AuditEvents_State", "\"PreviousStatus\" IN ('Open', 'WaitingAction', 'WaitingExternal') AND \"NewStatus\" = 'Resolved'");
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_TenantId_ActorUserId",
                        columns: x => new { x.TenantId, x.ActorUserId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_ExceptionCases_TenantId_ExceptionCaseId",
                        columns: x => new { x.TenantId, x.ExceptionCaseId },
                        principalTable: "ExceptionCases",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExceptionCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resolutions", x => x.Id);
                    table.UniqueConstraint("AK_Resolutions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Resolutions_State", "\"PreviousStatus\" IN ('Open', 'WaitingAction', 'WaitingExternal') AND \"NewStatus\" = 'Resolved'");
                    table.CheckConstraint("CK_Resolutions_Type", "\"Type\" = 'Override'");
                    table.ForeignKey(
                        name: "FK_Resolutions_AspNetUsers_TenantId_ActorUserId",
                        columns: x => new { x.TenantId, x.ActorUserId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resolutions_ExceptionCases_TenantId_ExceptionCaseId",
                        columns: x => new { x.TenantId, x.ExceptionCaseId },
                        principalTable: "ExceptionCases",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resolutions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_ActorUserId",
                table: "AuditEvents",
                columns: new[] { "TenantId", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_ExceptionCaseId_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "TenantId", "ExceptionCaseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_TenantId_ActorUserId",
                table: "Resolutions",
                columns: new[] { "TenantId", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_TenantId_ExceptionCaseId",
                table: "Resolutions",
                columns: new[] { "TenantId", "ExceptionCaseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "Resolutions");
        }
    }
}
