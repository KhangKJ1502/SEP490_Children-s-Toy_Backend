using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Services;
using Xunit;

namespace ToyStore.Tests.Services;

/// <summary>
/// Unit tests for PromotionService.
/// Passed/Failed: [to be filled]
/// Executed Date: [to be filled]
/// Defect ID: [to be filled]
/// </summary>
public class PromotionServiceTests
{
    // ─── Dependencies (mocked) ──────────────────────────────────────────────────
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PromotionService> _logger;
    private readonly ICartService _cartService;
    private readonly IValidator<CreatePromotionDto> _createValidator;
    private readonly IValidator<UpdatePromotionDto> _updateValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    // ─── Repository mocks ───────────────────────────────────────────────────────
    private readonly IPromotionRepository _promotionRepo;
    private readonly IProductRepository _productRepo;

    // ─── System Under Test ──────────────────────────────────────────────────────
    private readonly PromotionService _sut;

    // ─── Shared fixed "now" so date-sensitive tests are deterministic ───────────
    private readonly DateTime _now = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    public PromotionServiceTests()
    {
        _unitOfWork      = Substitute.For<IUnitOfWork>();
        _mapper          = Substitute.For<IMapper>();
        _logger          = Substitute.For<ILogger<PromotionService>>();
        _cartService     = Substitute.For<ICartService>();
        _createValidator = Substitute.For<IValidator<CreatePromotionDto>>();
        _updateValidator = Substitute.For<IValidator<UpdatePromotionDto>>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _timeProvider    = Substitute.For<ITimeProvider>();
        _configuration   = Substitute.For<IConfiguration>();

        _promotionRepo = Substitute.For<IPromotionRepository>();
        _productRepo   = Substitute.For<IProductRepository>();

        _unitOfWork.Promotions.Returns(_promotionRepo);
        _unitOfWork.Products.Returns(_productRepo);

        _timeProvider.UtcNow.Returns(_now);
        _currentUserService.AccountId.Returns(1);

        _sut = new PromotionService(
            _unitOfWork,
            _mapper,
            _logger,
            _cartService,
            _createValidator,
            _updateValidator,
            _currentUserService,
            _timeProvider,
            _configuration);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Function1 – GetPromotionsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    #region Function1 – GetPromotionsAsync

    /// <summary>
    /// UTCID01 – Normal: pageNumber=1, pageSize=10, no filters → Success with PaginatedResponse
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID01_NormalDefaultParams_ReturnsSuccess()
    {
        // Arrange
        var promotions = new List<Promotion> { new Promotion { PromotionId = 1, PromotionName = "Promo 1", PromotionType = "DISCOUNT", Status = "Active", StartDate = _now, EndDate = _now.AddDays(30) } };
        var pagedResult = new PaginatedResponse<Promotion>(promotions, 1, 1, 10);
        _promotionRepo.GetPagedAsync(1, 10, null, false, null, null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(pagedResult));
        _mapper.Map<List<PromotionListDto>>(promotions).Returns(new List<PromotionListDto>());

        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 1, pageSize: 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// UTCID02 – Normal: pageNumber=2, pageSize=10, sortBy=PromotionName, sortDesc=true, searchTerm=Summer, status=Active → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID02_NormalWithAllFilters_ReturnsSuccess()
    {
        // Arrange
        var promotions = new List<Promotion> { new Promotion { PromotionId = 2, PromotionName = "Summer Sale", PromotionType = "DISCOUNT", Status = "Active", StartDate = _now, EndDate = _now.AddDays(30) } };
        var pagedResult = new PaginatedResponse<Promotion>(promotions, 1, 2, 10);
        _promotionRepo.GetPagedAsync(2, 10, "PromotionName", true, "Summer", "Active", Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(pagedResult));
        _mapper.Map<List<PromotionListDto>>(promotions).Returns(new List<PromotionListDto>());

        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 2, pageSize: 10, sortBy: "PromotionName", sortDesc: true, searchTerm: "Summer", status: "Active");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.PageNumber);
    }

