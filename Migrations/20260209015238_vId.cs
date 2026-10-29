using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WallsShop.Migrations
{
    /// <inheritdoc />
    public partial class vId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Variants_VariantId",
                table: "OrderDetails",
                column: "VariantId",
                principalTable: "Variants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Variants_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails");
        }
    }
}
