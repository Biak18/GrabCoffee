using GrabCoffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrabCoffee.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("received");
        builder.Property(x => x.Fulfillment).HasColumnName("fulfillment").HasDefaultValue("pickup");
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").IsRequired();
        builder.Property(x => x.Tax).HasColumnName("tax").IsRequired();
        builder.Property(x => x.Total).HasColumnName("total").IsRequired();
        builder.Property(x => x.PlacedAt).HasColumnName("placed_at");
        builder.Property(x => x.ReadyAt).HasColumnName("ready_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasDefaultValue("cash");
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasDefaultValue("unpaid");
        builder.Property(x => x.PaymentRef).HasColumnName("payment_ref");
        builder.Property(x => x.PaidAt).HasColumnName("paid_at");
        builder.Property(x => x.Tip).HasColumnName("tip").HasDefaultValue(0m);
        builder.Property(x => x.PromoCode).HasColumnName("promo_code");
        builder.Property(x => x.Discount).HasColumnName("discount").HasDefaultValue(0m);
        builder.Property(x => x.DeliveryFee).HasColumnName("delivery_fee").HasDefaultValue(0m);
        builder.Property(x => x.DeliveryAddress).HasColumnName("delivery_address");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.DriverId).HasColumnName("driver_id");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(x => x.DeliveryLat).HasColumnName("delivery_lat");
        builder.Property(x => x.DeliveryLng).HasColumnName("delivery_lng");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.DriverId);
        // Mirrors orders_user_idempotency_key_idx (partial unique).
        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"idempotency_key\" IS NOT NULL");
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.CoffeeId).HasColumnName("coffee_id");
        builder.Property(x => x.Size).HasColumnName("size");
        builder.Property(x => x.Temperature).HasColumnName("temperature");
        builder.Property(x => x.Milk).HasColumnName("milk");
        builder.Property(x => x.Extras).HasColumnName("extras").HasColumnType("text[]");
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasDefaultValue(1);
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.CompareAtPrice).HasColumnName("compare_at_price");
        builder.HasIndex(x => x.OrderId);
    }
}

public sealed class CoffeeReviewConfiguration : IEntityTypeConfiguration<CoffeeReview>
{
    public void Configure(EntityTypeBuilder<CoffeeReview> builder)
    {
        builder.ToTable("coffee_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.CoffeeId).HasColumnName("coffee_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.Rating).HasColumnName("rating").IsRequired();
        builder.Property(x => x.Comment).HasColumnName("comment");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => x.CoffeeId);
    }
}

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("favorites");
        builder.HasKey(x => new { x.UserId, x.CoffeeId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.CoffeeId).HasColumnName("coffee_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.SenderId).HasColumnName("sender_id").IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => x.OrderId);
    }
}
