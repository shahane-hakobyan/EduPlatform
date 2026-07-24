using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class LessonIsFree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "ScheduleLessons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "ScheduleLessons");
        }
    }
}
