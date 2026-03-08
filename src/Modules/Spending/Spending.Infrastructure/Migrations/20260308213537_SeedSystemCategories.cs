using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spending.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Partial unique index for user categories (unique per user)
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_categories_UserId_Name_UserCategories""
                  ON spending.categories (""UserId"", ""Name"")
                  WHERE ""IsSystemCategory"" = false;");

            // Partial unique index for system categories (globally unique name)
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_categories_Name_SystemCategories""
                  ON spending.categories (""Name"")
                  WHERE ""IsSystemCategory"" = true;");

            var systemCategories = new[]
            {
                // Essential Expenses
                new { Id = Guid.NewGuid(), Name = "Groceries",        Description = "Food and household essentials",                UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Rent/Mortgage",    Description = "Monthly housing payment",                     UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Utilities",        Description = "Electric, water, gas, trash",                 UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Internet/Phone",   Description = "Internet, mobile phone, landline",            UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Insurance",        Description = "Health, auto, home, life insurance",          UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Healthcare",       Description = "Medical, dental, pharmacy, copays",           UserId = Guid.Empty },
                // Transportation
                new { Id = Guid.NewGuid(), Name = "Gas/Fuel",         Description = "Vehicle fuel and charging",                   UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Public Transit",   Description = "Bus, train, subway, metro",                   UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Parking",          Description = "Parking fees and permits",                    UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Vehicle Maintenance", Description = "Car repairs, oil changes, tires",          UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Rideshare/Taxi",   Description = "Uber, Lyft, taxi services",                  UserId = Guid.Empty },
                // Food & Dining
                new { Id = Guid.NewGuid(), Name = "Dining Out",       Description = "Restaurants and cafes",                      UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Fast Food",        Description = "Quick service and fast food",                 UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Coffee/Tea",       Description = "Coffee shops and beverages",                  UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Alcohol/Bars",     Description = "Bars, clubs, alcoholic beverages",            UserId = Guid.Empty },
                // Shopping
                new { Id = Guid.NewGuid(), Name = "Clothing",         Description = "Clothes, shoes, accessories",                 UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Electronics",      Description = "Gadgets, computers, accessories",             UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Home Goods",       Description = "Furniture, decor, household items",           UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Personal Care",    Description = "Haircuts, cosmetics, toiletries",             UserId = Guid.Empty },
                // Entertainment & Lifestyle
                new { Id = Guid.NewGuid(), Name = "Subscriptions",    Description = "Streaming, software, memberships",            UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Entertainment",    Description = "Movies, concerts, events",                    UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Hobbies",          Description = "Sports, crafts, recreational activities",     UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Fitness",          Description = "Gym, classes, sports equipment",              UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Travel",           Description = "Flights, hotels, vacation expenses",          UserId = Guid.Empty },
                // Financial & Other
                new { Id = Guid.NewGuid(), Name = "Education",        Description = "Tuition, courses, books, supplies",           UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Gifts/Donations",  Description = "Gifts, charity, contributions",               UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Pet Care",         Description = "Pet food, vet, grooming, supplies",           UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Miscellaneous",    Description = "Other expenses not categorized",              UserId = Guid.Empty },
                new { Id = Guid.NewGuid(), Name = "Taxes",            Description = "Federal, state, local, and property taxes",   UserId = Guid.Empty },
            };

            foreach (var category in systemCategories)
            {
                migrationBuilder.InsertData(
                    table: "categories",
                    schema: "spending",
                    columns: new[] { "Id", "Name", "Description", "UserId", "IsSystemCategory" },
                    values: new object[] { category.Id, category.Name, category.Description, category.UserId, true });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM spending.categories WHERE \"IsSystemCategory\" = true;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS spending.\"IX_categories_UserId_Name_UserCategories\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS spending.\"IX_categories_Name_SystemCategories\";");
        }
    }
}
