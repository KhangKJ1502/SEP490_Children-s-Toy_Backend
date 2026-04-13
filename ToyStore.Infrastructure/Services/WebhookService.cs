using ToyStore.Application.Common.Exceptions;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Webhook service implementation with validation and error handling.
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderService _orderService;
    
    public WebhookService(IUnitOfWork unitOfWork, IOrderService orderService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }
    
    public async Task<WebhookResultDto> ProcessPaymentWebhookAsync(
        PaymentWebhookDto webhook,
        string? sourceIP = null,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (webhook == null)
            throw new ValidationException("Webhook", "Dữ liệu webhook không hợp lệ.");
            
        if (string.IsNullOrWhiteSpace(webhook.Provider))
            throw new ValidationException("Provider", "Nhà cung cấp thanh toán không hợp lệ.");
            
        if (string.IsNullOrWhiteSpace(webhook.OrderReference))
            throw new ValidationException("OrderReference", "Mã đơn hàng không hợp lệ.");
            
        if (string.IsNullOrWhiteSpace(webhook.Status))
            throw new ValidationException("Status", "Trạng thái thanh toán không hợp lệ.");
        
        // Validate provider
        var validProviders = new[] { "VNPay", "Momo", "ZaloPay", "BankTransfer" };
        if (!validProviders.Contains(webhook.Provider, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException("Provider", $"Nhà cung cấp '{webhook.Provider}' không được hỗ trợ.");
        
        // Log the webhook
        var webhookLog = new PaymentWebhook
        {
            Provider = webhook.Provider,
            TransactionId = webhook.TransactionId ?? "",
            EventType = $"payment.{webhook.Status.ToLower()}",
            Payload = webhook.RawPayload ?? "",
            Signature = webhook.Signature,
            SourceIP = sourceIP,
            ReceivedAt = DateTime.UtcNow
        };
        
        try
        {
            // Validate signature if provided
            if (!string.IsNullOrEmpty(webhook.Signature))
            {
                var isValid = await ValidateSignatureAsync(
                    webhook.Provider, 
                    webhook.RawPayload ?? "", 
                    webhook.Signature, 
                    cancellationToken);
                    
                if (!isValid)
                {
                    webhookLog.IsProcessed = false;
                    webhookLog.ProcessingResult = "Invalid signature";
                    await SaveWebhookLog(webhookLog, cancellationToken);
                    
                    throw new UnauthorizedException("Chữ ký webhook không hợp lệ.");
                }
            }
            
            // Find the order
            var order = await _unitOfWork.Orders.GetByOrderNumberAsync(
                webhook.OrderReference, cancellationToken);
                
            if (order == null)
            {
                webhookLog.IsProcessed = false;
                webhookLog.ProcessingResult = $"Order not found: {webhook.OrderReference}";
                await SaveWebhookLog(webhookLog, cancellationToken);
                
                throw new NotFoundException("Order", webhook.OrderReference);
            }
            
            webhookLog.OrderId = order.Id;
            
            // Check if already processed
            if (order.PaymentStatus == "Paid" && 
                webhook.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                webhookLog.IsProcessed = true;
                webhookLog.ProcessingResult = "Already processed";
                await SaveWebhookLog(webhookLog, cancellationToken);
                
                return new WebhookResultDto
                {
                    Success = true,
                    Message = "Đơn hàng đã được xử lý trước đó.",
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber
                };
            }
            
            // Process based on status
            bool processed = false;
            string resultMessage = "";
            
            switch (webhook.Status.ToLower())
            {
                case "success":
                case "paid":
                case "completed":
                    try
                    {
                        processed = await _orderService.ProcessPaymentSuccessAsync(
                            order.Id, webhook.TransactionId ?? "", cancellationToken);
                        resultMessage = processed 
                            ? "Thanh toán đã được xử lý thành công." 
                            : "Không thể xử lý thanh toán.";
                    }
                    catch (BusinessRuleException ex)
                    {
                        resultMessage = ex.Message;
                        processed = false;
                    }
                    break;
                    
                case "failed":
                case "cancelled":
                    order.PaymentStatus = "Failed";
                    order.Notes = (order.Notes ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Thanh toán thất bại: {webhook.Status}";
                    _unitOfWork.Orders.Update(order);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    processed = true;
                    resultMessage = "Thanh toán thất bại đã được ghi nhận.";
                    break;
                    
                case "pending":
                    // No action needed, already pending
                    processed = true;
                    resultMessage = "Trạng thái chờ thanh toán đã được ghi nhận.";
                    break;
                    
                default:
                    throw new BusinessRuleException(
                        "UNKNOWN_STATUS",
                        $"Trạng thái thanh toán '{webhook.Status}' không được hỗ trợ.");
            }
            
            webhookLog.IsProcessed = processed;
            webhookLog.ProcessingResult = resultMessage;
            webhookLog.ProcessedAt = DateTime.UtcNow;
            webhookLog.ProcessingAttempts = 1;
            await SaveWebhookLog(webhookLog, cancellationToken);
            
            return new WebhookResultDto
            {
                Success = processed,
                Message = resultMessage,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber
            };
        }
        catch (Exception ex) when (ex is not Application.Common.Exceptions.ApplicationException)
        {
            webhookLog.IsProcessed = false;
            webhookLog.ProcessingResult = $"Error: {ex.Message}";
            webhookLog.ProcessingAttempts++;
            await SaveWebhookLog(webhookLog, cancellationToken);
            
            throw;
        }
    }
    
    public Task<bool> ValidateSignatureAsync(
        string provider,
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(provider))
            throw new ValidationException("Provider", "Provider không được để trống.");
            
        if (string.IsNullOrEmpty(signature))
            throw new ValidationException("Signature", "Chữ ký không được để trống.");
        
        // In a real implementation, this would validate using the provider's secret key
        // TODO: Implement actual signature validation for each provider
        // - VNPay: SHA256 with secret key
        // - Momo: RSA or HMAC-SHA256
        // - ZaloPay: HMAC-SHA256
        
        return Task.FromResult(true);
    }
    
    private async Task SaveWebhookLog(PaymentWebhook webhookLog, CancellationToken cancellationToken)
    {
        // Store in database for audit trail
        // This would use a separate repository for PaymentWebhook
        await Task.CompletedTask;
    }
}
