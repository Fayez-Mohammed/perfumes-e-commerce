using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WallsShop.Migrations
{
    /// <inheritdoc />
    public partial class lastupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Products",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Products");

          
         
           
        }
    }
}
