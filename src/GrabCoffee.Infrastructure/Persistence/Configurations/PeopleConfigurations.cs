using GrabCoffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrabCoffee.Infrastructure.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Label).HasColumnName("label").HasDefaultValue("Home");
        builder.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").IsRequired();
        builder.Property(x => x.StreetAddress).HasColumnName("address").IsRequired();
        builder.Property(x => x.Lat).HasColumnName("lat");
        builder.Property(x => x.Lng).HasColumnName("lng");
        builder.Property(x => x.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => x.UserId);
    }
}

public sealed class PushTokenConfiguration : IEntityTypeConfiguration<PushToken>
{
    public void Configure(EntityTypeBuilder<PushToken> builder)
    {
        builder.ToTable("push_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Token).HasColumnName("token").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => x.Token).IsUnique();
    }
}

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FullName).HasColumnName("full_name");
        builder.Property(x => x.Phone).HasColumnName("phone");
        builder.Property(x => x.Vehicle).HasColumnName("vehicle");
        builder.Property(x => x.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
    }
}

public sealed class StoreFavoriteConfiguration : IEntityTypeConfiguration<StoreFavorite>
{
    public void Configure(EntityTypeBuilder<StoreFavorite> builder)
    {
        builder.ToTable("store_favorites");
        builder.HasKey(x => new { x.UserId, x.StoreId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.StoreId).HasColumnName("store_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
    }
}
