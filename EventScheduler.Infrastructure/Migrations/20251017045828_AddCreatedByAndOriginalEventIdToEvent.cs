using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByAndOriginalEventIdToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalEventId",
                table: "Events",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OriginalEventId",
                table: "Events");
        }
    }
}
