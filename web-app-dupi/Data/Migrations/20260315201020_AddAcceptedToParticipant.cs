using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dupi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptedToParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Accepted",
                table: "ChallengeParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // All existing participants are already active members
            migrationBuilder.Sql("UPDATE \"ChallengeParticipants\" SET \"Accepted\" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accepted",
                table: "ChallengeParticipants");
        }
    }
}
