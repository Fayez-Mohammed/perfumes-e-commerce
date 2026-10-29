using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WallsShop.Migrations
{
    /// <inheritdoc />
    public partial class ShortDescriptionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortDescriptionEn",
                table: "ProductTranslations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescriptionEn",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortDescriptionEn",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "ShortDescriptionEn",
                table: "Products");
        }
    }
}
