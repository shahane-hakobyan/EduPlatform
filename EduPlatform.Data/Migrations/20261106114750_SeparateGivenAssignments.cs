using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class SeparateGivenAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentRecipients_Assignments_AssignmentId",
                table: "AssignmentRecipients");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Assignments_TemplateId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TeacherUserId_IsTemplate_IsArchived",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TemplateId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "IsTemplate",
                table: "Assignments",
                newName: "IsInBank");

            migrationBuilder.RenameColumn(
                name: "AssignmentId",
                table: "AssignmentRecipients",
                newName: "GivenAssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_AssignmentRecipients_AssignmentId_PupilUserId",
                table: "AssignmentRecipients",
                newName: "IX_AssignmentRecipients_GivenAssignmentId_PupilUserId");

            migrationBuilder.CreateTable(
                name: "GivenAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    TeacherUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GivenAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GivenAssignments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TeacherUserId_IsInBank",
                table: "Assignments",
                columns: new[] { "TeacherUserId", "IsInBank" });

            migrationBuilder.CreateIndex(
                name: "IX_GivenAssignments_AssignmentId",
                table: "GivenAssignments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GivenAssignments_TeacherUserId_IsArchived",
                table: "GivenAssignments",
                columns: new[] { "TeacherUserId", "IsArchived" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentRecipients_GivenAssignments_GivenAssignmentId",
                table: "AssignmentRecipients",
                column: "GivenAssignmentId",
                principalTable: "GivenAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentRecipients_GivenAssignments_GivenAssignmentId",
                table: "AssignmentRecipients");

            migrationBuilder.DropTable(
                name: "GivenAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TeacherUserId_IsInBank",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "IsInBank",
                table: "Assignments",
                newName: "IsTemplate");

            migrationBuilder.RenameColumn(
                name: "GivenAssignmentId",
                table: "AssignmentRecipients",
                newName: "AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_AssignmentRecipients_GivenAssignmentId_PupilUserId",
                table: "AssignmentRecipients",
                newName: "IX_AssignmentRecipients_AssignmentId_PupilUserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TeacherUserId_IsTemplate_IsArchived",
                table: "Assignments",
                columns: new[] { "TeacherUserId", "IsTemplate", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TemplateId",
                table: "Assignments",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentRecipients_Assignments_AssignmentId",
                table: "AssignmentRecipients",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Assignments_TemplateId",
                table: "Assignments",
                column: "TemplateId",
                principalTable: "Assignments",
                principalColumn: "Id");
        }
    }
}
