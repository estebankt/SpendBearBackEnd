using Spending.Domain.Repositories;
using StatementImport.Application.Abstractions;

namespace StatementImport.Infrastructure.Services;

public class SpendingCategoryProvider : ICategoryProvider
{
    private readonly ICategoryRepository _categoryRepository;

    // Maps common AI-suggested names → exact system category names
    private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Groceries
        ["Grocery"] = "Groceries",
        ["Supermarket"] = "Groceries",
        ["Food & Household"] = "Groceries",
        ["Food"] = "Groceries",

        // Rent/Mortgage
        ["Rent"] = "Rent/Mortgage",
        ["Mortgage"] = "Rent/Mortgage",
        ["Housing"] = "Rent/Mortgage",

        // Utilities
        ["Utility"] = "Utilities",
        ["Electric"] = "Utilities",
        ["Water"] = "Utilities",
        ["Gas Bill"] = "Utilities",
        ["Power"] = "Utilities",

        // Internet/Phone
        ["Internet"] = "Internet/Phone",
        ["Phone"] = "Internet/Phone",
        ["Mobile"] = "Internet/Phone",
        ["Cell Phone"] = "Internet/Phone",
        ["Telecom"] = "Internet/Phone",
        ["Telecommunications"] = "Internet/Phone",

        // Insurance
        ["Auto Insurance"] = "Insurance",
        ["Health Insurance"] = "Insurance",
        ["Home Insurance"] = "Insurance",
        ["Life Insurance"] = "Insurance",

        // Healthcare
        ["Health"] = "Healthcare",
        ["Medical"] = "Healthcare",
        ["Pharmacy"] = "Healthcare",
        ["Doctor"] = "Healthcare",
        ["Dental"] = "Healthcare",
        ["Hospital"] = "Healthcare",
        ["Copay"] = "Healthcare",
        ["Medicine"] = "Healthcare",

        // Gas/Fuel
        ["Gas"] = "Gas/Fuel",
        ["Fuel"] = "Gas/Fuel",
        ["Gasoline"] = "Gas/Fuel",
        ["Auto Fuel"] = "Gas/Fuel",
        ["EV Charging"] = "Gas/Fuel",

        // Public Transit
        ["Transit"] = "Public Transit",
        ["Bus"] = "Public Transit",
        ["Train"] = "Public Transit",
        ["Subway"] = "Public Transit",
        ["Metro"] = "Public Transit",
        ["Transportation"] = "Public Transit",

        // Parking
        ["Parking Fee"] = "Parking",
        ["Parking Garage"] = "Parking",

        // Vehicle Maintenance
        ["Car Repair"] = "Vehicle Maintenance",
        ["Auto Repair"] = "Vehicle Maintenance",
        ["Car Maintenance"] = "Vehicle Maintenance",
        ["Oil Change"] = "Vehicle Maintenance",
        ["Tires"] = "Vehicle Maintenance",
        ["Auto Service"] = "Vehicle Maintenance",
        ["Car Service"] = "Vehicle Maintenance",
        ["Auto Maintenance"] = "Vehicle Maintenance",

        // Rideshare/Taxi
        ["Rideshare"] = "Rideshare/Taxi",
        ["Taxi"] = "Rideshare/Taxi",
        ["Uber"] = "Rideshare/Taxi",
        ["Lyft"] = "Rideshare/Taxi",
        ["Cab"] = "Rideshare/Taxi",
        ["Ride Share"] = "Rideshare/Taxi",

        // Dining Out
        ["Restaurant"] = "Dining Out",
        ["Restaurants"] = "Dining Out",
        ["Dining"] = "Dining Out",
        ["Dine Out"] = "Dining Out",
        ["Restaurant Dining"] = "Dining Out",
        ["Sit-Down Dining"] = "Dining Out",
        ["Food & Drink"] = "Dining Out",
        ["Eating Out"] = "Dining Out",
        ["Cafe"] = "Dining Out",

        // Fast Food
        ["Quick Service"] = "Fast Food",
        ["Drive-Through"] = "Fast Food",
        ["Drive Thru"] = "Fast Food",
        ["Fast Casual"] = "Fast Food",

        // Coffee/Tea
        ["Coffee"] = "Coffee/Tea",
        ["Tea"] = "Coffee/Tea",
        ["Coffee Shop"] = "Coffee/Tea",
        ["Coffee Shops"] = "Coffee/Tea",
        ["Cafe Coffee"] = "Coffee/Tea",
        ["Beverage"] = "Coffee/Tea",
        ["Beverages"] = "Coffee/Tea",

        // Alcohol/Bars
        ["Alcohol"] = "Alcohol/Bars",
        ["Bars"] = "Alcohol/Bars",
        ["Bar"] = "Alcohol/Bars",
        ["Nightlife"] = "Alcohol/Bars",
        ["Liquor"] = "Alcohol/Bars",
        ["Wine"] = "Alcohol/Bars",
        ["Brewery"] = "Alcohol/Bars",
        ["Pub"] = "Alcohol/Bars",

        // Clothing
        ["Clothes"] = "Clothing",
        ["Apparel"] = "Clothing",
        ["Fashion"] = "Clothing",
        ["Shoes"] = "Clothing",
        ["Accessories"] = "Clothing",

        // Electronics
        ["Technology"] = "Electronics",
        ["Tech"] = "Electronics",
        ["Gadgets"] = "Electronics",
        ["Computer"] = "Electronics",
        ["Computers"] = "Electronics",

