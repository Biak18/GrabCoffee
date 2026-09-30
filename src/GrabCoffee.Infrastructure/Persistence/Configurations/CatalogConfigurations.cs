using GrabCoffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrabCoffee.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Address).HasColumnName("address").IsRequired();
        builder.Property(x => x.Lat).HasColumnName("lat");
        builder.Property(x => x.Lng).HasColumnName("lng");
        builder.Property(x => x.HoursJson).HasColumnName("hours").HasColumnType("jsonb");
        builder.Property(x => x.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(x => x.KpayPhone).HasColumnName("kpay_phone");
        builder.Property(x => x.PaymentNote).HasColumnName("payment_note");
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone");
        builder.HasIndex(x => x.OwnerId).IsUnique();
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
        builder.HasIndex(x => x.StoreId);
    }
}

public sealed class CoffeeConfiguration : IEntityTypeConfiguration<Coffee>
{
    public void Configure(EntityTypeBuilder<Coffee> builder)
    {
        builder.ToTable("coffees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.BasePrice).HasColumnName("base_price").IsRequired();
        builder.Property(x => x.ImageUrl).HasColumnName("image_url");
        builder.Property(x => x.Rating).HasColumnName("rating");
        builder.Property(x => x.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CategoryId);
    }
}

public sealed class CoffeeOptionConfiguration : IEntityTypeConfiguration<CoffeeOption>
{
    public void Configure(EntityTypeBuilder<CoffeeOption> builder)
    {
        builder.ToTable("coffee_options");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Type).HasColumnName("type").IsRequired();
        builder.Property(x => x.Label).HasColumnName("label").IsRequired();
        builder.Property(x => x.PriceDelta).HasColumnName("price_delta").HasDefaultValue(0m);
        builder.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
        builder.HasIndex(x => x.StoreId);
    }
}

public sealed class CoffeeOptionCategoryConfiguration : IEntityTypeConfiguration<CoffeeOptionCategory>
{
    public void Configure(EntityTypeBuilder<CoffeeOptionCategory> builder)
    {
        builder.ToTable("coffee_option_categories");
        builder.HasKey(x => new { x.OptionId, x.CategoryId });
        builder.Property(x => x.OptionId).HasColumnName("option_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
    }
}

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").IsRequired();
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").IsRequired();
        builder.Property(x => x.Scope).HasColumnName("scope").HasDefaultValue("all");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.CoffeeId).HasColumnName("coffee_id");
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code");
        builder.HasIndex(x => x.StoreId);
    }
}
