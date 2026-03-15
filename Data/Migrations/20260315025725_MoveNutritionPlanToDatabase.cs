using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dupi.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveNutritionPlanToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InputType = table.Column<string>(type: "text", nullable: false),
                    HasFile = table.Column<bool>(type: "boolean", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    SharedWithUsers = table.Column<string>(type: "jsonb", nullable: false),
                    FoodDescription = table.Column<string>(type: "text", nullable: false),
                    CaloriesMin = table.Column<int>(type: "integer", nullable: false),
                    CaloriesMax = table.Column<int>(type: "integer", nullable: false),
                    Proteins = table.Column<double>(type: "double precision", nullable: false),
                    Carbohydrates = table.Column<double>(type: "double precision", nullable: false),
                    Fats = table.Column<double>(type: "double precision", nullable: false),
                    WhatsGood = table.Column<string>(type: "jsonb", nullable: false),
                    WhatToImprove = table.Column<string>(type: "jsonb", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    ScoreSummary = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionPlans_UserId",
                table: "NutritionPlans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionPlans");
        }
    }
}
