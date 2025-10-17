using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserNameToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserName",
                table: "Events",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserName",
                table: "Events");
        }
    }
}
