using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceFreelancerSalaryWithTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasicSalary",
                table: "Users");

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumTargetValue",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AchievedTargetValue",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AchievedTargetValue",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MinimumTargetValue",
                table: "Users");

            migrationBuilder.AddColumn<decimal>(
                name: "BasicSalary",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