    /// <summary>
    /// UTCID03 – Boundary: pageNumber=1, pageSize=1 → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID03_BoundaryPageSizeMin_ReturnsSuccess()
    {
        // Arrange
        var promotions = new List<Promotion>();
        var pagedResult = new PaginatedResponse<Promotion>(promotions, 0, 1, 1);
        _promotionRepo.GetPagedAsync(1, 1, null, false, null, null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(pagedResult));
        _mapper.Map<List<PromotionListDto>>(promotions).Returns(new List<PromotionListDto>());

        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 1, pageSize: 1);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID04 – Boundary: pageNumber=1, pageSize=100 → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID04_BoundaryPageSizeMax_ReturnsSuccess()
    {
        // Arrange
        var promotions = new List<Promotion>();
        var pagedResult = new PaginatedResponse<Promotion>(promotions, 0, 1, 100);
        _promotionRepo.GetPagedAsync(1, 100, null, false, null, null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(pagedResult));
        _mapper.Map<List<PromotionListDto>>(promotions).Returns(new List<PromotionListDto>());

        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 1, pageSize: 100);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID05 – Abnormal: pageNumber=0 → VALIDATION_ERROR: Page number must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID05_AbnormalPageNumberZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 0, pageSize: 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page number must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID06 – Abnormal: pageSize=0 → VALIDATION_ERROR: Page size must be between 1 and 100.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID06_AbnormalPageSizeZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 1, pageSize: 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page size must be between 1 and 100.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID07 – Abnormal: pageSize=101 → VALIDATION_ERROR: Page size must be between 1 and 100.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsAsync_UTCID07_AbnormalPageSizeOverMax_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionsAsync(pageNumber: 1, pageSize: 101);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page size must be between 1 and 100.", result.ErrorMessage);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function2 – GetFlashSalePromotionsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    #region Function2 – GetFlashSalePromotionsAsync

