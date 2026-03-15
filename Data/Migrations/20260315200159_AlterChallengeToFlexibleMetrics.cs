using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dupi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterChallengeToFlexibleMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProteinTargetGrams",
                table: "Challenges",
                newName: "Metric");

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "Challenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TargetValue",
                table: "Challenges",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "TargetValue",
                table: "Challenges");

            migrationBuilder.RenameColumn(
                name: "Metric",
                table: "Challenges",
                newName: "ProteinTargetGrams");
        }
    }
}
