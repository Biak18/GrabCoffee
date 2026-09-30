using GrabCoffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrabCoffee.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyCardConfiguration : IEntityTypeConfiguration<LoyaltyCard>
{
    public void Configure(EntityTypeBuilder<LoyaltyCard> builder)
    {
        builder.ToTable("loyalty_cards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
        builder.Property(x => x.Stamps).HasColumnName("stamps").HasDefaultValue(0);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.UserId, x.StoreId });
    }
}

public sealed class LoyaltyEventConfiguration : IEntityTypeConfiguration<LoyaltyEvent>
{
    public void Configure(EntityTypeBuilder<LoyaltyEvent> builder)
    {
        builder.ToTable("loyalty_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.Kind).HasColumnName("kind").HasDefaultValue("earn");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => x.CardId);
    }
}