    /// <summary>
    /// UTCID01 – Normal: no params → Success with List of PromotionDto
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetFlashSalePromotionsAsync_UTCID01_Normal_ReturnsSuccess()
    {
        // Arrange
        var configSection = Substitute.For<IConfigurationSection>();
        configSection.Value.Returns("2");
        _configuration.GetSection("Promotions:FlashSaleVisibilityDays").Returns(configSection);

        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, PromotionName = "Flash Sale 1", PromotionType = "FLASH_SALE", Status = "Active", StartDate = _now, EndDate = _now.AddDays(1) }
        };
        _promotionRepo.GetFlashSalePromotionsAsync(2, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(promotions));

        var dtos = new List<PromotionDto> { new PromotionDto { PromotionId = 1, PromotionName = "Flash Sale 1" } };
        _mapper.Map<List<PromotionDto>>(promotions).Returns(dtos);

        // Act
        var result = await _sut.GetFlashSalePromotionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function3 – GetPromotionByIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    #region Function3 – GetPromotionByIdAsync

    /// <summary>
    /// UTCID01 – Normal: promotionId=1, exists → Success with PromotionDto
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionByIdAsync_UTCID01_NormalExistingId_ReturnsSuccess()
    {
        // Arrange
        var promotion = new Promotion
        {
            PromotionId = 1,
            PromotionName = "Test Promo",
            PromotionType = "DISCOUNT",
            Status = "Active",
            StartDate = _now,
            EndDate = _now.AddDays(30)
        };
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(promotion));
        _mapper.Map<PromotionDto>(promotion).Returns(new PromotionDto { PromotionId = 1, PromotionName = "Test Promo" });

        // Act
        var result = await _sut.GetPromotionByIdAsync(promotionId: 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.PromotionId);
    }

    /// <summary>
    /// UTCID02 – Abnormal: promotionId=1, does not exist in repo → NOT_FOUND
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionByIdAsync_UTCID02_AbnormalNotFound_ReturnsNotFound()
    {
        // Arrange
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(null));

        // Act
        var result = await _sut.GetPromotionByIdAsync(promotionId: 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Contains("1", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID03 – Boundary: promotionId=0 → VALIDATION_ERROR: Promotion ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionByIdAsync_UTCID03_BoundaryIdZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionByIdAsync(promotionId: 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Promotion ID must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID04 – Abnormal: promotionId=-1 → VALIDATION_ERROR: Promotion ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionByIdAsync_UTCID04_AbnormalNegativeId_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionByIdAsync(promotionId: -1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Promotion ID must be greater than 0.", result.ErrorMessage);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function4 – CreatePromotionAsync
    // ═══════════════════════════════════════════════════════════════════════════

    #region Function4 – CreatePromotionAsync

    private static CreatePromotionDto BuildCreateDto(
        string promotionName = "New Promotion",
        string promotionType = "DISCOUNT",
        DateTime? startDate = null,
        DateTime? endDate = null,
        string status = "Scheduled",
        int priority = 1,
        List<CreateProductPromotionDto>? productPromotions = null,
        List<CreatePromotionTimeSlotDto>? timeSlots = null)
    {
        return new CreatePromotionDto
        {
            PromotionName = promotionName,
            PromotionType = promotionType,
            StartDate = startDate ?? new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = endDate ?? new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = status,
            Priority = priority,
            ProductPromotions = productPromotions ?? new List<CreateProductPromotionDto>(),
            PromotionTimeSlots = timeSlots ?? new List<CreatePromotionTimeSlotDto>()
        };
    }

    private void SetupValidatorPass()
    {
        _createValidator.ValidateAsync(Arg.Any<CreatePromotionDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
    }

    private void SetupValidatorFail(string propertyName, string message)
    {
        var failures = new List<ValidationFailure> { new ValidationFailure(propertyName, message) };
        _createValidator.ValidateAsync(Arg.Any<CreatePromotionDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult(failures)));
    }

    /// <summary>
    /// UTCID01 – Normal: DISCOUNT type, no products, no time slots → Success with PromotionDto
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID01_NormalDiscount_ReturnsSuccess()
    {
        // Arrange
        var request = BuildCreateDto(promotionType: "DISCOUNT");
        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("New Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        var promotion = new Promotion { PromotionId = 1, PromotionName = "New Promotion", PromotionType = "DISCOUNT", Status = "Scheduled", StartDate = request.StartDate, EndDate = request.EndDate };
        _mapper.Map<Promotion>(request).Returns(promotion);
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(promotion));
        _mapper.Map<PromotionDto>(Arg.Any<Promotion>()).Returns(new PromotionDto { PromotionId = 1, PromotionName = "New Promotion" });

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// UTCID02 – Normal: FLASH_SALE type with 1 product, 1 time slot → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID02_NormalFlashSaleWithProductAndTimeSlot_ReturnsSuccess()
    {
        // Arrange
        var products = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 1, SalePrice = 50000 }
        };
        var slots = new List<CreatePromotionTimeSlotDto>
        {
            new CreatePromotionTimeSlotDto
            {
                StartAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc),
                Status = "Scheduled",
                PromotionProductSlots = new List<CreatePromotionProductSlotDto>
                {
                    new CreatePromotionProductSlotDto { ProductId = 1, SalePrice = 50000, SaleQuantity = 10 }
                }
            }
        };
        var request = BuildCreateDto(promotionType: "FLASH_SALE", productPromotions: products, timeSlots: slots);

        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("New Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        var dbProduct = new Product { ProductId = 1, ProductName = "Toy Car", Price = 100000 };
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new List<Product> { dbProduct }));

        var promotion = new Promotion { PromotionId = 2, PromotionName = "New Promotion", PromotionType = "FLASH_SALE", Status = "Scheduled", StartDate = request.StartDate, EndDate = request.EndDate };
        _mapper.Map<Promotion>(request).Returns(promotion);
        _mapper.Map<ProductPromotion>(Arg.Any<CreateProductPromotionDto>()).Returns(new ProductPromotion());
        _mapper.Map<PromotionTimeSlot>(Arg.Any<CreatePromotionTimeSlotDto>()).Returns(new PromotionTimeSlot { StartAt = slots[0].StartAt, EndAt = slots[0].EndAt, Status = "Scheduled" });
        _promotionRepo.GetByIdAsync(2, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(promotion));
        _mapper.Map<PromotionDto>(Arg.Any<Promotion>()).Returns(new PromotionDto { PromotionId = 2, PromotionName = "New Promotion" });

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// UTCID03 – Abnormal: Name already exists → CONFLICT: Promotion name already exists.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID03_AbnormalDuplicateName_ReturnsConflict()
    {
        // Arrange
        var request = BuildCreateDto(promotionName: "Existing Promotion");
        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("Existing Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(true));

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
        Assert.Equal("Promotion name already exists.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID04 – Abnormal: Empty PromotionName → VALIDATION_ERROR: Promotion name is required.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID04_AbnormalEmptyName_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateDto(promotionName: "");
        SetupValidatorFail("PromotionName", "Promotion name is required.");

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID05 – Abnormal: StartDate in the past → VALIDATION_ERROR: Start date must be at least 10 minutes from now.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID05_AbnormalPastStartDate_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateDto(startDate: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc));
        SetupValidatorFail("StartDate", "Start date must be at least 10 minutes from now.");

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID06 – Abnormal: EndDate before StartDate → VALIDATION_ERROR: End date must be greater than start date.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID06_AbnormalEndDateBeforeStartDate_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateDto(
            startDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc));
        SetupValidatorFail("EndDate", "End date must be greater than start date.");

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID07 – Boundary: Priority=0 → Success (0 is valid, >= 0)
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID07_BoundaryPriorityZero_ReturnsSuccess()
    {
        // Arrange
        var request = BuildCreateDto(priority: 0, promotionType: "FLASH_SALE");
        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("New Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        var promotion = new Promotion { PromotionId = 3, PromotionName = "New Promotion", PromotionType = "FLASH_SALE", Status = "Scheduled", Priority = 0, StartDate = request.StartDate, EndDate = request.EndDate };
        _mapper.Map<Promotion>(request).Returns(promotion);
        _promotionRepo.GetByIdAsync(3, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(promotion));
        _mapper.Map<PromotionDto>(Arg.Any<Promotion>()).Returns(new PromotionDto { PromotionId = 3, PromotionName = "New Promotion" });

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID08 – Abnormal: Priority=-1 → VALIDATION_ERROR: Priority must be greater than or equal to 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID08_AbnormalNegativePriority_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateDto(priority: -1);
        SetupValidatorFail("Priority", "Priority must be greater than or equal to 0.");

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID09 – Abnormal: ProductId=999 does not exist → VALIDATION_ERROR: One or more products do not exist.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID09_AbnormalProductNotFound_ReturnsValidationError()
    {
        // Arrange
        var products = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 999, SalePrice = 50000 }
        };
        var request = BuildCreateDto(promotionType: "FLASH_SALE", productPromotions: products);

        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("New Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        var promotion = new Promotion { PromotionId = 4, PromotionName = "New Promotion", PromotionType = "FLASH_SALE", Status = "Scheduled", StartDate = request.StartDate, EndDate = request.EndDate };
        _mapper.Map<Promotion>(request).Returns(promotion);

        // Return empty list → product 999 does not exist
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new List<Product>()));

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("One or more products do not exist.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID10 – Abnormal: SalePrice > product original price → VALIDATION_ERROR: Sale price for product Toy Car cannot be greater than original price (100000).
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID10_AbnormalSalePriceExceedsOriginal_ReturnsValidationError()
    {
        // Arrange
        var products = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 1, SalePrice = 150000 }
        };
        var request = BuildCreateDto(promotionType: "FLASH_SALE", productPromotions: products);

        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("New Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        var promotion = new Promotion { PromotionId = 5, PromotionName = "New Promotion", PromotionType = "FLASH_SALE", Status = "Scheduled", StartDate = request.StartDate, EndDate = request.EndDate };
        _mapper.Map<Promotion>(request).Returns(promotion);

        var dbProduct = new Product { ProductId = 1, ProductName = "Toy Car", Price = 100000 };
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new List<Product> { dbProduct }));

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Toy Car", result.ErrorMessage);
        Assert.Contains("100000", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID11 – Abnormal: TimeSlot EndAt - StartAt &lt; 10 min → VALIDATION_ERROR via validator
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID11_AbnormalTimeSlotTooShort_ReturnsValidationError()
    {
        // Arrange
        var slots = new List<CreatePromotionTimeSlotDto>
        {
            new CreatePromotionTimeSlotDto
            {
                StartAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                EndAt   = new DateTime(2026, 8, 1, 10, 5, 0, DateTimeKind.Utc),
                Status  = "Scheduled",
                PromotionProductSlots = new List<CreatePromotionProductSlotDto>
                {
                    new CreatePromotionProductSlotDto { ProductId = 1, SalePrice = 50000, SaleQuantity = 10 }
                }
            }
        };
        var request = BuildCreateDto(promotionType: "FLASH_SALE", timeSlots: slots);
        SetupValidatorFail("PromotionTimeSlots[0].EndAt", "End time must be at least 10 minutes after start time.");

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID12 – Abnormal: SaleQuantity=0 in time slot → VALIDATION_ERROR via validator
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID12_AbnormalSaleQuantityZero_ReturnsValidationError()
    {
        // Arrange
        var slots = new List<CreatePromotionTimeSlotDto>
        {
            new CreatePromotionTimeSlotDto
            {
                StartAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                EndAt   = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc),
                Status  = "Scheduled",
                PromotionProductSlots = new List<CreatePromotionProductSlotDto>
                {
                    new CreatePromotionProductSlotDto { ProductId = 1, SalePrice = 50000, SaleQuantity = 0 }
                }
            }
        };
        var request = BuildCreateDto(promotionType: "FLASH_SALE", timeSlots: slots);
        SetupValidatorFail("PromotionTimeSlots[0].PromotionProductSlots[0].SaleQuantity", "Sale quantity must be greater than 0.");

        // Act
        var result = await _sut.CreatePromotionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID13 – Abnormal: Database transaction fails → Exception is thrown
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreatePromotionAsync_UTCID13_AbnormalDatabaseTransactionFails_ThrowsException()
    {
        // Arrange
        var request = BuildCreateDto(promotionType: "FLASH_SALE");
        SetupValidatorPass();
        _promotionRepo.ExistsPromotionNameAsync("New Promotion", null, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        var promotion = new Promotion { PromotionId = 6, PromotionName = "New Promotion", PromotionType = "FLASH_SALE", Status = "Scheduled", StartDate = request.StartDate, EndDate = request.EndDate };
        _mapper.Map<Promotion>(request).Returns(promotion);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                   .Returns<Task<int>>(_ => throw new Exception("Database transaction failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.CreatePromotionAsync(request));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function5 – UpdatePromotionAsync
    // ═══════════════════════════════════════════════════════════════════════════

    #region Function5 – UpdatePromotionAsync

    private void SetupUpdateValidatorPass()
    {
        _updateValidator.ValidateAsync(Arg.Any<UpdatePromotionDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
    }

    private void SetupUpdateValidatorFail(string propertyName, string message)
    {
        var failures = new List<ValidationFailure> { new ValidationFailure(propertyName, message) };
        _updateValidator.ValidateAsync(Arg.Any<UpdatePromotionDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult(failures)));
    }

    private static UpdatePromotionDto BuildUpdateDto(
        string? promotionName = "Updated Name",
        string? promotionType = "FLASH_SALE",
        bool? isDeleted = false,
        string? status = "Inactive",
        DateTime? endDate = null,
        int? priority = null,
        List<CreateProductPromotionDto>? productPromotions = null,
        List<CreatePromotionTimeSlotDto>? timeSlots = null)
    {
        return new UpdatePromotionDto
        {
            PromotionName = promotionName,
            PromotionType = promotionType,
            IsDeleted = isDeleted,
            Status = status,
            EndDate = endDate ?? new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            Priority = priority,
            ProductPromotions = productPromotions,
            PromotionTimeSlots = timeSlots
        };
    }

    private Promotion BuildExistingPromotion(
        int id = 1,
        string status = "Scheduled",
        bool hasTransactions = false)
    {
        var promo = new Promotion
        {
            PromotionId = id,
            PromotionName = "Old Name",
            PromotionType = "FLASH_SALE",
            Status = status,
            StartDate = _now.AddDays(1),
            EndDate = _now.AddDays(60),
            Priority = 1,
            IsDeleted = false,
            CreatedAt = _now,
            ProductPromotions = new List<ProductPromotion>
            {
                new ProductPromotion { ProductId = 1, PromotionId = id, SalePrice = 50000, IsDeleted = false }
            },
            PromotionTimeSlots = new List<PromotionTimeSlot>()
        };

        if (hasTransactions)
        {
            var slot = new PromotionTimeSlot
            {
                TimeSlotId = 1,
                PromotionId = id,
                StartAt = _now.AddDays(1),
                EndAt = _now.AddDays(1).AddHours(1),
                Status = "Scheduled",
                IsDeleted = false,
                PromotionProductSlots = new List<PromotionProductSlot>
                {
                    new PromotionProductSlot { ProductId = 1, SalePrice = 50000, SaleQuantity = 10, SoldQuantity = 5 }
                }
            };
            promo.PromotionTimeSlots.Add(slot);
        }

        return promo;
    }

    /// <summary>
    /// UTCID01 – Normal: promotionId=1, Scheduled→Inactive, with product update → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID01_NormalScheduledToInactive_ReturnsSuccess()
    {
        // Arrange
        var products = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 1, SalePrice = 50000 }
        };
        var request = BuildUpdateDto(status: "Inactive", productPromotions: products);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        var dbProduct = new Product { ProductId = 1, ProductName = "Toy Car", Price = 100000 };
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new List<Product> { dbProduct }));

        _promotionRepo.ExistsPromotionNameAsync("Updated Name", 1, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        // Setup fullValidation pass
        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreatePromotionDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));

        _mapper.Map<CreatePromotionDto>(existing).Returns(new CreatePromotionDto
        {
            PromotionName = "Updated Name",
            PromotionType = "FLASH_SALE",
            StartDate = existing.StartDate,
            EndDate = existing.EndDate,
            Status = "Inactive",
            Priority = 1
        });

        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Is<string?>(s => s != null && s.Contains("Product")))
                      .Returns(Task.FromResult<Promotion?>(existing));
        _mapper.Map<PromotionDto>(Arg.Any<Promotion>()).Returns(new PromotionDto { PromotionId = 1, PromotionName = "Updated Name" });

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// UTCID02 – Boundary: promotionId=0 → VALIDATION_ERROR: Promotion ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID02_BoundaryIdZero_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(status: "Scheduled");

        // Act
        var result = await _sut.UpdatePromotionAsync(0, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Promotion ID must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID03 – Abnormal: promotionId=1, not found in repo → NOT_FOUND
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID03_AbnormalNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = BuildUpdateDto(status: "Scheduled");
        SetupUpdateValidatorPass();
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(null));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    /// <summary>
    /// UTCID04 – Abnormal: IsDeleted=true on Active promotion → VALIDATION_ERROR: Cannot delete an Active promotion.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID04_AbnormalDeleteActivePromotion_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: true, status: "Scheduled");
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Active");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Cannot delete an Active promotion.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID06 – Abnormal: existingPromotion.Status=Expired → VALIDATION_ERROR: Cannot update an Expired promotion.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID06_AbnormalUpdateExpiredPromotion_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: false, status: "Inactive");
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Expired");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Cannot update an Expired promotion.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID07 – Abnormal: Change PromotionType on Active promotion → VALIDATION_ERROR: Cannot modify PromotionType for a promotion that is active or has transactions.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID07_AbnormalChangeTypeOnActivePromotion_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(promotionType: "DISCOUNT", isDeleted: false, status: "Active");
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Active");
        existing.PromotionType = "FLASH_SALE"; // different from request
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("PromotionType", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID08 – Abnormal: Adding products on Active promotion → VALIDATION_ERROR: Cannot add or remove products...
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID08_AbnormalAddProductOnActivePromotion_ReturnsValidationError()
    {
        // Arrange – 2 products coming in, but existing has only 1
        var requestProducts = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 1, SalePrice = 50000 },
            new CreateProductPromotionDto { ProductId = 2, SalePrice = 30000 }
        };
        var request = BuildUpdateDto(promotionType: "FLASH_SALE", isDeleted: false, status: "Active", productPromotions: requestProducts);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Active");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("add or remove products", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID09 – Abnormal: Modify SalePrice on Active promotion → VALIDATION_ERROR: Cannot modify sale price...
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID09_AbnormalChangeSalePriceOnActivePromotion_ReturnsValidationError()
    {
        // Arrange – same product list but different sale price
        var requestProducts = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 1, SalePrice = 40000 } // was 50000
        };
        var request = BuildUpdateDto(promotionType: "FLASH_SALE", isDeleted: false, status: "Active", productPromotions: requestProducts);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Active");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("sale price", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID10 – Abnormal: End date in the past + Active/Scheduled target status → VALIDATION_ERROR: Cannot activate or schedule a promotion with an end date in the past.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID10_AbnormalPastEndDateWithActiveStatus_ReturnsValidationError()
    {
        // Arrange
        var pastEndDate = _now.AddDays(-1); // end date in the past
        var request = BuildUpdateDto(isDeleted: false, status: "Scheduled", endDate: pastEndDate);
        request.StartDate = pastEndDate.AddDays(-1); // start date is 1 day before end date
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        existing.EndDate = pastEndDate;
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("end date in the past", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID11 – Abnormal: Active promotion → try to set Scheduled → VALIDATION_ERROR: Active promotion can only be changed to Inactive or remain Active.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID11_AbnormalActiveToScheduled_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: false, status: "Scheduled", promotionType: "FLASH_SALE", productPromotions: null);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Active");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Active promotion can only be changed to Inactive", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID12 – Abnormal: Inactive promotion → try to set Expired → VALIDATION_ERROR: Inactive promotion can only be rescheduled/reactivated or remain Inactive.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID12_AbnormalInactiveToExpired_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: false, status: "Expired", productPromotions: null);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Inactive");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Inactive promotion can only be rescheduled", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID13 – Abnormal: Scheduled promotion → try to set Expired → VALIDATION_ERROR: Scheduled promotion can only be changed to Active, Inactive, or remain Scheduled.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID13_AbnormalScheduledToExpired_ReturnsValidationError()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: false, status: "Expired", productPromotions: null);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Scheduled promotion can only be changed to Active, Inactive, or remain Scheduled.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID14 – Abnormal: Duplicate promotion name on update → CONFLICT: Promotion name already exists.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID14_AbnormalDuplicateName_ReturnsConflict()
    {
        // Arrange
        var request = BuildUpdateDto(promotionName: "Existing Promotion", isDeleted: false, status: "Inactive", productPromotions: null);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreatePromotionDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
        _mapper.Map<CreatePromotionDto>(Arg.Any<Promotion>()).Returns(new CreatePromotionDto { PromotionName = "Existing Promotion", PromotionType = "FLASH_SALE", StartDate = existing.StartDate, EndDate = existing.EndDate, Status = "Inactive", Priority = 1 });

        _promotionRepo.ExistsPromotionNameAsync("Existing Promotion", 1, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(true));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
        Assert.Equal("Promotion name already exists.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID16 – Abnormal: Product not found during update → VALIDATION_ERROR: One or more products do not exist.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID16_AbnormalProductNotFound_ReturnsValidationError()
    {
        // Arrange
        var requestProducts = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 999, SalePrice = 50000 }
        };
        var request = BuildUpdateDto(isDeleted: false, status: "Inactive", productPromotions: requestProducts);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        _promotionRepo.ExistsPromotionNameAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreatePromotionDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
        _mapper.Map<CreatePromotionDto>(Arg.Any<Promotion>()).Returns(new CreatePromotionDto { PromotionName = "Updated Name", PromotionType = "FLASH_SALE", StartDate = existing.StartDate, EndDate = existing.EndDate, Status = "Inactive", Priority = 1 });

        // Return empty → product 999 not found
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new List<Product>()));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("One or more products do not exist.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID17 – Abnormal: SalePrice > original price on update → VALIDATION_ERROR: Sale price for product Toy Car cannot be greater than original price.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID17_AbnormalSalePriceExceedsOriginal_ReturnsValidationError()
    {
        // Arrange
        var requestProducts = new List<CreateProductPromotionDto>
        {
            new CreateProductPromotionDto { ProductId = 1, SalePrice = 150000 }
        };
        var request = BuildUpdateDto(isDeleted: false, status: "Inactive", productPromotions: requestProducts);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        _promotionRepo.ExistsPromotionNameAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreatePromotionDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
        _mapper.Map<CreatePromotionDto>(Arg.Any<Promotion>()).Returns(new CreatePromotionDto { PromotionName = "Updated Name", PromotionType = "FLASH_SALE", StartDate = existing.StartDate, EndDate = existing.EndDate, Status = "Inactive", Priority = 1 });

        var dbProduct = new Product { ProductId = 1, ProductName = "Toy Car", Price = 100000 };
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new List<Product> { dbProduct }));

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Toy Car", result.ErrorMessage);
        Assert.Contains("100000", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID15 – Abnormal: Database transaction fails on update → Exception is thrown
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID15_AbnormalDatabaseTransactionFails_ThrowsException()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: false, status: "Inactive", productPromotions: null);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        _promotionRepo.ExistsPromotionNameAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(false));

        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreatePromotionDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
        _mapper.Map<CreatePromotionDto>(Arg.Any<Promotion>()).Returns(new CreatePromotionDto { PromotionName = "Updated Name", PromotionType = "FLASH_SALE", StartDate = existing.StartDate, EndDate = existing.EndDate, Status = "Inactive", Priority = 1 });

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                   .Returns<Task<int>>(_ => throw new Exception("Database transaction failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.UpdatePromotionAsync(1, request));
    }

    /// <summary>
    /// UTCID05 – Normal: IsDeleted=true on Scheduled promotion → Soft delete succeeds (Success with PromotionDto)
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdatePromotionAsync_UTCID05_NormalSoftDeleteScheduledPromotion_ReturnsSuccess()
    {
        // Arrange
        var request = BuildUpdateDto(isDeleted: true, status: "Scheduled", productPromotions: null);
        SetupUpdateValidatorPass();

        var existing = BuildExistingPromotion(status: "Scheduled");
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Any<string?>())
                      .Returns(Task.FromResult<Promotion?>(existing));

        // After soft delete, fetch updated promotion
        _promotionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>(), Arg.Is<string?>(s => s != null && s.Contains("Product")))
                      .Returns(Task.FromResult<Promotion?>(existing));
        _mapper.Map<PromotionDto>(Arg.Any<Promotion>())
               .Returns(new PromotionDto { PromotionId = 1, PromotionName = "Old Name" });

        // Act
        var result = await _sut.UpdatePromotionAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function6 – GetPromotionsByProductIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    #region Function6 – GetPromotionsByProductIdAsync

    /// <summary>
    /// UTCID01 – Normal: productId=1, product exists → Success with List&lt;ProductPromotionInfoDto&gt;
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsByProductIdAsync_UTCID01_NormalExistingProduct_ReturnsSuccess()
    {
        // Arrange
        var product = new Product
        {
            ProductId = 1,
            ProductName = "Toy Car",
            Price = 100000,
            CategoryId = 1,
            ProductStatus = "Available",
            StockThreshold = 5,
            LowStockNotificationEnabled = false
        };
        _productRepo.GetByIdAsync(1, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<Product?>(product));

        var promotionList = new List<Application.DTOs.Promotions.ProductPromotionInfoDto>
        {
            new Application.DTOs.Promotions.ProductPromotionInfoDto { PromotionId = 1, PromotionName = "Summer Sale" }
        };
        _promotionRepo.GetPromotionsByProductIdAsync(1, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(promotionList));

        // Act
        var result = await _sut.GetPromotionsByProductIdAsync(productId: 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
    }

    /// <summary>
    /// UTCID03 – Boundary: productId=0 → VALIDATION_ERROR: Product ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsByProductIdAsync_UTCID03_BoundaryIdZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionsByProductIdAsync(productId: 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Product ID must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID04 – Abnormal: productId=-1 → VALIDATION_ERROR: Product ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsByProductIdAsync_UTCID04_AbnormalNegativeId_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetPromotionsByProductIdAsync(productId: -1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Product ID must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID02 – Abnormal: productId=1, product does not exist in repo → NOT_FOUND: Product with ID '1' was not found.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetPromotionsByProductIdAsync_UTCID02_AbnormalProductNotFound_ReturnsNotFound()
    {
        // Arrange
        _productRepo.GetByIdAsync(1, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<Product?>(null));

        // Act
        var result = await _sut.GetPromotionsByProductIdAsync(productId: 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Contains("1", result.ErrorMessage);
    }

    #endregion
}
