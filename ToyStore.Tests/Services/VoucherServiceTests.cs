using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Services;
using Xunit;

namespace ToyStore.Tests.Services;

/// <summary>
/// Unit tests for VoucherService.
/// Passed/Failed: [to be filled]
/// Executed Date: [to be filled]
/// Defect ID: [to be filled]
/// </summary>
public class VoucherServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VoucherService> _logger;
    private readonly IValidator<CreateVoucherDto> _createValidator;
    private readonly IValidator<UpdateVoucherDto> _updateValidator;
    private readonly ITimeProvider _timeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOptions<VoucherRiskThresholds> _optionsThresholds;

    private readonly IVoucherRepository _voucherRepo;
    private readonly VoucherService _sut;

    private readonly DateTime _now = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly VoucherRiskThresholds _thresholds = new VoucherRiskThresholds
    {
        MaxDiscountCap = 200000m,
        MaxTotalDiscount = 20000000m
    };

    public VoucherServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<VoucherService>>();
        _createValidator = Substitute.For<IValidator<CreateVoucherDto>>();
        _updateValidator = Substitute.For<IValidator<UpdateVoucherDto>>();
        _timeProvider = Substitute.For<ITimeProvider>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _optionsThresholds = Options.Create(_thresholds);

        _voucherRepo = Substitute.For<IVoucherRepository>();
        _unitOfWork.Vouchers.Returns(_voucherRepo);

        _timeProvider.UtcNow.Returns(_now);
        _currentUserService.AccountId.Returns(1);
        _currentUserService.RoleName.Returns("Admin");

        // General mapper mock for mapping collections and objects
        _mapper.Map<Voucher>(Arg.Any<CreateVoucherDto>()).Returns(call => {
            var dto = call.Arg<CreateVoucherDto>();
            return new Voucher
            {
                VoucherCode = dto.VoucherCode,
                VoucherName = dto.VoucherName,
                VoucherDescription = dto.VoucherDescription,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountCap = dto.MaxDiscountCap,
                DiscountTarget = dto.DiscountTarget,
                MinOrderAmount = dto.MinOrderAmount,
                TotalQuantity = dto.TotalQuantity,
                MaxUsagePerUser = dto.MaxUsagePerUser,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status
            };
        });

        _mapper.Map<VoucherDto>(Arg.Any<Voucher>()).Returns(call => {
            var entity = call.Arg<Voucher>();
            return new VoucherDto
            {
                VoucherId = entity.VoucherId,
                VoucherCode = entity.VoucherCode,
                VoucherName = entity.VoucherName,
                VoucherDescription = entity.VoucherDescription,
                DiscountType = entity.DiscountType,
                DiscountValue = entity.DiscountValue,
                MaxDiscountCap = entity.MaxDiscountCap,
                DiscountTarget = entity.DiscountTarget,
                MinOrderAmount = entity.MinOrderAmount,
                TotalQuantity = entity.TotalQuantity,
                UsedQuantity = entity.UsedQuantity,
                MaxUsagePerUser = entity.MaxUsagePerUser,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = entity.Status,
                Reason = entity.Reason,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        });

        _mapper.When(m => m.Map(Arg.Any<UpdateVoucherDto>(), Arg.Any<Voucher>()))
               .Do(call => {
                   var src = call.Arg<UpdateVoucherDto>();
                   var dest = call.Arg<Voucher>();
                   if (src.VoucherCode != null) dest.VoucherCode = src.VoucherCode;
                   if (src.VoucherName != null) dest.VoucherName = src.VoucherName;
                   if (src.VoucherDescription != null) dest.VoucherDescription = src.VoucherDescription;
                   if (src.DiscountType != null) dest.DiscountType = src.DiscountType;
                   if (src.DiscountValue.HasValue) dest.DiscountValue = src.DiscountValue.Value;
                   if (src.MaxDiscountCap.HasValue) dest.MaxDiscountCap = src.MaxDiscountCap;
                   if (src.DiscountTarget != null) dest.DiscountTarget = src.DiscountTarget;
                   if (src.MinOrderAmount.HasValue) dest.MinOrderAmount = src.MinOrderAmount;
                   if (src.TotalQuantity.HasValue) dest.TotalQuantity = src.TotalQuantity;
                   if (src.MaxUsagePerUser.HasValue) dest.MaxUsagePerUser = src.MaxUsagePerUser;
                   if (src.StartDate.HasValue) dest.StartDate = src.StartDate.Value;
                   if (src.EndDate.HasValue) dest.EndDate = src.EndDate.Value;
                   if (src.Status != null) dest.Status = src.Status;
                   if (src.Reason != null) dest.Reason = src.Reason;
                   if (src.IsDeleted.HasValue) dest.IsDeleted = src.IsDeleted.Value;
               });

        _sut = new VoucherService(
            _unitOfWork,
            _mapper,
            _logger,
            _createValidator,
            _updateValidator,
            _timeProvider,
            _currentUserService,
            _optionsThresholds);
    }

    private void SetupCreateValidatorPass()
    {
        _createValidator.ValidateAsync(Arg.Any<CreateVoucherDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreateVoucherDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
    }

    private void SetupCreateValidatorFail(string propertyName, string error)
    {
        var result = new ValidationResult(new[] { new ValidationFailure(propertyName, error) });
        _createValidator.ValidateAsync(Arg.Any<CreateVoucherDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(result));
    }

    private void SetupUpdateValidatorPass()
    {
        _updateValidator.ValidateAsync(Arg.Any<UpdateVoucherDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new ValidationResult()));
    }

    private void SetupUpdateValidatorFail(string propertyName, string error)
    {
        var result = new ValidationResult(new[] { new ValidationFailure(propertyName, error) });
        _updateValidator.ValidateAsync(Arg.Any<UpdateVoucherDto>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(result));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Function1 – GetVouchersAsync
    // ═══════════════════════════════════════════════════════════════════════════
    #region Function1 – GetVouchersAsync

    /// <summary>
    /// UTCID01 – Normal: pageNumber=1, pageSize=10, unauthenticated user → Success with PaginatedResponse
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID01_NormalUnauthenticated_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.AccountId.Returns(0);
        var vouchers = new List<Voucher> { new Voucher { VoucherId = 1, VoucherCode = "SAVE10" } };
        var pagedResponse = new PaginatedResponse<Voucher>(vouchers, 1, 1, 10);
        _voucherRepo.GetPagedAsync(1, 10, null, false, null, null, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(pagedResponse));

        var dtos = new List<VoucherListDto> { new VoucherListDto { VoucherId = 1, VoucherCode = "SAVE10" } };
        _mapper.Map<List<VoucherListDto>>(vouchers).Returns(dtos);

        // Act
        var result = await _sut.GetVouchersAsync(pageNumber: 1, pageSize: 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
    }

    /// <summary>
    /// UTCID02 – Normal: pageNumber=1, pageSize=10, authenticated user with user usage count tracked → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID02_NormalAuthenticated_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.AccountId.Returns(10);
        var vouchers = new List<Voucher> { new Voucher { VoucherId = 1, VoucherCode = "SAVE10", MaxUsagePerUser = 2 } };
        var pagedResponse = new PaginatedResponse<Voucher>(vouchers, 1, 1, 10);
        _voucherRepo.GetPagedAsync(1, 10, null, false, null, null, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(pagedResponse));

        var dtos = new List<VoucherListDto> { new VoucherListDto { VoucherId = 1, VoucherCode = "SAVE10", MaxUsagePerUser = 2 } };
        _mapper.Map<List<VoucherListDto>>(vouchers).Returns(dtos);

        _voucherRepo.CountUsageByAccountAsync(1, 10, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(1));

        // Act
        var result = await _sut.GetVouchersAsync(pageNumber: 1, pageSize: 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Items[0].CurrentUserUsageCount);
    }

    /// <summary>
    /// UTCID03 – Normal: pageNumber=1, pageSize=10, filters and sorting → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID03_NormalFiltersAndSorting_ReturnsSuccess()
    {
        // Arrange
        var vouchers = new List<Voucher>();
        var pagedResponse = new PaginatedResponse<Voucher>(vouchers, 0, 1, 10);
        _voucherRepo.GetPagedAsync(1, 10, "VoucherCode", true, "SUMMER", "Active", Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(pagedResponse));
        _mapper.Map<List<VoucherListDto>>(vouchers).Returns(new List<VoucherListDto>());

        // Act
        var result = await _sut.GetVouchersAsync(1, 10, "VoucherCode", true, "SUMMER", "Active");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// UTCID04 – Boundary: pageNumber=1, pageSize=1 → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID04_BoundaryPageSizeMin_ReturnsSuccess()
    {
        // Arrange
        var vouchers = new List<Voucher>();
        var pagedResponse = new PaginatedResponse<Voucher>(vouchers, 0, 1, 1);
        _voucherRepo.GetPagedAsync(1, 1, null, false, null, null, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(pagedResponse));
        _mapper.Map<List<VoucherListDto>>(vouchers).Returns(new List<VoucherListDto>());

        // Act
        var result = await _sut.GetVouchersAsync(1, 1);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID05 – Boundary: pageNumber=1, pageSize=100 → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID05_BoundaryPageSizeMax_ReturnsSuccess()
    {
        // Arrange
        var vouchers = new List<Voucher>();
        var pagedResponse = new PaginatedResponse<Voucher>(vouchers, 0, 1, 100);
        _voucherRepo.GetPagedAsync(1, 100, null, false, null, null, Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(pagedResponse));
        _mapper.Map<List<VoucherListDto>>(vouchers).Returns(new List<VoucherListDto>());

        // Act
        var result = await _sut.GetVouchersAsync(1, 100);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID06 – Abnormal: pageNumber=0 → VALIDATION_ERROR: Page number must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID06_AbnormalPageNumberZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetVouchersAsync(pageNumber: 0, pageSize: 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page number must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID07 – Abnormal: pageNumber=-1 → VALIDATION_ERROR: Page number must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID07_AbnormalPageNumberNegative_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetVouchersAsync(pageNumber: -1, pageSize: 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page number must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID08 – Abnormal: pageSize=0 → VALIDATION_ERROR: Page size must be between 1 and 100.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID08_AbnormalPageSizeZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetVouchersAsync(pageNumber: 1, pageSize: 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page size must be between 1 and 100.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID09 – Abnormal: pageSize=101 → VALIDATION_ERROR: Page size must be between 1 and 100.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVouchersAsync_UTCID09_AbnormalPageSizeOverMax_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetVouchersAsync(pageNumber: 1, pageSize: 101);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Page size must be between 1 and 100.", result.ErrorMessage);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function2 – CreateVoucherAsync
    // ═══════════════════════════════════════════════════════════════════════════
    #region Function2 – CreateVoucherAsync

    private static CreateVoucherDto BuildCreateRequest(
        string code = "SAVE10",
        string role = "Admin",
        string type = "PERCENTAGE",
        decimal val = 10,
        string target = "ORDER_TOTAL",
        int? qty = 100,
        short? usage = 1,
        DateTime? start = null,
        DateTime? end = null,
        decimal? maxDiscountCap = null)
    {
        return new CreateVoucherDto
        {
            VoucherCode = code,
            VoucherName = "Test Voucher",
            VoucherDescription = "Test Voucher Description",
            DiscountType = type,
            DiscountValue = val,
            MaxDiscountCap = maxDiscountCap,
            DiscountTarget = target,
            TotalQuantity = qty,
            MaxUsagePerUser = usage,
            StartDate = start ?? DateTime.UtcNow.AddDays(1),
            EndDate = end ?? DateTime.UtcNow.AddDays(11),
            Status = "Scheduled"
        };
    }

    /// <summary>
    /// UTCID01 – Normal: Admin, PERCENTAGE voucher → Success, Status = Scheduled
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID01_AdminPercentage_ReturnsSuccessScheduled()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildCreateRequest();
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Scheduled", result.Data.Status);
    }

    /// <summary>
    /// UTCID02 – Normal: Staff, PERCENTAGE voucher under threshold → Success, Status = Scheduled
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID02_StaffPercentageUnderThreshold_ReturnsSuccessScheduled()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildCreateRequest(qty: null, maxDiscountCap: 150000m); // max discount cap 150_000 <= 200_000, max total discount: (150_000 * 1) <= 20_000_000
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Scheduled", result.Data.Status);
    }

    /// <summary>
    /// UTCID03 – Normal: Staff, PERCENTAGE voucher above threshold (Cap) → Success, Status = Pending
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID03_StaffPercentageAboveThresholdCap_ReturnsSuccessPending()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildCreateRequest(maxDiscountCap: 250000m); // max discount cap 250_000 > 200_000
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Data.Status);
    }

    /// <summary>
    /// UTCID04 – Normal: Staff, PERCENTAGE voucher above threshold (Total) → Success, Status = Pending
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID04_StaffPercentageAboveThresholdTotal_ReturnsSuccessPending()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildCreateRequest(qty: 150, maxDiscountCap: 150000m); // 150 * 150_000 = 22_500_000 > 20_000_000
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Data.Status);
    }

    /// <summary>
    /// UTCID05 – Normal: Staff, FIXED voucher, SHIPPING_FEE target under threshold → Success, Status = Scheduled
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID05_StaffFixedShippingFeeUnderThreshold_ReturnsSuccessScheduled()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildCreateRequest(type: "FIXED", val: 10000m, target: "SHIPPING_FEE", qty: 100, usage: null);
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Scheduled", result.Data.Status);
    }

    /// <summary>
    /// UTCID06 – Normal: Staff, FIXED voucher above threshold (DiscountValue) → Success, Status = Pending
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID06_StaffFixedAboveThresholdValue_ReturnsSuccessPending()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildCreateRequest(type: "FIXED", val: 250000m, target: "ORDER_TOTAL", qty: 100); // 250_000 > 200_000
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Data.Status);
    }

    /// <summary>
    /// UTCID07 – Normal: Staff, FINAL_PRICE target → Success, Status = Pending
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID07_StaffFinalPriceTarget_ReturnsSuccessPending()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildCreateRequest(target: "FINAL_PRICE");
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Data.Status);
    }

    /// <summary>
    /// UTCID08 – Boundary: DiscountValue = 100 for percentage → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID08_BoundaryPercentageDiscountValue100_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildCreateRequest(val: 100);
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID09 – Boundary: StartDate = 10 minutes from now, code = "SAV" → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID09_BoundaryStartDateAndCodeMinLength_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildCreateRequest(code: "SAV", start: _now.AddMinutes(10));
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAV", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAV", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAV", Status = "Scheduled" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID10 – Boundary: code = "SAVE_CONTAINS_30_CHARS_NOW_MAX" → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID10_BoundaryCodeMaxLength_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildCreateRequest(code: "SAVE_CONTAINS_30_CHARS_NOW_MAX");
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE_CONTAINS_30_CHARS_NOW_MAX", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE_CONTAINS_30_CHARS_NOW_MAX", Arg.Any<CancellationToken>()).Returns(new Voucher { VoucherId = 1, VoucherCode = "SAVE_CONTAINS_30_CHARS_NOW_MAX", Status = "Scheduled" });

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID11 – Abnormal: VoucherCode already exists → Conflict
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID11_AbnormalCodeExists_ReturnsConflict()
    {
        // Arrange
        var request = BuildCreateRequest();
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    /// <summary>
    /// UTCID12 – Abnormal: VoucherCode too short (2 chars) → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID12_AbnormalCodeTooShort_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(code: "SA");
        SetupCreateValidatorFail("VoucherCode", "Voucher code must be at least 3 characters.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID13 – Abnormal: VoucherCode has invalid characters → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID13_AbnormalCodeInvalidChars_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(code: "SAVE!10");
        SetupCreateValidatorFail("VoucherCode", "Voucher code must contain only letters, numbers, underscores, or hyphens.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID14 – Abnormal: DiscountValue > 100 for percentage → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID14_AbnormalPercentageDiscountValueOverMax_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(val: 101);
        SetupCreateValidatorFail("DiscountValue", "Discount value must be less than or equal to 100 for percentage vouchers.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID15 – Abnormal: DiscountType is invalid → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID15_AbnormalDiscountTypeInvalid_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(type: "INVALID");
        SetupCreateValidatorFail("DiscountType", "Discount type must be either FIXED or PERCENTAGE.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID16 – Abnormal: StartDate is in the past → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID16_AbnormalStartDatePast_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(start: _now.AddDays(-1));
        SetupCreateValidatorFail("StartDate", "Start date must be at least 10 minutes from now.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID17 – Abnormal: Voucher duration less than 1 day → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID17_AbnormalDurationTooShort_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(start: _now.AddDays(1), end: _now.AddDays(1));
        SetupCreateValidatorFail("EndDate", "Voucher duration must be at least 1 day.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID18 – Abnormal: Database connection lost during transaction → Throws Exception
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID18_DatabaseException_ThrowsException()
    {
        // Arrange
        var request = BuildCreateRequest();
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                   .Returns<Task<int>>(_ => throw new Exception("Database connection lost."));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.CreateVoucherAsync(request));
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// UTCID19 – Abnormal: Created voucher could not be loaded → Failure("ERROR", "Failed to create voucher.")
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID19_LoadFailed_ReturnsError()
    {
        // Arrange
        var request = BuildCreateRequest();
        SetupCreateValidatorPass();
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", null, Arg.Any<CancellationToken>()).Returns(false);
        _voucherRepo.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns((Voucher?)null);

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ERROR", result.ErrorCode);
        Assert.Equal("Failed to create voucher.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID20 – Abnormal: DiscountValue = 0 → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID20_AbnormalDiscountValueZero_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(val: 0);
        SetupCreateValidatorFail("DiscountValue", "Discount value must be greater than 0.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID21 – Abnormal: TotalQuantity = 0 → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID21_AbnormalQuantityZero_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(qty: 0);
        SetupCreateValidatorFail("TotalQuantity", "Total quantity must be greater than 0 when provided.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID22 – Abnormal: MaxUsagePerUser = 0 → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID22_AbnormalUsageZero_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(usage: 0);
        SetupCreateValidatorFail("MaxUsagePerUser", "Max usage per user must be greater than or equal to 1 when provided.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID23 – Abnormal: StartDate greater than EndDate → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task CreateVoucherAsync_UTCID23_AbnormalStartAfterEnd_ReturnsValidationError()
    {
        // Arrange
        var request = BuildCreateRequest(start: _now.AddDays(5), end: _now.AddDays(2));
        SetupCreateValidatorFail("StartDate", "Start date must be earlier than end date.");

        // Act
        var result = await _sut.CreateVoucherAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function3 – UpdateVoucherAsync
    // ═══════════════════════════════════════════════════════════════════════════
    #region Function3 – UpdateVoucherAsync

    private static UpdateVoucherDto BuildUpdateRequest(
        string? name = null,
        bool? isDeleted = null,
        string? status = null,
        string? target = null,
        DateTime? start = null,
        DateTime? end = null,
        int? qty = null,
        short? usage = null,
        string? code = null,
        string? reason = null)
    {
        return new UpdateVoucherDto
        {
            VoucherCode = code,
            VoucherName = name,
            VoucherDescription = name is null ? null : "New Description",
            DiscountType = null,
            DiscountValue = null,
            MaxDiscountCap = null,
            DiscountTarget = target,
            MinOrderAmount = null,
            TotalQuantity = qty,
            MaxUsagePerUser = usage,
            StartDate = start,
            EndDate = end,
            Status = status,
            Reason = reason,
            IsDeleted = isDeleted
        };
    }

    /// <summary>
    /// UTCID01 – Normal: Admin, update name and quantity → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID01_AdminNormalUpdate_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(name: "New Name", qty: 100, usage: 1);
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", 1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", existing.VoucherName);
        Assert.Equal(100, existing.TotalQuantity);
    }

    /// <summary>
    /// UTCID02 – Normal: Staff, soft delete non-active voucher → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID02_StaffSoftDeleteScheduled_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildUpdateRequest(isDeleted: true);
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled", EndDate = _now.AddDays(10), IsDeleted = false, DiscountValue = 10, StartDate = _now, DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(existing.IsDeleted);
    }

    /// <summary>
    /// UTCID03 – Normal: Staff, update with no fields changed → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID03_StaffNoChanges_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildUpdateRequest();
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", 1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// UTCID04 – Admin rejects voucher with reason → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID04_AdminRejectWithReason_ReturnsSuccess()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Rejected", reason: "Exceeds margin limit");
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", 1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected", existing.Status);
        Assert.Equal("Exceeds margin limit", existing.Reason);
    }

    /// <summary>
    /// UTCID05 – Admin approves scheduled voucher with past start date → Success (Status becomes Active)
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID05_AdminApprovePastStart_ReturnsSuccessActive()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Scheduled", start: _now.AddDays(-1), end: _now.AddDays(10));
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending", EndDate = _now.AddDays(10), StartDate = _now.AddDays(-1), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", 1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Active", existing.Status);
    }

    /// <summary>
    /// UTCID06 – Admin approves scheduled voucher with future start date → Success (Status remains Scheduled)
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID06_AdminApproveFutureStart_ReturnsSuccessScheduled()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Scheduled", start: _now.AddDays(2), end: _now.AddDays(10));
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending", EndDate = _now.AddDays(10), StartDate = _now.AddDays(2), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", 1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Scheduled", existing.Status);
    }

    /// <summary>
    /// UTCID07 – Boundary: voucherId = 0 → VALIDATION_ERROR: Voucher ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID07_BoundaryIdZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.UpdateVoucherAsync(0, BuildUpdateRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Voucher ID must be greater than 0.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID08 – Abnormal: Voucher does not exist → NOT_FOUND
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID08_AbnormalNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUpdateValidatorPass();
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Voucher?)null);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, BuildUpdateRequest(name: "New Name"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal("Voucher with ID '1' was not found.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID09 – Abnormal: Staff tries to delete Active voucher → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID09_StaffDeleteActive_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Staff");
        var request = BuildUpdateRequest(isDeleted: true);
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Active", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Cannot delete an Active voucher.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID10 – Abnormal: Try to update core financial fields of a used voucher → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID10_UpdateFinancialFieldOfUsedVoucher_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(target: "ORDER_TOTAL"); // Changing DiscountTarget
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Active", EndDate = _now.AddDays(10), UsedQuantity = 1, DiscountType = "PERCENTAGE", DiscountTarget = "SHIPPING_FEE" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Cannot modify core financial fields", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID11 – Abnormal: Try to update total quantity below used quantity → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID11_QuantityBelowUsedQuantity_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(qty: 5); // qty = 5, which is < usedQty (10)
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Active", EndDate = _now.AddDays(10), UsedQuantity = 10, DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Total quantity cannot be set below the used quantity (10).", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID12 – Abnormal: Try to update non-locked fields of an expired voucher → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID12_UpdateLockedFieldsOnExpired_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(name: "New Name Only"); // modifying VoucherName
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Expired", EndDate = _now.AddDays(-2), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Expired vouchers are locked. You can only update the End Date, Status, or delete it to reactivate/archive.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID13 – Abnormal: Admin rejects voucher without reason → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID13_AdminRejectNoReason_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Rejected", reason: null);
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending", EndDate = _now.AddDays(10), Reason = null, DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Reason is required when rejecting a voucher.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID14 – Abnormal: Admin approves an expired voucher → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID14_AdminApproveExpiredVoucher_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Scheduled", end: _now.AddDays(-1));
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Pending", EndDate = _now.AddDays(-1), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Cannot approve a voucher that has already expired. Please reject it or ask staff to update the dates.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID15 – Abnormal: Reactivate an expired inactive voucher without changing date → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID15_ReactivateExpiredInactive_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Active", end: _now.AddDays(-1));
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Inactive", EndDate = _now.AddDays(-1), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Cannot reactivate a voucher that has already expired. Please update the End Date to a future time.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID16 – Abnormal: Reactivate an inactive voucher that reached usage limit → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID16_ReactivateUsedOutInactive_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(status: "Active");
        SetupUpdateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Inactive", EndDate = _now.AddDays(10), TotalQuantity = 10, UsedQuantity = 10, DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Cannot reactivate this voucher because it has reached its usage limit (10/10).", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID17 – Abnormal: Voucher code already exists on another voucher → Conflict
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID17_DuplicateCodeConflict_ReturnsConflict()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(code: "EXISTINGCODE");
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("EXISTINGCODE", 1, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
        Assert.Equal("Voucher code already exists.", result.ErrorMessage);
    }

    /// <summary>
    /// UTCID18 – Abnormal: Discount value must be less than or equal to 100 for percentage vouchers → ValidationError
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID18_DiscountValuePercentageExceedsMax_ReturnsValidationError()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(target: "SHIPPING_FEE");
        SetupUpdateValidatorPass();
        
        var validationFailResult = new ValidationResult(new[] { new ValidationFailure("DiscountValue", "Discount value must be less than or equal to 100 for percentage vouchers.") });
        _createValidator.ValidateAsync(Arg.Any<ValidationContext<CreateVoucherDto>>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(validationFailResult));

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL", DiscountValue = 105 };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.UpdateVoucherAsync(1, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.True(result.ValidationErrors.ContainsKey("DiscountValue"));
        Assert.Contains("Discount value must be less than or equal to 100 for percentage vouchers.", result.ValidationErrors["DiscountValue"]);
    }

    /// <summary>
    /// UTCID19 – Abnormal: Database exception during transaction → Throws Exception
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task UpdateVoucherAsync_UTCID19_DatabaseException_ThrowsException()
    {
        // Arrange
        _currentUserService.RoleName.Returns("Admin");
        var request = BuildUpdateRequest(name: "New Name");
        SetupUpdateValidatorPass();
        SetupCreateValidatorPass();

        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10", Status = "Scheduled", EndDate = _now.AddDays(10), DiscountType = "PERCENTAGE", DiscountTarget = "ORDER_TOTAL" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _voucherRepo.ExistsVoucherCodeAsync("SAVE10", 1, Arg.Any<CancellationToken>()).Returns(false);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                   .Returns<Task<int>>(_ => throw new Exception("Database lost connection."));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.UpdateVoucherAsync(1, request));
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Function4 – GetVoucherByIdAsync
    // ═══════════════════════════════════════════════════════════════════════════
    #region Function4 – GetVoucherByIdAsync

    /// <summary>
    /// UTCID01 – Normal: existing id → Success
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVoucherByIdAsync_UTCID01_NormalExisting_ReturnsSuccess()
    {
        // Arrange
        var existing = new Voucher { VoucherId = 1, VoucherCode = "SAVE10" };
        _voucherRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Voucher?>(existing));

        // Act
        var result = await _sut.GetVoucherByIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("SAVE10", result.Data.VoucherCode);
    }

    /// <summary>
    /// UTCID02 – Boundary: voucherId = 0 → VALIDATION_ERROR: Voucher ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVoucherByIdAsync_UTCID02_BoundaryIdZero_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetVoucherByIdAsync(0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID03 – Abnormal: voucherId = -1 → VALIDATION_ERROR: Voucher ID must be greater than 0.
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVoucherByIdAsync_UTCID03_AbnormalIdNegative_ReturnsValidationError()
    {
        // Act
        var result = await _sut.GetVoucherByIdAsync(-1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
    }

    /// <summary>
    /// UTCID04 – Abnormal: non-existing id → NOT_FOUND
    /// Passed/Failed: [to be filled]
    /// Executed Date: [to be filled]
    /// Defect ID: [to be filled]
    /// </summary>
    [Fact]
    public async Task GetVoucherByIdAsync_UTCID04_AbnormalNotFound_ReturnsNotFound()
    {
        // Arrange
        _voucherRepo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Voucher?>(null));

        // Act
        var result = await _sut.GetVoucherByIdAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal("Voucher with ID '999' was not found.", result.ErrorMessage);
    }

    #endregion
}
