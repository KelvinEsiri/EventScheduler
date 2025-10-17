using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLateStatusAndUserJoinSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "EventInvitations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitations_UserId",
                table: "EventInvitations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventInvitations_Users_UserId",
                table: "EventInvitations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventInvitations_Users_UserId",
                table: "EventInvitations");

            migrationBuilder.DropIndex(
                name: "IX_EventInvitations_UserId",
                table: "EventInvitations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EventInvitations");
        }
    }
}
