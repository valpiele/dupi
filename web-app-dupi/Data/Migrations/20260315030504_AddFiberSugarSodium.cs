using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dupi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiberSugarSodium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Fiber",
                table: "NutritionPlans",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Sodium",
                table: "NutritionPlans",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Sugar",
                table: "NutritionPlans",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fiber",
                table: "NutritionPlans");

            migrationBuilder.DropColumn(
                name: "Sodium",
                table: "NutritionPlans");

            migrationBuilder.DropColumn(
                name: "Sugar",
                table: "NutritionPlans");
        }
    }
}
