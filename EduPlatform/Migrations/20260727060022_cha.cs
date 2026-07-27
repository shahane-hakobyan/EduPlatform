using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class cha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleLessons_Grades_GradeId",
                table: "ScheduleLessons");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleLessons_Subjects_SubjectId",
                table: "ScheduleLessons");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleLessons_GradeId_Day_PeriodNumber",
                table: "ScheduleLessons");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleLessons_SubjectId",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "ScheduleLessons");

            migrationBuilder.CreateTable(
                name: "LessonGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleLessonId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonGrades_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonGrades_ScheduleLessons_ScheduleLessonId",
                        column: x => x.ScheduleLessonId,
                        principalTable: "ScheduleLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleLessonId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonSubjects_ScheduleLessons_ScheduleLessonId",
                        column: x => x.ScheduleLessonId,
                        principalTable: "ScheduleLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonGrades_GradeId",
                table: "LessonGrades",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonGrades_ScheduleLessonId",
                table: "LessonGrades",
                column: "ScheduleLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSubjects_ScheduleLessonId",
                table: "LessonSubjects",
                column: "ScheduleLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSubjects_SubjectId",
                table: "LessonSubjects",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonGrades");

            migrationBuilder.DropTable(
                name: "LessonSubjects");

            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                table: "ScheduleLessons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "ScheduleLessons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleLessons_GradeId_Day_PeriodNumber",
                table: "ScheduleLessons",
                columns: new[] { "GradeId", "Day", "PeriodNumber" },
                unique: true,
                filter: "[GradeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleLessons_SubjectId",
                table: "ScheduleLessons",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleLessons_Grades_GradeId",
                table: "ScheduleLessons",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleLessons_Subjects_SubjectId",
                table: "ScheduleLessons",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
