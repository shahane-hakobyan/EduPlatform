using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class GradeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherGrades",
                table: "TeacherGrades");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleLessons_TeacherUserId_Day_PeriodNumber",
                table: "ScheduleLessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PupilGrades",
                table: "PupilGrades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonSubjects",
                table: "LessonSubjects");

            migrationBuilder.DropIndex(
                name: "IX_LessonSubjects_ScheduleLessonId",
                table: "LessonSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonGrades",
                table: "LessonGrades");

            migrationBuilder.DropIndex(
                name: "IX_LessonGrades_ScheduleLessonId",
                table: "LessonGrades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_InstitutionId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TeacherGrades");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PupilGrades");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "LessonSubjects");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "LessonGrades");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Grades");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherUserId",
                table: "TeacherSubjects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DshhTeacherId",
                table: "TeacherSubjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TeacherUserId",
                table: "TeacherGrades",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "TeacherGrades",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DshhTeacherId",
                table: "TeacherGrades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RoomNumber",
                table: "ScheduleLessons",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ScheduleLessons",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "ScheduleLessons",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DshhTeacherId",
                table: "ScheduleLessons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PupilUserId",
                table: "PupilGrades",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "PupilGrades",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DshhPupilId",
                table: "PupilGrades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Institutions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "KtakInstitutionId",
                table: "Institutions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DshhInstitutionId",
                table: "Institutions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeName",
                table: "Grades",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects",
                columns: new[] { "TeacherUserId", "SubjectId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherGrades",
                table: "TeacherGrades",
                columns: new[] { "TeacherUserId", "GradeId", "AcademicYear" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PupilGrades",
                table: "PupilGrades",
                columns: new[] { "PupilUserId", "GradeId", "AcademicYear" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonSubjects",
                table: "LessonSubjects",
                columns: new[] { "ScheduleLessonId", "SubjectId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonGrades",
                table: "LessonGrades",
                columns: new[] { "ScheduleLessonId", "GradeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleLessons_TeacherUserId_AcademicYear_Day_PeriodNumber",
                table: "ScheduleLessons",
                columns: new[] { "TeacherUserId", "AcademicYear", "Day", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_KtakInstitutionId",
                table: "Institutions",
                column: "KtakInstitutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_InstitutionId_GradeName",
                table: "Grades",
                columns: new[] { "InstitutionId", "GradeName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherGrades",
                table: "TeacherGrades");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleLessons_TeacherUserId_AcademicYear_Day_PeriodNumber",
                table: "ScheduleLessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PupilGrades",
                table: "PupilGrades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonSubjects",
                table: "LessonSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonGrades",
                table: "LessonGrades");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_KtakInstitutionId",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Grades_InstitutionId_GradeName",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "DshhTeacherId",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "TeacherGrades");

            migrationBuilder.DropColumn(
                name: "DshhTeacherId",
                table: "TeacherGrades");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "DshhTeacherId",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "PupilGrades");

            migrationBuilder.DropColumn(
                name: "DshhPupilId",
                table: "PupilGrades");

            migrationBuilder.DropColumn(
                name: "DshhInstitutionId",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "GradeName",
                table: "Grades");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherUserId",
                table: "TeacherSubjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TeacherSubjects",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherUserId",
                table: "TeacherGrades",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TeacherGrades",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "RoomNumber",
                table: "ScheduleLessons",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ScheduleLessons",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PupilUserId",
                table: "PupilGrades",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PupilGrades",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "LessonSubjects",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "LessonGrades",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "KtakInstitutionId",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Grades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherGrades",
                table: "TeacherGrades",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PupilGrades",
                table: "PupilGrades",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonSubjects",
                table: "LessonSubjects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonGrades",
                table: "LessonGrades",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleLessons_TeacherUserId_Day_PeriodNumber",
                table: "ScheduleLessons",
                columns: new[] { "TeacherUserId", "Day", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSubjects_ScheduleLessonId",
                table: "LessonSubjects",
                column: "ScheduleLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonGrades_ScheduleLessonId",
                table: "LessonGrades",
                column: "ScheduleLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_InstitutionId",
                table: "Grades",
                column: "InstitutionId");
        }
    }
}
