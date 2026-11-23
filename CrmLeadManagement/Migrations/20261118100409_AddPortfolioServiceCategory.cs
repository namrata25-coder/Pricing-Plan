using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioServiceCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "PortfolioServices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PortfolioServices");
        }
    }
}
