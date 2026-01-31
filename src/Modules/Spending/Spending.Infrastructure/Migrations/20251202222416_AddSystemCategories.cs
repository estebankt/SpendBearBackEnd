using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spending.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop existing unique constraint
            migrationBuilder.DropIndex(
                name: "IX_categories_UserId_Name",
                table: "categories");

            // 2. Add IsSystemCategory column (default false for existing data)
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemCategory",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 3. Create partial unique index for user categories
            // Users can't have duplicate category names
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_categories_UserId_Name_UserCategories""
                  ON categories (""UserId"", ""Name"")
                  WHERE ""IsSystemCategory"" = false;");

            // 4. Create partial unique index for system categories
            // Prevent duplicate system category names globally
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_categories_Name_SystemCategories""
                  ON categories (""Name"")
                  WHERE ""IsSystemCategory"" = true;");

            // 5. Insert 28 system categories
            var systemCategories = new[]
            {
                // Essential Expenses
                new { Id = Guid.NewGuid(), Name = "Groceries", Description = "Food and household essentials", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Rent/Mortgage", Description = "Monthly housing payment", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Utilities", Description = "Electric, water, gas, trash", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Internet/Phone", Description = "Internet, mobile phone, landline", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Insurance", Description = "Health, auto, home, life insurance", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Healthcare", Description = "Medical, dental, pharmacy, copays", UserId = Guid.Empty, IsSystemCategory = true },

                // Transportation
                new { Id = Guid.NewGuid(), Name = "Gas/Fuel", Description = "Vehicle fuel and charging", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Public Transit", Description = "Bus, train, subway, metro", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Parking", Description = "Parking fees and permits", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Vehicle Maintenance", Description = "Car repairs, oil changes, tires", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Rideshare/Taxi", Description = "Uber, Lyft, taxi services", UserId = Guid.Empty, IsSystemCategory = true },

                // Food & Dining
                new { Id = Guid.NewGuid(), Name = "Dining Out", Description = "Restaurants and cafes", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Fast Food", Description = "Quick service and fast food", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Coffee/Tea", Description = "Coffee shops and beverages", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Alcohol/Bars", Description = "Bars, clubs, alcoholic beverages", UserId = Guid.Empty, IsSystemCategory = true },

                // Shopping
                new { Id = Guid.NewGuid(), Name = "Clothing", Description = "Clothes, shoes, accessories", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Electronics", Description = "Gadgets, computers, accessories", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Home Goods", Description = "Furniture, decor, household items", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Personal Care", Description = "Haircuts, cosmetics, toiletries", UserId = Guid.Empty, IsSystemCategory = true },

                // Entertainment & Lifestyle
                new { Id = Guid.NewGuid(), Name = "Subscriptions", Description = "Streaming, software, memberships", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Entertainment", Description = "Movies, concerts, events", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Hobbies", Description = "Sports, crafts, recreational activities", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Fitness", Description = "Gym, classes, sports equipment", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Travel", Description = "Flights, hotels, vacation expenses", UserId = Guid.Empty, IsSystemCategory = true },

                // Financial & Other
                new { Id = Guid.NewGuid(), Name = "Education", Description = "Tuition, courses, books, supplies", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Gifts/Donations", Description = "Gifts, charity, contributions", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Pet Care", Description = "Pet food, vet, grooming, supplies", UserId = Guid.Empty, IsSystemCategory = true },
                new { Id = Guid.NewGuid(), Name = "Miscellaneous", Description = "Other expenses not categorized", UserId = Guid.Empty, IsSystemCategory = true }
            };

            foreach (var category in systemCategories)
            {
                migrationBuilder.InsertData(
                    table: "categories",
                    columns: new[] { "Id", "Name", "Description", "UserId", "IsSystemCategory" },
                    values: new object[] { category.Id, category.Name, category.Description, category.UserId, category.IsSystemCategory });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove system categories
            migrationBuilder.Sql("DELETE FROM categories WHERE \"IsSystemCategory\" = true;");

            // Drop the partial indexes
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_categories_UserId_Name_UserCategories\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_categories_Name_SystemCategories\";");

            // Remove IsSystemCategory column
            migrationBuilder.DropColumn(
                name: "IsSystemCategory",
                table: "categories");

            // Recreate original unique constraint
            migrationBuilder.CreateIndex(
                name: "IX_categories_UserId_Name",
                table: "categories",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }
    }
}
