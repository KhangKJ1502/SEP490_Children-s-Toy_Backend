using Microsoft.Extensions.Logging;
using Moq;
using ToyStore.Application.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Enums;
using XUnit;

namespace ToyStore.Application.Tests;

public class ShippingStatusMapperTests
{
    private readonly ShippingStatusMapper _mapper;
    private readonly Mock<ILogger<ShippingStatusMapper>> _loggerMock;

    public ShippingStatusMapperTests()
    {
        _loggerMock = new Mock<ILogger<ShippingStatusMapper>>();
        _mapper = new ShippingStatusMapper(_loggerMock.Object);
    }

    [Theory]
    [InlineData(ShippingStatuses.ReadyToPick, OrderStatus.Shipped)]
    [InlineData(ShippingStatuses.Picking, OrderStatus.Shipped)]
    [InlineData(ShippingStatuses.Picked, OrderStatus.Shipped)]
    [InlineData(ShippingStatuses.Storing, OrderStatus.Shipped)]
    [InlineData(ShippingStatuses.Sorting, OrderStatus.Shipped)]
    [InlineData(ShippingStatuses.Transporting, OrderStatus.Shipped)]
    [InlineData(ShippingStatuses.Delivering, OrderStatus.Delivering)]
    [InlineData(ShippingStatuses.MoneyCollectDelivering, OrderStatus.Delivering)]
    [InlineData(ShippingStatuses.Delivered, OrderStatus.Delivered)]
    [InlineData(ShippingStatuses.Cancel, OrderStatus.Cancelled)]
    [InlineData(ShippingStatuses.DeliveryFail, OrderStatus.Cancelled)]
    [InlineData(ShippingStatuses.Lost, OrderStatus.Cancelled)]
    [InlineData(ShippingStatuses.Damage, OrderStatus.Cancelled)]
    [InlineData(ShippingStatuses.Exception, OrderStatus.Cancelled)]
    [InlineData("return", OrderStatus.Cancelled)]
    [InlineData("return_transporting", OrderStatus.Cancelled)]
    public void MapToInternalStatus_ValidStatus_ReturnsCorrectInternalStatus(string providerStatus, OrderStatus expected)
    {
        // Act
        var result = _mapper.MapToInternalStatus(providerStatus);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToInternalStatus_UnknownStatus_ReturnsNullAndLogsWarning()
    {
        // Act
        var result = _mapper.MapToInternalStatus("unknown_status");

        // Assert
        Assert.Null(result);
        // Verify logger was called with Warning level
    }

    [Theory]
    [InlineData(ShippingStatuses.Delivering, OrderStatus.Pending, "Delivering")]
    [InlineData("unknown", OrderStatus.Shipped, "Shipped")]
    public void NormalizeStatus_ReturnsBusinessFriendlyStatus(string providerStatus, OrderStatus currentInternal, string expected)
    {
        // Act
        var result = _mapper.NormalizeStatus(providerStatus, currentInternal);

        // Assert
        Assert.Equal(expected, result);
    }
}