        // Home Goods
        ["Home"] = "Home Goods",
        ["Household"] = "Home Goods",
        ["Furniture"] = "Home Goods",
        ["Home Decor"] = "Home Goods",
        ["Home Improvement"] = "Home Goods",
        ["Household Items"] = "Home Goods",

        // Personal Care
        ["Beauty"] = "Personal Care",
        ["Cosmetics"] = "Personal Care",
        ["Grooming"] = "Personal Care",
        ["Haircut"] = "Personal Care",
        ["Toiletries"] = "Personal Care",
        ["Salon"] = "Personal Care",
        ["Spa"] = "Personal Care",

        // Subscriptions
        ["Subscription"] = "Subscriptions",
        ["Streaming"] = "Subscriptions",
        ["Membership"] = "Subscriptions",
        ["Software"] = "Subscriptions",
        ["Digital Services"] = "Subscriptions",
        ["Online Services"] = "Subscriptions",

        // Entertainment
        ["Movies"] = "Entertainment",
        ["Movie"] = "Entertainment",
        ["Concert"] = "Entertainment",
        ["Concerts"] = "Entertainment",
        ["Events"] = "Entertainment",
        ["Theater"] = "Entertainment",
        ["Theatre"] = "Entertainment",
        ["Amusement"] = "Entertainment",
        ["Recreation"] = "Entertainment",

        // Hobbies
        ["Hobby"] = "Hobbies",
        ["Sports"] = "Hobbies",
        ["Crafts"] = "Hobbies",
        ["Arts & Crafts"] = "Hobbies",

        // Fitness
        ["Gym"] = "Fitness",
        ["Gym Membership"] = "Fitness",
        ["Workout"] = "Fitness",
        ["Exercise"] = "Fitness",
        ["Sports Equipment"] = "Fitness",
        ["Health & Fitness"] = "Fitness",
        ["Health Club"] = "Fitness",

        // Travel
        ["Flights"] = "Travel",
        ["Flight"] = "Travel",
        ["Hotel"] = "Travel",
        ["Hotels"] = "Travel",
        ["Vacation"] = "Travel",
        ["Airfare"] = "Travel",
        ["Lodging"] = "Travel",
        ["Accommodation"] = "Travel",
        ["Travel Expenses"] = "Travel",

        // Education
        ["Tuition"] = "Education",
        ["School"] = "Education",
        ["Books"] = "Education",
        ["Course"] = "Education",
        ["Courses"] = "Education",
        ["Online Learning"] = "Education",
        ["Training"] = "Education",

        // Gifts/Donations
        ["Gifts"] = "Gifts/Donations",
        ["Gift"] = "Gifts/Donations",
        ["Donations"] = "Gifts/Donations",
        ["Donation"] = "Gifts/Donations",
        ["Charity"] = "Gifts/Donations",
        ["Contributions"] = "Gifts/Donations",

        // Pet Care
        ["Pet"] = "Pet Care",
        ["Pets"] = "Pet Care",
        ["Veterinary"] = "Pet Care",
        ["Vet"] = "Pet Care",
        ["Pet Food"] = "Pet Care",
        ["Pet Supplies"] = "Pet Care",

        // Taxes
        ["Tax"] = "Taxes",
        ["Income Tax"] = "Taxes",
        ["Property Tax"] = "Taxes",
        ["Sales Tax"] = "Taxes",
        ["State Tax"] = "Taxes",
        ["Federal Tax"] = "Taxes",
        ["Tax Payment"] = "Taxes",
        ["Tax Preparation"] = "Taxes",

        // Miscellaneous
        ["Other"] = "Miscellaneous",
        ["General"] = "Miscellaneous",
        ["Uncategorized"] = "Miscellaneous"
    };

    public SpendingCategoryProvider(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryInfo>> GetAvailableCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAvailableCategoriesForUserAsync(userId, cancellationToken);
        return categories.Select(c => new CategoryInfo(c.Id, c.Name, c.Description)).ToList();
    }

    public async Task<Guid?> GetCategoryIdByNameAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var categories = await _categoryRepository.GetAllAvailableCategoriesForUserAsync(userId, cancellationToken);
        var trimmedName = name.Trim();

        // Tier 1: Exact match (case-insensitive)
        var exactMatch = categories.FirstOrDefault(c =>
            c.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
            return exactMatch.Id;

        // Tier 2: Synonym dictionary
        if (Synonyms.TryGetValue(trimmedName, out var mappedName))
        {
            var synonymMatch = categories.FirstOrDefault(c =>
                c.Name.Equals(mappedName, StringComparison.OrdinalIgnoreCase));
            if (synonymMatch is not null)
                return synonymMatch.Id;
        }

        // Tier 3: Contains match — AI name contains a category word or vice versa
        var normalizedInput = trimmedName.ToUpperInvariant();
        foreach (var category in categories)
        {
            var normalizedCategory = category.Name.ToUpperInvariant();

            // "Restaurant Dining" contains "Dining" which is in "Dining Out"
            // Split category name on common delimiters and check if input contains any significant word
            var categoryWords = normalizedCategory.Split(new[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in categoryWords)
            {
                // Skip very short words that would cause false positives
                if (word.Length < 4)
                    continue;

                if (normalizedInput.Contains(word, StringComparison.OrdinalIgnoreCase))
                    return category.Id;
            }
        }

        return null;
    }
}
