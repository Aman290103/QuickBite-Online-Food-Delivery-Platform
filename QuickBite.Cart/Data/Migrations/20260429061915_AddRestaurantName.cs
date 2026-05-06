using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Cart.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RestaurantName",
                table: "Carts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestaurantName",
                table: "Carts");
        }
    }
}
