using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentTaskText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskText",
                table: "Assignments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskText",
                table: "Assignments");
        }
    }
}
