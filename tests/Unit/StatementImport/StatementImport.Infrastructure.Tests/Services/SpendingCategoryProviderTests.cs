using FluentAssertions;
using Moq;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;
using StatementImport.Infrastructure.Services;

namespace StatementImport.Infrastructure.Tests.Services;

public class SpendingCategoryProviderTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly SpendingCategoryProvider _provider;
    private readonly Guid _userId = Guid.NewGuid();

    // Categories with known IDs
    private readonly List<Category> _systemCategories;

    public SpendingCategoryProviderTests()
    {
        _provider = new SpendingCategoryProvider(_categoryRepositoryMock.Object);

        _systemCategories = new List<Category>
        {
            CreateSystemCategory("Groceries", "Food and household essentials"),
            CreateSystemCategory("Dining Out", "Restaurants and cafes"),
            CreateSystemCategory("Fast Food", "Quick service and fast food"),
            CreateSystemCategory("Coffee/Tea", "Coffee shops and beverages"),
            CreateSystemCategory("Fitness", "Gym, classes, sports equipment"),
            CreateSystemCategory("Rideshare/Taxi", "Uber, Lyft, taxi services"),
            CreateSystemCategory("Entertainment", "Movies, concerts, events"),
            CreateSystemCategory("Clothing", "Clothes, shoes, accessories"),
            CreateSystemCategory("Miscellaneous", "Other expenses not categorized"),
            CreateSystemCategory("Travel", "Flights, hotels, vacation expenses"),
            CreateSystemCategory("Alcohol/Bars", "Bars, clubs, alcoholic beverages"),
            CreateSystemCategory("Home Goods", "Furniture, decor, household items"),
            CreateSystemCategory("Personal Care", "Haircuts, cosmetics, toiletries"),
            CreateSystemCategory("Gas/Fuel", "Vehicle fuel and charging"),
            CreateSystemCategory("Healthcare", "Medical, dental, pharmacy, copays"),
            CreateSystemCategory("Public Transit", "Bus, train, subway, metro")
        };

        _categoryRepositoryMock
            .Setup(x => x.GetAllAvailableCategoriesForUserAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_systemCategories);
    }

    private static Category CreateSystemCategory(string name, string description)
    {
        var result = Category.CreateSystemCategory(name, description);
        return result.Value;
    }

    private Guid GetCategoryId(string name) =>
        _systemCategories.First(c => c.Name == name).Id;

    // --- Tier 1: Exact match ---

    [Fact]
    public async Task GetCategoryIdByNameAsync_ExactMatch_ReturnsId()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Groceries", _userId);

        result.Should().Be(GetCategoryId("Groceries"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_ExactMatch_CaseInsensitive_ReturnsId()
    {
        var result = await _provider.GetCategoryIdByNameAsync("groceries", _userId);

        result.Should().Be(GetCategoryId("Groceries"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_ExactMatch_DiningOut_ReturnsId()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Dining Out", _userId);

        result.Should().Be(GetCategoryId("Dining Out"));
    }

    // --- Tier 2: Synonym match ---

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Restaurant_ReturnsDiningOut()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Restaurant", _userId);

        result.Should().Be(GetCategoryId("Dining Out"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Gym_ReturnsFitness()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Gym", _userId);

        result.Should().Be(GetCategoryId("Fitness"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Coffee_ReturnsCoffeeTea()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Coffee", _userId);

        result.Should().Be(GetCategoryId("Coffee/Tea"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Uber_ReturnsRideshareTaxi()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Uber", _userId);

        result.Should().Be(GetCategoryId("Rideshare/Taxi"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Pharmacy_ReturnsHealthcare()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Pharmacy", _userId);

        result.Should().Be(GetCategoryId("Healthcare"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Hotel_ReturnsTravel()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Hotel", _userId);

        result.Should().Be(GetCategoryId("Travel"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Synonym_Furniture_ReturnsHomeGoods()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Furniture", _userId);

        result.Should().Be(GetCategoryId("Home Goods"));
    }

    // --- Tier 3: Contains match ---

    [Fact]
    public async Task GetCategoryIdByNameAsync_Contains_RestaurantDining_ReturnsDiningOut()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Restaurant Dining", _userId);

        // "Restaurant" is a synonym for "Dining Out" — should match via synonym
        result.Should().Be(GetCategoryId("Dining Out"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Contains_CoffeeShops_ReturnsCoffeeTea()
    {
        // "Coffee Shops" is a synonym
        var result = await _provider.GetCategoryIdByNameAsync("Coffee Shops", _userId);

        result.Should().Be(GetCategoryId("Coffee/Tea"));
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_Contains_FitnessCenter_ReturnsFitness()
    {
        // "Fitness Center" — "Fitness" is an exact category name via contains match
        // (the word "FITNESS" from category "Fitness" has 7 chars >= 4)
        var result = await _provider.GetCategoryIdByNameAsync("Fitness Center", _userId);

        result.Should().Be(GetCategoryId("Fitness"));
    }

    // --- Null/empty input ---

    [Fact]
    public async Task GetCategoryIdByNameAsync_NullInput_ReturnsNull()
    {
        var result = await _provider.GetCategoryIdByNameAsync(null!, _userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_EmptyInput_ReturnsNull()
    {
        var result = await _provider.GetCategoryIdByNameAsync("", _userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryIdByNameAsync_WhitespaceInput_ReturnsNull()
    {
        var result = await _provider.GetCategoryIdByNameAsync("   ", _userId);

        result.Should().BeNull();
    }

    // --- No match ---

    [Fact]
    public async Task GetCategoryIdByNameAsync_NoMatch_ReturnsNull()
    {
        var result = await _provider.GetCategoryIdByNameAsync("Completely Unknown Category XYZ", _userId);

        result.Should().BeNull();
    }
}
