using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioIndustryDemoLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndustryDemoLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdminId = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DemoLink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryDemoLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndustryDemoLinks_AdminId",
                table: "IndustryDemoLinks",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_IndustryDemoLinks_AdminId_Category",
                table: "IndustryDemoLinks",
                columns: new[] { "AdminId", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndustryDemoLinks");
        }
    }
}
