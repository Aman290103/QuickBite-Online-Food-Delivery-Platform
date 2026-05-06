using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Cart.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxAmountToCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Carts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Carts");
        }
    }
}
