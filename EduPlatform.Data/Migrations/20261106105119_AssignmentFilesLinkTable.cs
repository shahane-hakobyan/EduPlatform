using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentFilesLinkTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentFiles_Assignments_AssignmentId",
                table: "AssignmentFiles");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentFiles_AssignmentId",
                table: "AssignmentFiles");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "AssignmentFiles");

            migrationBuilder.CreateTable(
                name: "AssignmentFileLinks",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentFileLinks", x => new { x.FileId, x.AssignmentId });
                    table.ForeignKey(
                        name: "FK_AssignmentFileLinks_AssignmentFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "AssignmentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentFileLinks_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentFileLinks_AssignmentId",
                table: "AssignmentFileLinks",
                column: "AssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentFileLinks");

            migrationBuilder.AddColumn<int>(
                name: "AssignmentId",
                table: "AssignmentFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentFiles_AssignmentId",
                table: "AssignmentFiles",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentFiles_Assignments_AssignmentId",
                table: "AssignmentFiles",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
