using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesExecutiveCompensation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AtOrAboveTargetCommissionPercent",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BasicSalary",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BelowTargetCommissionPercent",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesTargetValue",
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
                name: "AtOrAboveTargetCommissionPercent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BasicSalary",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BelowTargetCommissionPercent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalesTargetValue",
                table: "Users");
        }
    }
}
