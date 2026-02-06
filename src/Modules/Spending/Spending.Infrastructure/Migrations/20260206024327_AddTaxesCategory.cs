using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spending.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxesCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "Name", "Description", "UserId", "IsSystemCategory" },
                values: new object[] { Guid.NewGuid(), "Taxes", "Federal, state, local, and property taxes", Guid.Empty, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM categories WHERE \"Name\" = 'Taxes' AND \"IsSystemCategory\" = true;");
        }
    }
}
