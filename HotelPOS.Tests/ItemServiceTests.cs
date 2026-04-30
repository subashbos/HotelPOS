using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

/// <summary>
/// Unit tests for ItemService — validates input validation rules and
/// correct delegation to IItemRepository via a mocked dependency.
/// </summary>
public class ItemServiceTests
{
    private readonly Mock<IItemRepository> _mockRepo = new();
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        _service = new ItemService(_mockRepo.Object);
    }

    // ========== AddItemAsync — validation failures ==========

    [Fact]
    public async Task AddItemAsync_NullDto_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddItemAsync(null!));
    }

    [Fact]
    public async Task AddItemAsync_EmptyName_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = "", Price = 50m };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    [Fact]
    public async Task AddItemAsync_WhitespaceName_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = "   ", Price = 50m };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    [Fact]
    public async Task AddItemAsync_TabOnlyName_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = "\t\t", Price = 50m };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    [Fact]
    public async Task AddItemAsync_NameExceeds200Characters_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = new string('A', 201), Price = 50m };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    [Fact]
    public async Task AddItemAsync_ZeroPrice_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = "Coffee", Price = 0m };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    [Fact]
    public async Task AddItemAsync_NegativePrice_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = "Coffee", Price = -0.01m };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    [Fact]
    public async Task AddItemAsync_LargeNegativePrice_ThrowsArgumentException()
    {
        var dto = new CreateItemDto { Name = "Coffee", Price = decimal.MinValue };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(dto));
    }

    // ========== AddItemAsync — boundary values that SHOULD succeed ==========

    [Fact]
    public async Task AddItemAsync_Exactly200CharacterName_Succeeds()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(1);
        var dto = new CreateItemDto { Name = new string('A', 200), Price = 50m };

        var ex = await Record.ExceptionAsync(() => _service.AddItemAsync(dto));
        Assert.Null(ex);
    }

    [Fact]
    public async Task AddItemAsync_SmallestValidPrice_Succeeds()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(1);
        var dto = new CreateItemDto { Name = "Coffee", Price = 0.01m };

        var ex = await Record.ExceptionAsync(() => _service.AddItemAsync(dto));
        Assert.Null(ex);
    }

    [Fact]
    public async Task AddItemAsync_SingleCharacterName_Succeeds()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(1);
        var dto = new CreateItemDto { Name = "A", Price = 10m };

        var ex = await Record.ExceptionAsync(() => _service.AddItemAsync(dto));
        Assert.Null(ex);
    }

    // ========== AddItemAsync — correct behaviour when valid ==========

    [Fact]
    public async Task AddItemAsync_ValidDto_CallsRepositoryExactlyOnce()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(42);
        var dto = new CreateItemDto { Name = "Coffee", Price = 50m };

        await _service.AddItemAsync(dto);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ValidDto_ReturnsRepositoryId()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(42);
        var dto = new CreateItemDto { Name = "Coffee", Price = 50m };

        var result = await _service.AddItemAsync(dto);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AddItemAsync_NameWithLeadingTrailingSpaces_TrimsName()
    {
        Item? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>()))
                 .Callback<Item>(i => captured = i)
                 .ReturnsAsync(1);

        var dto = new CreateItemDto { Name = "  Coffee Latte  ", Price = 50m };
        await _service.AddItemAsync(dto);

        Assert.Equal("Coffee Latte", captured?.Name);
    }

    [Fact]
    public async Task AddItemAsync_ValidDto_SetsCorrectPrice()
    {
        Item? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>()))
                 .Callback<Item>(i => captured = i)
                 .ReturnsAsync(1);

        var dto = new CreateItemDto { Name = "Biryani", Price = 299.99m };
        await _service.AddItemAsync(dto);

        Assert.Equal(299.99m, captured?.Price);
    }

    [Fact]
    public async Task AddItemAsync_ValidDto_SetsCorrectTaxPercentage()
    {
        Item? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>()))
                 .Callback<Item>(i => captured = i)
                 .ReturnsAsync(1);

        var dto = new CreateItemDto { Name = "Biryani", Price = 300m, TaxPercentage = 18m };
        await _service.AddItemAsync(dto);

        Assert.Equal(18m, captured?.TaxPercentage);
    }

    [Fact]
    public async Task AddItemAsync_ValidDto_DoesNotPassIdToRepository()
    {
        // The new Item should have default Id = 0; DB generates the real ID
        Item? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>()))
                 .Callback<Item>(i => captured = i)
                 .ReturnsAsync(1);

        await _service.AddItemAsync(new CreateItemDto { Name = "Dal", Price = 80m });

        Assert.Equal(0, captured?.Id);
    }

    // ========== GetItemsAsync ==========

    [Fact]
    public async Task GetItemsAsync_ReturnsWhatRepositoryReturns()
    {
        var expected = new List<Item>
        {
            new() { Id = 1, Name = "Coffee", Price = 50m },
            new() { Id = 2, Name = "Tea",    Price = 30m }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expected);

        var result = await _service.GetItemsAsync();

        Assert.Equal(2, result.Count);
        Assert.Same(expected[0], result[0]);
    }

    [Fact]
    public async Task GetItemsAsync_EmptyRepository_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());

        var result = await _service.GetItemsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemsAsync_CallsRepositoryExactlyOnce()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());

        await _service.GetItemsAsync();

        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ========== UpdateItemAsync ==========

    [Fact]
    public async Task UpdateItemAsync_ValidDto_UpdatesAllProperties()
    {
        var existing = new Item { Id = 10, Name = "Old", Price = 10m, CategoryId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existing);
        
        var dto = new CreateItemDto 
        { 
            Name = "New Name", 
            Price = 20m, 
            TaxPercentage = 5m, 
            CategoryId = 2 
        };

        await _service.UpdateItemAsync(10, dto);

        Assert.Equal("New Name", existing.Name);
        Assert.Equal(20m, existing.Price);
        Assert.Equal(5m, existing.TaxPercentage);
        Assert.Equal(2, existing.CategoryId);
        _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_NonExistentItem_ThrowsException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Item?)null);
        var dto = new CreateItemDto { Name = "Valid", Price = 10m };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateItemAsync(99, dto));
    }
}
