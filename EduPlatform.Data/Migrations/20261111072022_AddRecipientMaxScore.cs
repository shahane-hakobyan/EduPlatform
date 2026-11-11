using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientMaxScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "AssignmentRecipients",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "AssignmentRecipients");
        }
    }
}
