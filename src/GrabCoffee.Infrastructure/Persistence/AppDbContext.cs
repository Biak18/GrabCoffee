using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Orders;
using GrabCoffee.Application.Features.Stores.GetMyEarnings;
using GrabCoffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrabCoffee.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Coffee> Coffees => Set<Coffee>();
    public DbSet<CoffeeOption> CoffeeOptions => Set<CoffeeOption>();
    public DbSet<CoffeeOptionCategory> CoffeeOptionCategories => Set<CoffeeOptionCategory>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<CoffeeReview> CoffeeReviews => Set<CoffeeReview>();
    public DbSet<LoyaltyCard> LoyaltyCards => Set<LoyaltyCard>();
    public DbSet<LoyaltyEvent> LoyaltyEvents => Set<LoyaltyEvent>();
    public DbSet<StoreFavorite> StoreFavorites => Set<StoreFavorite>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // ---- IAppDbContext ----
    public Task<bool> IsAdminAsync(Guid userId, CancellationToken ct)
        => Profiles.AsNoTracking()
            .AnyAsync(p => p.Id == userId && (p.Role == "seller" || p.Role == "admin"), ct);

    public Task<Profile?> FindProfileAsync(Guid id, CancellationToken ct)
        => Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    Task<int> IAppDbContext.SaveChangesAsync(CancellationToken ct) => base.SaveChangesAsync(ct);

    // ---- Stores ----
    public Task<List<Store>> GetStoresAsync(int page, int pageSize, CancellationToken ct)
        => Stores.AsNoTracking()
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountStoresAsync(CancellationToken ct)
        => Stores.CountAsync(ct);

    public Task<Store?> FindStoreAsync(Guid id, CancellationToken ct)
        => Stores.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Store?> FindStoreByOwnerAsync(Guid ownerId, CancellationToken ct)
        => Stores.FirstOrDefaultAsync(s => s.OwnerId == ownerId, ct);

    public Task AddStoreAsync(Store store, CancellationToken ct)
        => Stores.AddAsync(store, ct).AsTask();

    // Mirrors become_seller(): rejects sellers, inserts the store, flips
    // profiles.role. The prevent_direct_role_change trigger requires
    // SET LOCAL app.allow_role_change inside the same transaction.
    public async Task<Guid> OnboardSellerAsync(Guid userId, string name, string address, string? hoursJson, CancellationToken ct)
    {
        var profile = await Profiles.FirstOrDefaultAsync(p => p.Id == userId, ct)
            ?? throw new NotFoundException("Profile not found.");
        if (string.Equals(profile.Role, "seller", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("You already have a store.");

        await using var tx = await Database.BeginTransactionAsync(ct);
        await Database.ExecuteSqlRawAsync("SET LOCAL app.allow_role_change = 'true'", ct);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            HoursJson = hoursJson,
            OwnerId = userId,
        };
        await Stores.AddAsync(store, ct);
        profile.Role = "seller";
        await base.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return store.Id;
    }

    // ---- Menu ----
    public Task<List<Category>> GetCategoriesByStoreAsync(Guid storeId, CancellationToken ct)
        => Categories.AsNoTracking()
            .Where(c => c.StoreId == storeId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public Task<Category?> FindCategoryAsync(Guid id, CancellationToken ct)
        => Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task AddCategoryAsync(Category category, CancellationToken ct)
        => Categories.AddAsync(category, ct).AsTask();

    private static IQueryable<Coffee> FilterCoffees(
        IQueryable<Coffee> query,
        Guid? storeId,
        Guid? categoryId,
        string? search,
        bool onlyActive)
    {
        if (onlyActive)
            query = query.Where(c => c.IsActive);
        if (storeId.HasValue)
            query = query.Where(c => c.StoreId == storeId.Value);
        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{search}%") ||
                (c.Description != null && EF.Functions.ILike(c.Description, $"%{search}%")));
        return query;
    }

    private static IQueryable<Coffee> SortCoffees(IQueryable<Coffee> query, string sort)
        => sort.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(c => c.BasePrice),
            "price_desc" => query.OrderByDescending(c => c.BasePrice),
            "name" => query.OrderBy(c => c.Name),
            _ => query.OrderByDescending(c => c.Rating),
        };

    public async Task<List<Coffee>> GetCoffeesAsync(Guid? storeId, Guid? categoryId, string? search, string sort, int page, int pageSize, bool onlyActive, CancellationToken ct)
        => await SortCoffees(FilterCoffees(Coffees.AsNoTracking(), storeId, categoryId, search, onlyActive), sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountCoffeesAsync(Guid? storeId, Guid? categoryId, string? search, bool onlyActive, CancellationToken ct)
        => FilterCoffees(Coffees.AsNoTracking(), storeId, categoryId, search, onlyActive).CountAsync(ct);

    public Task<Coffee?> FindCoffeeAsync(Guid id, CancellationToken ct)
        => Coffees.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Coffee>> GetCoffeesByStoreAsync(Guid storeId, CancellationToken ct)
        => Coffees.AsNoTracking()
            .Where(c => c.StoreId == storeId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public Task AddCoffeeAsync(Coffee coffee, CancellationToken ct)
        => Coffees.AddAsync(coffee, ct).AsTask();

    public Task<List<Coffee>> GetFeaturedCoffeesAsync(CancellationToken ct)
        => Coffees.AsNoTracking()
            .Where(c => c.IsActive && c.IsFeatured)
            .OrderByDescending(c => c.Rating)
            .Take(10)
            .ToListAsync(ct);

    public Task<List<Coffee>> GetPopularCoffeesAsync(CancellationToken ct)
        => Coffees.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Rating)
            .Take(10)
            .ToListAsync(ct);

    public Task<List<Coffee>> GetRecommendedCoffeesAsync(CancellationToken ct)
        => Coffees.AsNoTracking()
            .Where(c => c.IsActive && !c.IsFeatured)
            .OrderByDescending(c => c.Rating)
            .Take(10)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, string>> GetStoreNamesAsync(IEnumerable<Guid> storeIds, CancellationToken ct)
        => await Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

    // Mirrors get_coffee_options(): options of the category's store that are
    // either unscoped or explicitly scoped to this category.
    public async Task<List<CoffeeOption>> GetOptionsForCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var category = await Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct)
            ?? throw new NotFoundException("Category not found.");

        return await CoffeeOptions.AsNoTracking()
            .Where(o => o.StoreId == category.StoreId
                && (!CoffeeOptionCategories.Any(occ => occ.OptionId == o.Id)
                    || CoffeeOptionCategories.Any(occ => occ.OptionId == o.Id && occ.CategoryId == categoryId)))
            .OrderBy(o => o.Type)
            .ThenBy(o => o.Label)
            .ToListAsync(ct);
    }

    public Task<List<Coffee>> SearchCoffeesAsync(string term, CancellationToken ct)
        => Coffees.AsNoTracking()
            .Where(c => c.IsActive
                && (EF.Functions.ILike(c.Name, $"%{term}%")
                    || (c.Description != null && EF.Functions.ILike(c.Description, $"%{term}%"))))
            .OrderByDescending(c => c.Rating)
            .Take(20)
            .ToListAsync(ct);

    public Task<List<Store>> SearchStoresAsync(string term, CancellationToken ct)
        => Stores.AsNoTracking()
            .Where(s => EF.Functions.ILike(s.Name, $"%{term}%")
                || EF.Functions.ILike(s.Address, $"%{term}%"))
            .OrderBy(s => s.Name)
            .Take(10)
            .ToListAsync(ct);

    public Task<List<CoffeeOption>> GetOptionsByStoreAsync(Guid storeId, CancellationToken ct)
        => CoffeeOptions.AsNoTracking()
            .Where(o => o.StoreId == storeId)
            .OrderBy(o => o.Type)
            .ThenBy(o => o.Label)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, List<Guid>>> GetOptionCategoryMapAsync(Guid storeId, CancellationToken ct)
    {
        var optionIds = await CoffeeOptions.AsNoTracking()
            .Where(o => o.StoreId == storeId)
            .Select(o => o.Id)
            .ToListAsync(ct);
        var rows = await CoffeeOptionCategories.AsNoTracking()
            .Where(occ => optionIds.Contains(occ.OptionId))
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.OptionId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.CategoryId).ToList());
    }

    public Task<CoffeeOption?> FindOptionAsync(Guid id, CancellationToken ct)
        => CoffeeOptions.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task AddOptionAsync(CoffeeOption option, CancellationToken ct)
        => CoffeeOptions.AddAsync(option, ct).AsTask();

    public async Task RemoveOptionAsync(CoffeeOption option, CancellationToken ct)
    {
        var scopings = await CoffeeOptionCategories
            .Where(occ => occ.OptionId == option.Id)
            .ToListAsync(ct);
        CoffeeOptionCategories.RemoveRange(scopings);
        CoffeeOptions.Remove(option);
    }

    // Mirrors set_option_category_scoping(): replace-all; empty = unscoped.
    public async Task SetOptionScopingAsync(Guid optionId, List<Guid> categoryIds, CancellationToken ct)
    {
        var existing = await CoffeeOptionCategories
            .Where(occ => occ.OptionId == optionId)
            .ToListAsync(ct);
        CoffeeOptionCategories.RemoveRange(existing);
        foreach (var categoryId in categoryIds.Distinct())
            await CoffeeOptionCategories.AddAsync(
                new CoffeeOptionCategory { OptionId = optionId, CategoryId = categoryId }, ct);
    }

    // ---- Orders ----
    public Task<List<Coffee>> FindActiveCoffeesAsync(Guid storeId, IEnumerable<Guid> coffeeIds, CancellationToken ct)
        => Coffees.AsNoTracking()
            .Where(c => c.StoreId == storeId && c.IsActive && coffeeIds.Contains(c.Id))
            .ToListAsync(ct);

    public Task<List<CoffeeOption>> GetStoreOptionsAsync(Guid storeId, CancellationToken ct)
        => CoffeeOptions.AsNoTracking()
            .Where(o => o.StoreId == storeId)
            .ToListAsync(ct);

    public Task<LoyaltyCard?> FindLoyaltyCardAsync(Guid userId, Guid storeId, CancellationToken ct)
        => LoyaltyCards.FirstOrDefaultAsync(c => c.UserId == userId && c.StoreId == storeId, ct);

    // Race-safe redeem: the row updates only when stamps >= 10 still holds.
    public async Task<bool> TryRedeemLoyaltyAsync(Guid userId, Guid storeId, DateTime utcNow, CancellationToken ct)
        => await LoyaltyCards
            .Where(c => c.UserId == userId && c.StoreId == storeId && c.Stamps >= 10)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Stamps, c => c.Stamps - 10)
                    .SetProperty(c => c.UpdatedAt, utcNow),
                ct) == 1;

    public Task<Order?> FindOrderAsync(Guid id, CancellationToken ct)
        => Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> FindOrderByIdempotencyAsync(Guid userId, string key, CancellationToken ct)
        => Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.IdempotencyKey == key)
            .Select(o => new Order { Id = o.Id })
            .FirstOrDefaultAsync(ct);

    // Atomic place-order persist: loyalty decrement (race-safe conditional
    // update) + order/items insert in one transaction. Duplicate idempotency
    // keys resolve to the existing order id (mirrors the RPC's
    // unique_violation fallback on orders_user_idempotency_key_idx).
    public async Task<Guid> InsertOrderAsync(Order order, List<OrderItem> items, bool redeemLoyalty, CancellationToken ct)
    {
        await using var tx = await Database.BeginTransactionAsync(ct);
        await Database.ExecuteSqlRawAsync("SET LOCAL app.backend_write = 'true'", ct);

        if (redeemLoyalty)
        {
            var redeemed = await TryRedeemLoyaltyAsync(order.UserId, order.StoreId, DateTime.UtcNow, ct);
            if (!redeemed)
                throw new InvalidOperationException("Loyalty reward is no longer available.");
        }

        await Orders.AddAsync(order, ct);
        await OrderItems.AddRangeAsync(items, ct);
        try
        {
            await base.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (order.IdempotencyKey is not null)
            {
                var existingId = await Orders.AsNoTracking()
                    .Where(o => o.UserId == order.UserId && o.IdempotencyKey == order.IdempotencyKey)
                    .Select(o => o.Id)
                    .FirstOrDefaultAsync(ct);
                if (existingId != Guid.Empty)
                {
                    await tx.RollbackAsync(ct);
                    return existingId;
                }
            }
            throw;
        }

        await tx.CommitAsync(ct);
        return order.Id;
    }

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid id, CancellationToken ct)
    {
        var order = await Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            return null;

        var items = await (
            from oi in OrderItems.AsNoTracking()
            join c in Coffees.AsNoTracking() on oi.CoffeeId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            where oi.OrderId == id
            orderby oi.CreatedAt
            select new OrderItemDto(
                oi.Id, oi.CoffeeId,
                c != null ? c.Name : null,
                c != null ? c.ImageUrl : null,
                oi.Size, oi.Temperature, oi.Milk, oi.Extras,
                oi.Quantity, oi.UnitPrice, oi.CompareAtPrice))
            .ToListAsync(ct);

        string? driverName = null;
        string? driverPhone = null;
        if (order.DriverId.HasValue)
        {
            var driver = await Drivers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == order.DriverId.Value, ct);
            driverName = driver?.FullName;
            driverPhone = driver?.Phone;
        }

        return new OrderDetailsDto(
            order.Id, order.UserId, order.StoreId, order.Status, order.Fulfillment,
            order.Subtotal, order.Tax, order.Total, order.Discount, order.Tip,
            order.PromoCode, order.DeliveryFee, order.DeliveryAddress,
            order.DeliveryLat, order.DeliveryLng,
            order.PlacedAt, order.ReadyAt, order.CompletedAt, order.CancelledAt,
            order.PaymentMethod, order.PaymentStatus, order.PaymentRef,
            order.DriverId, driverName, driverPhone, items);
    }

    private static IQueryable<Order> FilterOrderSummaries(
        IQueryable<Order> query,
        Guid? userId,
        Guid? storeId,
        Guid? driverId,
        string[]? statuses)
    {
        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);
        if (storeId.HasValue)
            query = query.Where(o => o.StoreId == storeId.Value);
        if (driverId.HasValue)
            query = query.Where(o => o.DriverId == driverId.Value);
        if (statuses is { Length: > 0 })
            query = query.Where(o => statuses.Contains(o.Status));
        return query;
    }

    public async Task<List<OrderSummaryDto>> GetOrderSummariesAsync(
        Guid? userId, Guid? storeId, Guid? driverId, string[]? statuses,
        int page, int pageSize, CancellationToken ct)
    {
        var orders = await FilterOrderSummaries(Orders.AsNoTracking(), userId, storeId, driverId, statuses)
            .OrderByDescending(o => o.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        if (orders.Count == 0)
            return [];

        var orderIds = orders.Select(o => o.Id).ToList();
        var thumbs = await (
            from oi in OrderItems.AsNoTracking()
            join c in Coffees.AsNoTracking() on oi.CoffeeId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            where orderIds.Contains(oi.OrderId!.Value)
            group new { oi, ImageUrl = c != null ? c.ImageUrl : null } by oi.OrderId!.Value into g
            select new
            {
                OrderId = g.Key,
                Count = g.Count(),
                Thumbnail = g.OrderBy(x => x.oi.CreatedAt).Select(x => x.ImageUrl).FirstOrDefault(),
            })
            .ToListAsync(ct);
        var byOrder = thumbs.ToDictionary(t => t.OrderId);

        return orders.Select(o => new OrderSummaryDto(
            o.Id, o.Status, o.Total, o.PlacedAt,
            byOrder.TryGetValue(o.Id, out var t) ? t.Count : 0,
            byOrder.TryGetValue(o.Id, out var th) ? th.Thumbnail : null)).ToList();
    }

    public Task<int> CountOrderSummariesAsync(
        Guid? userId, Guid? storeId, Guid? driverId, string[]? statuses, CancellationToken ct)
        => FilterOrderSummaries(Orders.AsNoTracking(), userId, storeId, driverId, statuses).CountAsync(ct);

    public Task<Driver?> FindDriverAsync(Guid id, CancellationToken ct)
        => Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    // Status-changing writes run under the backend-write flag so the
    // prevent_customer_status_change trigger (which keys off auth.uid(),
    // always null on this connection) lets them through. Rules themselves
    // are enforced in code before calling this.
    public async Task ExecuteInOrderWriteTxAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await using var tx = await Database.BeginTransactionAsync(ct);
        await Database.ExecuteSqlRawAsync("SET LOCAL app.backend_write = 'true'", ct);
        await action(ct);
        await base.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ---- Account & social ----
    public Task<List<LoyaltyCard>> GetLoyaltyCardsAsync(Guid userId, CancellationToken ct)
        => LoyaltyCards.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public Task<List<Address>> GetAddressesAsync(Guid userId, CancellationToken ct)
        => Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public Task<Address?> FindAddressAsync(Guid id, CancellationToken ct)
        => Addresses.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task AddAddressAsync(Address address, CancellationToken ct)
        => Addresses.AddAsync(address, ct).AsTask();

    public Task RemoveAddressAsync(Address address, CancellationToken ct)
    {
        Addresses.Remove(address);
        return Task.CompletedTask;
    }

    // Mirrors set_default_address(): ownership check + atomic flip.
    public async Task SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        await using var tx = await Database.BeginTransactionAsync(ct);
        var address = await Addresses.FirstOrDefaultAsync(a => a.Id == addressId, ct);
        if (address is null || address.UserId != userId)
            throw new NotFoundException("Address not found.");

        await Addresses
            .Where(a => a.UserId == userId && a.Id != addressId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.IsDefault, false), ct);
        address.IsDefault = true;
        await base.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task<List<Guid>> GetFavoriteCoffeeIdsAsync(Guid userId, CancellationToken ct)
        => Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.CoffeeId)
            .ToListAsync(ct);

    public async Task<List<Coffee>> GetFavoriteCoffeesAsync(Guid userId, CancellationToken ct)
    {
        var ids = await GetFavoriteCoffeeIdsAsync(userId, ct);
        if (ids.Count == 0)
            return [];
        return await Coffees.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
    }

    public async Task AddFavoriteAsync(Guid userId, Guid coffeeId, CancellationToken ct)
    {
        var exists = await Favorites.AnyAsync(f => f.UserId == userId && f.CoffeeId == coffeeId, ct);
        if (!exists)
            await Favorites.AddAsync(new Favorite { UserId = userId, CoffeeId = coffeeId, CreatedAt = DateTime.UtcNow }, ct);
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid coffeeId, CancellationToken ct)
    {
        var favorite = await Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.CoffeeId == coffeeId, ct);
        if (favorite is not null)
            Favorites.Remove(favorite);
    }

    public Task<List<Guid>> GetFavoriteStoreIdsAsync(Guid userId, CancellationToken ct)
        => StoreFavorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.StoreId)
            .ToListAsync(ct);

    public async Task<List<Store>> GetFavoriteStoresAsync(Guid userId, CancellationToken ct)
    {
        var ids = await StoreFavorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.StoreId)
            .ToListAsync(ct);
        if (ids.Count == 0)
            return [];
        return await Stores.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);
    }

    public async Task AddStoreFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct)
    {
        var exists = await StoreFavorites.AnyAsync(f => f.UserId == userId && f.StoreId == storeId, ct);
        if (!exists)
            await StoreFavorites.AddAsync(
                new StoreFavorite { UserId = userId, StoreId = storeId, CreatedAt = DateTime.UtcNow }, ct);
    }

    public async Task RemoveStoreFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct)
    {
        var favorite = await StoreFavorites.FirstOrDefaultAsync(f => f.UserId == userId && f.StoreId == storeId, ct);
        if (favorite is not null)
            StoreFavorites.Remove(favorite);
    }

    public Task<List<CoffeeReview>> GetCoffeeReviewsAsync(Guid coffeeId, int limit, CancellationToken ct)
        => CoffeeReviews.AsNoTracking()
            .Where(r => r.CoffeeId == coffeeId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<List<Guid>> GetReviewedCoffeeIdsAsync(Guid orderId, CancellationToken ct)
        => CoffeeReviews.AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .Select(r => r.CoffeeId)
            .ToListAsync(ct);

    public Task<bool> OrderContainsCoffeeAsync(Guid orderId, Guid coffeeId, CancellationToken ct)
        => OrderItems.AsNoTracking()
            .AnyAsync(oi => oi.OrderId == orderId && oi.CoffeeId == coffeeId, ct);

    public Task AddReviewAsync(CoffeeReview review, CancellationToken ct)
        => CoffeeReviews.AddAsync(review, ct).AsTask();

    public async Task<Dictionary<Guid, string?>> GetProfileNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
        => await Profiles.AsNoTracking()
            .Where(p => userIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

    public Task<List<ChatMessage>> GetOrderMessagesAsync(Guid orderId, int limit, DateTime? before, CancellationToken ct)
    {
        var query = ChatMessages.AsNoTracking()
            .Where(m => m.OrderId == orderId);
        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value);
        return query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task AddChatMessageAsync(ChatMessage message, CancellationToken ct)
        => ChatMessages.AddAsync(message, ct).AsTask();

    // Mirrors register_driver(): upsert driver row (nulls keep old values,
    // always available) + unconditional role flip through the role lock.
    public async Task RegisterDriverAsync(Guid userId, string? fullName, string? phone, string? vehicle, CancellationToken ct)
    {
        await using var tx = await Database.BeginTransactionAsync(ct);
        await Database.ExecuteSqlRawAsync("SET LOCAL app.allow_role_change = 'true'", ct);

        var driver = await Drivers.FirstOrDefaultAsync(d => d.Id == userId, ct);
        if (driver is null)
        {
            await Drivers.AddAsync(new Driver
            {
                Id = userId,
                FullName = fullName,
                Phone = phone,
                Vehicle = vehicle,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
            }, ct);
        }
        else
        {
            if (fullName is not null)
                driver.FullName = fullName;
            if (phone is not null)
                driver.Phone = phone;
            if (vehicle is not null)
                driver.Vehicle = vehicle;
            driver.IsAvailable = true;
        }

        var profile = await Profiles.FirstOrDefaultAsync(p => p.Id == userId, ct)
            ?? throw new NotFoundException("Profile not found.");
        profile.Role = "driver";

        await base.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task SetDriverAvailabilityAsync(Guid userId, bool isAvailable, CancellationToken ct)
    {
        var updated = await Drivers
            .Where(d => d.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(d => d.IsAvailable, isAvailable), ct);
        if (updated == 0)
            throw new NotFoundException("Driver profile not found.");
    }

    public Task<List<Driver>> GetAvailableDriversAsync(CancellationToken ct)
        => Drivers.AsNoTracking()
            .Where(d => d.IsAvailable)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task UpdateProfileAsync(Guid userId, string? fullName, string? avatarUrl, CancellationToken ct)
    {
        var profile = await Profiles.FirstOrDefaultAsync(p => p.Id == userId, ct)
            ?? throw new NotFoundException("Profile not found.");
        if (fullName is not null)
            profile.FullName = fullName;
        if (avatarUrl is not null)
            profile.AvatarUrl = avatarUrl;
        await base.SaveChangesAsync(ct);
    }

    // Mirrors delete_account(): sellers with a store are refused; wipes the
    // same rows in the same order, then drops the auth user (profiles and
    // other dependents follow DB cascades exactly as with the RPC).
    public async Task DeleteAccountAsync(Guid userId, CancellationToken ct)
    {
        await using var tx = await Database.BeginTransactionAsync(ct);

        if (await Stores.AnyAsync(s => s.OwnerId == userId, ct))
            throw new InvalidOperationException("Seller accounts with a store cannot self-delete. Contact support.");

        await Favorites.Where(f => f.UserId == userId).ExecuteDeleteAsync(ct);
        await CoffeeReviews.Where(r => r.UserId == userId).ExecuteDeleteAsync(ct);
        await Orders.Where(o => o.UserId == userId).ExecuteDeleteAsync(ct);
        await LoyaltyCards.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
        await Database.ExecuteSqlRawAsync("DELETE FROM auth.users WHERE id = {0}", [userId], ct);

        await tx.CommitAsync(ct);
    }

    public async Task UpsertPushTokenAsync(Guid userId, string token, CancellationToken ct)
    {
        var existing = await PushTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
        if (existing is null)
        {
            await PushTokens.AddAsync(new PushToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
            }, ct);
        }
        else if (existing.UserId != userId)
        {
            existing.UserId = userId;
        }
        await base.SaveChangesAsync(ct);
    }

    public async Task DeleteMyPushTokensAsync(Guid userId, CancellationToken ct)
        => await PushTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);

    // Narrow columns only (~30 days), mirroring the mobile bound so the
    // payload stays constant-size as order history grows.
    public Task<List<SellerEarningRow>> GetSellerEarningRowsAsync(Guid storeId, DateTime sinceUtc, CancellationToken ct)
        => Orders.AsNoTracking()
            .Where(o => o.StoreId == storeId && o.PlacedAt != null && o.PlacedAt >= sinceUtc)
            .Select(o => new SellerEarningRow(o.Status, o.Total, o.PlacedAt))
            .ToListAsync(ct);

    // ---- Promotions ----
    // Mirrors fetchActivePromotions: only CODELESS promos auto-apply.
    // Voucher-code promos are redeemed explicitly via lookup (never double-count).
    public Task<List<Promotion>> GetActivePromotionsAsync(Guid? storeId, DateOnly today, CancellationToken ct)
    {
        var query = Promotions.AsNoTracking()
            .Where(p => p.Code == null
                && p.IsActive
                && p.StartsAt <= today
                && p.EndsAt >= today);
        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId.Value);
        return query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<Promotion?> LookupPromoCodeAsync(Guid storeId, string code, DateOnly today, CancellationToken ct)
        => Promotions.AsNoTracking()
            .Where(p => p.StoreId == storeId
                && p.IsActive
                && p.Code != null
                && EF.Functions.ILike(p.Code, code)
                && p.StartsAt <= today
                && p.EndsAt >= today)
            .FirstOrDefaultAsync(ct);

    public Task<List<Promotion>> GetPromotionsByStoreAsync(Guid storeId, CancellationToken ct)
        => Promotions.AsNoTracking()
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public Task<Promotion?> FindPromotionAsync(int id, CancellationToken ct)
        => Promotions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task AddPromotionAsync(Promotion promotion, CancellationToken ct)
        => Promotions.AddAsync(promotion, ct).AsTask();

    public Task RemovePromotionAsync(Promotion promotion, CancellationToken ct)
    {
        Promotions.Remove(promotion);
        return Task.CompletedTask;
    }
}
