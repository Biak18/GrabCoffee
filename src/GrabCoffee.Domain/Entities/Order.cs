namespace GrabCoffee.Domain.Entities;

// Mirrors public.orders.
// Status: received | preparing | ready | driver_assigned | out_for_delivery
//   | delivered | completed | cancelled.
// Fulfillment: pickup | delivery. PaymentMethod: cash | kpay | mmqr.
// PaymentStatus: unpaid | awaiting_verification | verified.
public sealed class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public string Status { get; set; } = "received";
    public string Fulfillment { get; set; } = "pickup";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime? PlacedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string PaymentStatus { get; set; } = "unpaid";
    public string? PaymentRef { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Tip { get; set; }
    public string? PromoCode { get; set; }
    public decimal Discount { get; set; }
    public decimal DeliveryFee { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public double? DeliveryLat { get; set; }
    public double? DeliveryLng { get; set; }
}
