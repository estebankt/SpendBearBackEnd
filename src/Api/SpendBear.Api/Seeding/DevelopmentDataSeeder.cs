using Npgsql;

namespace SpendBear.Api.Seeding;

/// <summary>
/// Seeds the development database with realistic test data using raw SQL.
/// Uses direct SQL inserts to avoid triggering domain events.
/// Idempotent — skips seeding if the test user already exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string TestAuth0Id = "auth0|6982540c4b34d6933a2410e2";
    private const string TestEmail = "testuser@spendbear.com";

    public static async Task SeedAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Idempotency check — verify the exact test user ID exists (not just Auth0 ID)
        await using (var checkCmd = new NpgsqlCommand(
            """SELECT COUNT(1) FROM identity."Users" WHERE "Id" = @id""", connection))
        {
            checkCmd.Parameters.AddWithValue("id", TestUserId);
            var count = (long)(await checkCmd.ExecuteScalarAsync())!;
            if (count > 0)
            {
                logger.LogInformation("Development seed data already exists — skipping");
                return;
            }
        }

        // Remove any stale user with the same Auth0 ID but different internal ID
        await using (var cleanupCmd = new NpgsqlCommand(
            """DELETE FROM identity."Users" WHERE "Auth0UserId" = @auth0Id AND "Id" != @id""", connection))
        {
            cleanupCmd.Parameters.AddWithValue("auth0Id", TestAuth0Id);
            cleanupCmd.Parameters.AddWithValue("id", TestUserId);
            await cleanupCmd.ExecuteNonQueryAsync();
        }

        logger.LogInformation("Seeding development database with test data...");

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Identity — User
            await InsertUserAsync(connection, transaction);

            // 2. Fetch system category IDs
            var categories = await FetchSystemCategoriesAsync(connection, transaction);

            // 3. Insert custom user categories
            var customCategories = await InsertCustomCategoriesAsync(connection, transaction);
            foreach (var kvp in customCategories)
                categories[kvp.Key] = kvp.Value;

            // 4. Build and insert transactions
            var transactions = BuildTransactions(categories);
            await InsertTransactionsAsync(connection, transaction, transactions);

            // 5. Insert budgets (with computed CurrentSpent)
            await InsertBudgetsAsync(connection, transaction, categories, transactions);

            // 6. Insert analytics snapshots
            await InsertAnalyticsSnapshotsAsync(connection, transaction, categories, transactions);

            // 7. Insert notifications
            await InsertNotificationsAsync(connection, transaction);

            await transaction.CommitAsync();
            logger.LogInformation("Test data seeded successfully — {Count} transactions created", transactions.Count);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task InsertUserAsync(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO identity."Users" ("Id", "Auth0UserId", "Email", "FirstName", "LastName", "CreatedAt", "LastLoginAt")
            VALUES (@id, @auth0Id, @email, @firstName, @lastName, @createdAt, @lastLogin)
            """, conn, tx);

        cmd.Parameters.AddWithValue("id", TestUserId);
        cmd.Parameters.AddWithValue("auth0Id", TestAuth0Id);
        cmd.Parameters.AddWithValue("email", TestEmail);
        cmd.Parameters.AddWithValue("firstName", "Test");
        cmd.Parameters.AddWithValue("lastName", "User");
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow.AddMonths(-6));
        cmd.Parameters.AddWithValue("lastLogin", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<Dictionary<string, Guid>> FetchSystemCategoriesAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        var result = new Dictionary<string, Guid>();

        await using var cmd = new NpgsqlCommand(
            """SELECT "Id", "Name" FROM categories WHERE "IsSystemCategory" = true""", conn, tx);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result[reader.GetString(1)] = reader.GetGuid(0);
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> InsertCustomCategoriesAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        var customCategories = new (string Name, string Description)[]
        {
            ("Side Hustle Income", "Income from freelance and side projects"),
            ("Freelance Work", "Contract and freelance development work"),
            ("Home Office", "Work from home expenses and equipment")
        };

        var result = new Dictionary<string, Guid>();

        foreach (var (name, description) in customCategories)
        {
            var id = Guid.NewGuid();
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO categories ("Id", "Name", "Description", "UserId", "IsSystemCategory")
                VALUES (@id, @name, @desc, @userId, false)
                """, conn, tx);

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("desc", description);
            cmd.Parameters.AddWithValue("userId", TestUserId);

            await cmd.ExecuteNonQueryAsync();
            result[name] = id;
        }

        return result;
    }

    private record TransactionData(
        Guid Id, long AmountCents, string Currency, DateTime Date,
        string Description, Guid CategoryId, string CategoryName, int Type);

    private static List<TransactionData> BuildTransactions(Dictionary<string, Guid> categories)
    {
        var transactions = new List<TransactionData>();
        var rng = new Random(42); // deterministic seed for reproducibility

        // 6 months: Aug 2025 – Jan 2026
        var months = new[]
        {
            new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        foreach (var monthStart in months)
        {
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var lastDay = new DateTime(monthStart.Year, monthStart.Month, daysInMonth, 0, 0, 0, DateTimeKind.Utc);

            // --- INCOME (Type=2) ---
            // Salary: 15th and last day
            transactions.Add(MakeTx(rng, 260000, "USD",
                monthStart.AddDays(14), "Paycheck - Direct Deposit", categories["Miscellaneous"], "Miscellaneous", 2));
            transactions.Add(MakeTx(rng, 260000, "USD",
                lastDay, "Paycheck - Direct Deposit", categories["Miscellaneous"], "Miscellaneous", 2));

            // Occasional side hustle income (some months)
            if (categories.ContainsKey("Side Hustle Income") && rng.Next(3) == 0)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 20000, 80000), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, "Upwork Payment", "Freelance Invoice", "Side Project Revenue"),
                    categories["Side Hustle Income"], "Side Hustle Income", 2));
            }

            // --- EXPENSES (Type=1) ---

            // Rent — 1st of month
            transactions.Add(MakeTx(rng, 185000, "USD",
                monthStart, "Monthly Rent Payment", categories["Rent/Mortgage"], "Rent/Mortgage", 1));

            // Utilities — ~5th
            transactions.Add(MakeTx(rng, RandCents(rng, 12000, 20000), "USD",
                monthStart.AddDays(4 + rng.Next(2)),
                "Electric & Water Bill", categories["Utilities"], "Utilities", 1));

            // Internet/Phone — ~10th
            transactions.Add(MakeTx(rng, 8500, "USD",
                monthStart.AddDays(9 + rng.Next(2)),
                "Xfinity Internet", categories["Internet/Phone"], "Internet/Phone", 1));

            // Insurance — 1st
            transactions.Add(MakeTx(rng, 17500, "USD",
                monthStart, "Auto & Renters Insurance", categories["Insurance"], "Insurance", 1));

            // Subscriptions (recurring)
            transactions.Add(MakeTx(rng, 1599, "USD",
                monthStart.AddDays(2), "Netflix Subscription", categories["Subscriptions"], "Subscriptions", 1));
            transactions.Add(MakeTx(rng, 1099, "USD",
                monthStart.AddDays(2), "Spotify Premium", categories["Subscriptions"], "Subscriptions", 1));
            transactions.Add(MakeTx(rng, 1499, "USD",
                monthStart.AddDays(4), "Gym Membership", categories["Subscriptions"], "Subscriptions", 1));

            // Groceries — 4-5 times per month
            var groceryCount = rng.Next(4, 6);
            var groceryStores = new[] { "Trader Joe's", "Costco", "Whole Foods", "Kroger", "Aldi" };
            for (int i = 0; i < groceryCount; i++)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 4000, 15000), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, groceryStores), categories["Groceries"], "Groceries", 1));
            }

            // Dining Out — 3-4 times
            var diningCount = rng.Next(3, 5);
            var restaurants = new[] { "Chipotle", "Olive Garden", "Thai Orchid", "Sushi Palace", "The Burger Joint", "Panda Express" };
            for (int i = 0; i < diningCount; i++)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 1500, 7500), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, restaurants), categories["Dining Out"], "Dining Out", 1));
            }

            // Coffee/Tea — 8-12 times
            var coffeeCount = rng.Next(8, 13);
            var coffeeShops = new[] { "Starbucks", "Local Coffee Shop", "Dunkin'", "Peet's Coffee" };
            for (int i = 0; i < coffeeCount; i++)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 400, 700), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, coffeeShops), categories["Coffee/Tea"], "Coffee/Tea", 1));
            }

            // Gas/Fuel — 2-3 times
            var gasCount = rng.Next(2, 4);
            var gasStations = new[] { "Shell", "Chevron", "BP", "Exxon" };
            for (int i = 0; i < gasCount; i++)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 3500, 6500), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, gasStations), categories["Gas/Fuel"], "Gas/Fuel", 1));
            }

            // Entertainment — 1-2 times
            var entertainmentCount = rng.Next(1, 3);
            var entertainmentVenues = new[] { "AMC Theatres", "Concert Tickets", "Bowling Alley", "Escape Room", "Mini Golf" };
            for (int i = 0; i < entertainmentCount; i++)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 1500, 5000), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, entertainmentVenues), categories["Entertainment"], "Entertainment", 1));
            }

            // Fast Food — 2-3 times
            var fastFoodCount = rng.Next(2, 4);
            var fastFoodPlaces = new[] { "McDonald's", "Wendy's", "Taco Bell", "Chick-fil-A", "Popeyes" };
            for (int i = 0; i < fastFoodCount; i++)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 800, 1500), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, fastFoodPlaces), categories["Fast Food"], "Fast Food", 1));
            }

            // Clothing — 0-1 times
            if (rng.Next(2) == 0)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 3000, 12000), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, "Target Clothing", "H&M", "Nordstrom Rack", "Uniqlo"),
                    categories["Clothing"], "Clothing", 1));
            }

            // Healthcare — 0-1 times
            if (rng.Next(2) == 0)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 2500, 20000), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, "CVS Pharmacy", "Doctor Copay", "Dentist Visit", "Walgreens"),
                    categories["Healthcare"], "Healthcare", 1));
            }

            // Personal Care — 0-1 times
            if (rng.Next(3) == 0)
            {
                transactions.Add(MakeTx(rng, RandCents(rng, 2000, 6000), "USD",
                    RandomDay(rng, monthStart, daysInMonth),
                    Pick(rng, "Great Clips Haircut", "Bath & Body Works", "Target Toiletries"),
                    categories["Personal Care"], "Personal Care", 1));
            }
        }

        return transactions;
    }

    private static async Task InsertTransactionsAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, List<TransactionData> transactions)
    {
        foreach (var t in transactions)
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO spending."Transactions" ("Id", "Amount", "Currency", "Date", "Description", "CategoryId", "UserId", "Type")
                VALUES (@id, @amount, @currency, @date, @desc, @catId, @userId, @type)
                """, conn, tx);

            cmd.Parameters.AddWithValue("id", t.Id);
            cmd.Parameters.AddWithValue("amount", t.AmountCents);
            cmd.Parameters.AddWithValue("currency", t.Currency);
            cmd.Parameters.AddWithValue("date", t.Date);
            cmd.Parameters.AddWithValue("desc", t.Description);
            cmd.Parameters.AddWithValue("catId", t.CategoryId);
            cmd.Parameters.AddWithValue("userId", TestUserId);
            cmd.Parameters.AddWithValue("type", t.Type);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertBudgetsAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx,
        Dictionary<string, Guid> categories, List<TransactionData> transactions)
    {
        // Current month budget period
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        // Get current month expenses
        var currentMonthExpenses = transactions
            .Where(t => t.Type == 1 && t.Date >= startDate && t.Date < startDate.AddMonths(1))
            .ToList();

        var budgets = new (string Name, string? CategoryName, decimal Amount, decimal Threshold)[]
        {
            ("Grocery Budget", "Groceries", 600m, 80m),
            ("Dining Budget", "Dining Out", 250m, 75m),
            ("Entertainment Budget", "Entertainment", 150m, 80m),
            ("Transport Budget", "Gas/Fuel", 200m, 85m),
            ("Coffee Budget", "Coffee/Tea", 60m, 90m),
            ("Overall Monthly", null, 4000m, 80m)
        };

        foreach (var (name, categoryName, amount, threshold) in budgets)
        {
            Guid? categoryId = categoryName != null && categories.ContainsKey(categoryName)
                ? categories[categoryName]
                : null;

            // Compute current spent from seeded transactions
            decimal currentSpent;
            if (categoryId.HasValue)
            {
                currentSpent = currentMonthExpenses
                    .Where(t => t.CategoryId == categoryId.Value)
                    .Sum(t => t.AmountCents) / 100m;
            }
            else
            {
                currentSpent = currentMonthExpenses.Sum(t => t.AmountCents) / 100m;
            }

            var isExceeded = currentSpent > amount;
            var warningTriggered = currentSpent >= amount * (threshold / 100m);

            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO budgets."Budgets"
                    ("Id", "Name", "Amount", "Currency", "Period", "StartDate", "EndDate",
                     "UserId", "CategoryId", "CurrentSpent", "WarningThreshold", "IsExceeded", "WarningTriggered")
                VALUES (@id, @name, @amount, @currency, @period, @start, @end,
                        @userId, @catId, @spent, @threshold, @exceeded, @warning)
                """, conn, tx);

            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("amount", amount);
            cmd.Parameters.AddWithValue("currency", "USD");
            cmd.Parameters.AddWithValue("period", 0); // Monthly
            cmd.Parameters.AddWithValue("start", startDate);
            cmd.Parameters.AddWithValue("end", endDate);
            cmd.Parameters.AddWithValue("userId", TestUserId);
            cmd.Parameters.AddWithValue("catId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("spent", currentSpent);
            cmd.Parameters.AddWithValue("threshold", threshold);
            cmd.Parameters.AddWithValue("exceeded", isExceeded);
            cmd.Parameters.AddWithValue("warning", warningTriggered);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertAnalyticsSnapshotsAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx,
        Dictionary<string, Guid> categories, List<TransactionData> transactions)
    {
        var months = transactions
            .Select(t => new DateTime(t.Date.Year, t.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        foreach (var monthStart in months)
        {
            var monthEnd = monthStart.AddMonths(1);
            var monthTxns = transactions.Where(t => t.Date >= monthStart && t.Date < monthEnd).ToList();

            var totalIncome = monthTxns.Where(t => t.Type == 2).Sum(t => t.AmountCents) / 100m;
            var totalExpense = monthTxns.Where(t => t.Type == 1).Sum(t => t.AmountCents) / 100m;
            var netBalance = totalIncome - totalExpense;

            // Build category breakdowns as JSON
            var spendingByCategory = monthTxns
                .Where(t => t.Type == 1)
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(t => t.AmountCents) / 100m);

            var incomeByCategory = monthTxns
                .Where(t => t.Type == 2)
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(t => t.AmountCents) / 100m);

            var snapshotDate = DateOnly.FromDateTime(monthStart);

            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO analytics.analytic_snapshots
                    ("Id", "UserId", "SnapshotDate", "Period", "TotalIncome", "TotalExpense", "NetBalance",
                     "SpendingByCategory", "IncomeByCategory")
                VALUES (@id, @userId, @date, @period, @income, @expense, @net, @spending::jsonb, @incomecat::jsonb)
                """, conn, tx);

            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("userId", TestUserId);
            cmd.Parameters.AddWithValue("date", snapshotDate);
            cmd.Parameters.AddWithValue("period", "Monthly");
            cmd.Parameters.AddWithValue("income", totalIncome);
            cmd.Parameters.AddWithValue("expense", totalExpense);
            cmd.Parameters.AddWithValue("net", netBalance);
            cmd.Parameters.AddWithValue("spending",
                System.Text.Json.JsonSerializer.Serialize(spendingByCategory));
            cmd.Parameters.AddWithValue("incomecat",
                System.Text.Json.JsonSerializer.Serialize(incomeByCategory));

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertNotificationsAsync(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        var now = DateTime.UtcNow;

        var notifications = new (int Type, int Channel, string Title, string Message, int Status, DateTime CreatedAt, DateTime? SentAt, DateTime? ReadAt, string Metadata)[]
        {
            // BudgetWarning (Type=0), InApp (Channel=1), Read (Status=3)
            (0, 1, "Grocery Budget Warning", "You've spent 82% of your grocery budget this month.",
                3, now.AddDays(-5), now.AddDays(-5), now.AddDays(-4), """{"budgetName":"Grocery Budget","percentage":"82"}"""),
            (0, 1, "Coffee Budget Warning", "You've spent 91% of your coffee budget this month.",
                3, now.AddDays(-3), now.AddDays(-3), now.AddDays(-2), """{"budgetName":"Coffee Budget","percentage":"91"}"""),
            (0, 1, "Overall Monthly Budget Warning", "You've spent 80% of your overall monthly budget.",
                3, now.AddDays(-7), now.AddDays(-7), now.AddDays(-6), """{"budgetName":"Overall Monthly","percentage":"80"}"""),

            // BudgetExceeded (Type=1), InApp (Channel=1), Sent (Status=1)
            (1, 1, "Coffee Budget Exceeded", "You've exceeded your $60 coffee budget for this month.",
                1, now.AddDays(-1), now.AddDays(-1), null, """{"budgetName":"Coffee Budget","amountOver":"8.50"}"""),
            (1, 1, "Entertainment Budget Exceeded", "You've exceeded your $150 entertainment budget.",
                1, now.AddDays(-2), now.AddDays(-2), null, """{"budgetName":"Entertainment Budget","amountOver":"12.00"}"""),

            // TransactionCreated (Type=3), InApp (Channel=1), Read (Status=3)
            (3, 1, "Transaction Recorded", "Paycheck - Direct Deposit for $2,600.00 was recorded.",
                3, now.AddDays(-1), now.AddDays(-1), now.AddDays(-1), """{"amount":"260000","description":"Paycheck - Direct Deposit"}"""),
            (3, 1, "Transaction Recorded", "Costco purchase of $127.43 was recorded.",
                3, now.AddDays(-4), now.AddDays(-4), now.AddDays(-3), """{"amount":"12743","description":"Costco"}"""),
            (3, 1, "Transaction Recorded", "Monthly Rent Payment of $1,850.00 was recorded.",
                3, now.AddDays(-10), now.AddDays(-10), now.AddDays(-9), """{"amount":"185000","description":"Monthly Rent Payment"}"""),

            // BudgetCreated (Type=2), Email (Channel=0), Sent (Status=1)
            (2, 0, "Budget Created", "Your 'Overall Monthly' budget of $4,000 has been created.",
                1, now.AddDays(-20), now.AddDays(-20), null, """{"budgetName":"Overall Monthly","amount":"4000"}"""),

            // Recent unread notifications — Pending (Status=0) for UI testing
            (0, 1, "Dining Budget Warning", "You've spent 78% of your dining budget this month.",
                0, now.AddHours(-2), null, null, """{"budgetName":"Dining Budget","percentage":"78"}"""),
            (3, 1, "Transaction Recorded", "Starbucks purchase of $5.75 was recorded.",
                0, now.AddMinutes(-30), null, null, """{"amount":"575","description":"Starbucks"}"""),
        };

        foreach (var (type, channel, title, message, status, createdAt, sentAt, readAt, metadata) in notifications)
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO notifications."Notifications"
                    ("Id", "UserId", "Type", "Channel", "Title", "Message", "Status",
                     "CreatedAt", "SentAt", "ReadAt", "Metadata", "FailureReason")
                VALUES (@id, @userId, @type, @channel, @title, @message, @status,
                        @createdAt, @sentAt, @readAt, @metadata::jsonb, NULL)
                """, conn, tx);

            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("userId", TestUserId);
            cmd.Parameters.AddWithValue("type", type);
            cmd.Parameters.AddWithValue("channel", channel);
            cmd.Parameters.AddWithValue("title", title);
            cmd.Parameters.AddWithValue("message", message);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("createdAt", createdAt);
            cmd.Parameters.AddWithValue("sentAt", sentAt.HasValue ? sentAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("readAt", readAt.HasValue ? readAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("metadata", metadata);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    // --- Helper methods ---

    private static TransactionData MakeTx(Random rng, long amountCents, string currency,
        DateTime date, string description, Guid categoryId, string categoryName, int type)
    {
        return new TransactionData(Guid.NewGuid(), amountCents, currency, date, description,
            categoryId, categoryName, type);
    }

    private static long RandCents(Random rng, int minCents, int maxCents)
        => rng.Next(minCents, maxCents + 1);

    private static DateTime RandomDay(Random rng, DateTime monthStart, int daysInMonth)
        => monthStart.AddDays(rng.Next(0, daysInMonth));

    private static string Pick(Random rng, params string[] options)
        => options[rng.Next(options.Length)];
}
