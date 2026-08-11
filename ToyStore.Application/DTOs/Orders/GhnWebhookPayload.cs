using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ToyStore.Application.DTOs.Orders;

public class GhnWebhookPayload
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("data")]
    public GhnWebhookPayloadData? Data { get; set; }

    [JsonPropertyName("OrderCode")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("ClientOrderCode")]
    public string? ClientOrderCode { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Time")]
    public DateTime? Time { get; set; }

    [JsonPropertyName("CODAmount")]
    public long? CODAmount { get; set; }

    [JsonPropertyName("ReasonCode")]
    public string? ReasonCode { get; set; }

    [JsonPropertyName("Reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("TotalFee")]
    public decimal? TotalFee { get; set; }

    [JsonPropertyName("Weight")]
    public int? Weight { get; set; }

    [JsonPropertyName("Length")]
    public int? Length { get; set; }

    [JsonPropertyName("Width")]
    public int? Width { get; set; }

    [JsonPropertyName("Height")]
    public int? Height { get; set; }

    [JsonPropertyName("PaymentType")]
    public int? PaymentType { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("image_pod")]
    public string? ImagePod { get; set; }

    [JsonPropertyName("pod")]
    public string? POD { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("image_urls")]
    public List<string>? ImageUrls { get; set; }

    public string? EffectiveOrderCode => Data?.OrderCode ?? OrderCode;
    public string? EffectiveClientOrderCode => Data?.ClientOrderCode ?? ClientOrderCode;
    public string? EffectiveStatus => Data?.Status ?? Status;
    public string? EffectiveReasonCode => Data?.ReasonCode ?? ReasonCode;
    public string? EffectiveReason => Data?.Reason ?? Reason;
    public long? EffectiveCODAmount => Data?.CODAmount ?? CODAmount;
    public decimal? EffectiveTotalFee => Data?.TotalFee ?? TotalFee;

    public string? GetDeliveryImageUrl()
    {
        var dataImage = Data?.GetDeliveryImageUrl();
        if (!string.IsNullOrWhiteSpace(dataImage)) return dataImage;

        if (!string.IsNullOrWhiteSpace(ImagePod)) return ImagePod.Trim();
        if (!string.IsNullOrWhiteSpace(Image)) return Image.Trim();
        if (!string.IsNullOrWhiteSpace(POD)) return POD.Trim();
        if (Images != null && Images.Count > 0 && !string.IsNullOrWhiteSpace(Images[0])) return Images[0].Trim();
        if (ImageUrls != null && ImageUrls.Count > 0 && !string.IsNullOrWhiteSpace(ImageUrls[0])) return ImageUrls[0].Trim();
        return null;
    }
}

public class GhnWebhookPayloadData
{
    [JsonPropertyName("OrderCode")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("ClientOrderCode")]
    public string? ClientOrderCode { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("StatusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("CODAmount")]
    public long? CODAmount { get; set; }

    [JsonPropertyName("ReasonCode")]
    public string? ReasonCode { get; set; }

    [JsonPropertyName("Reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("TotalFee")]
    public decimal? TotalFee { get; set; }

    [JsonPropertyName("image_pod")]
    public string? ImagePod { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("pod")]
    public string? POD { get; set; }

    public string? GetDeliveryImageUrl()
    {
        if (!string.IsNullOrWhiteSpace(ImagePod)) return ImagePod.Trim();
        if (!string.IsNullOrWhiteSpace(Image)) return Image.Trim();
        if (!string.IsNullOrWhiteSpace(POD)) return POD.Trim();
        return null;
    }
}
