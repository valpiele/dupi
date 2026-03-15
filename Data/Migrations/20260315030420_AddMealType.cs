using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dupi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMealType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealType",
                table: "NutritionPlans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealType",
                table: "NutritionPlans");
        }
    }
}
