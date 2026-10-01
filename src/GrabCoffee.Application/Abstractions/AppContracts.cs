using GrabCoffee.Application.Features.Orders;
using GrabCoffee.Application.Features.Stores.GetMyEarnings;
using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Abstractions;

public interface IAppDbContext
{
    Task<bool> IsAdminAsync(Guid userId, CancellationToken ct);
    Task<Profile?> FindProfileAsync(Guid id, CancellationToken ct);
    Task<List<Store>> GetStoresAsync(int page, int pageSize, CancellationToken ct);
    Task<int> CountStoresAsync(CancellationToken ct);
    Task<Store?> FindStoreAsync(Guid id, CancellationToken ct);
    Task<Store?> FindStoreByOwnerAsync(Guid ownerId, CancellationToken ct);
    Task AddStoreAsync(Store store, CancellationToken ct);
    Task<Guid> OnboardSellerAsync(Guid userId, string name, string address, string? hoursJson, CancellationToken ct);
    // ---- Menu (categories / coffees / options) ----
    Task<List<Category>> GetCategoriesByStoreAsync(Guid storeId, CancellationToken ct);
    Task<Category?> FindCategoryAsync(Guid id, CancellationToken ct);
    Task AddCategoryAsync(Category category, CancellationToken ct);
    Task<List<Coffee>> GetCoffeesAsync(Guid? storeId, Guid? categoryId, string? search, string sort, int page, int pageSize, bool onlyActive, CancellationToken ct);
    Task<int> CountCoffeesAsync(Guid? storeId, Guid? categoryId, string? search, bool onlyActive, CancellationToken ct);
    Task<Coffee?> FindCoffeeAsync(Guid id, CancellationToken ct);
    Task<List<Coffee>> GetCoffeesByStoreAsync(Guid storeId, CancellationToken ct);
    Task AddCoffeeAsync(Coffee coffee, CancellationToken ct);
    Task<List<Coffee>> GetFeaturedCoffeesAsync(CancellationToken ct);
    Task<List<Coffee>> GetPopularCoffeesAsync(CancellationToken ct);
    Task<List<Coffee>> GetRecommendedCoffeesAsync(CancellationToken ct);
    Task<Dictionary<Guid, string>> GetStoreNamesAsync(IEnumerable<Guid> storeIds, CancellationToken ct);
    Task<List<CoffeeOption>> GetOptionsForCategoryAsync(Guid categoryId, CancellationToken ct);
    Task<List<Coffee>> SearchCoffeesAsync(string term, CancellationToken ct);
    Task<List<Store>> SearchStoresAsync(string term, CancellationToken ct);
    Task<List<CoffeeOption>> GetOptionsByStoreAsync(Guid storeId, CancellationToken ct);
    Task<Dictionary<Guid, List<Guid>>> GetOptionCategoryMapAsync(Guid storeId, CancellationToken ct);
    Task<CoffeeOption?> FindOptionAsync(Guid id, CancellationToken ct);
    Task AddOptionAsync(CoffeeOption option, CancellationToken ct);
    Task RemoveOptionAsync(CoffeeOption option, CancellationToken ct);
    Task SetOptionScopingAsync(Guid optionId, List<Guid> categoryIds, CancellationToken ct);
    // ---- Promotions ----
    Task<List<Promotion>> GetActivePromotionsAsync(Guid? storeId, DateOnly today, CancellationToken ct);
    Task<Promotion?> LookupPromoCodeAsync(Guid storeId, string code, DateOnly today, CancellationToken ct);
    Task<List<Promotion>> GetPromotionsByStoreAsync(Guid storeId, CancellationToken ct);
    Task<Promotion?> FindPromotionAsync(int id, CancellationToken ct);
    Task AddPromotionAsync(Promotion promotion, CancellationToken ct);
    Task RemovePromotionAsync(Promotion promotion, CancellationToken ct);
    // ---- Orders ----
    Task<List<Coffee>> FindActiveCoffeesAsync(Guid storeId, IEnumerable<Guid> coffeeIds, CancellationToken ct);
    Task<List<CoffeeOption>> GetStoreOptionsAsync(Guid storeId, CancellationToken ct);
    Task<LoyaltyCard?> FindLoyaltyCardAsync(Guid userId, Guid storeId, CancellationToken ct);
    Task<bool> TryRedeemLoyaltyAsync(Guid userId, Guid storeId, DateTime utcNow, CancellationToken ct);
    Task<Order?> FindOrderAsync(Guid id, CancellationToken ct);
    Task<Order?> FindOrderByIdempotencyAsync(Guid userId, string key, CancellationToken ct);
    Task<Guid> InsertOrderAsync(Order order, List<OrderItem> items, bool redeemLoyalty, CancellationToken ct);
    Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid id, CancellationToken ct);
    Task<List<OrderSummaryDto>> GetOrderSummariesAsync(Guid? userId, Guid? storeId, Guid? driverId, string[]? statuses, int page, int pageSize, CancellationToken ct);
    Task<int> CountOrderSummariesAsync(Guid? userId, Guid? storeId, Guid? driverId, string[]? statuses, CancellationToken ct);
    Task<Driver?> FindDriverAsync(Guid id, CancellationToken ct);
    Task ExecuteInOrderWriteTxAsync(Func<CancellationToken, Task> action, CancellationToken ct);
    // ---- Account & social ----
    Task<List<LoyaltyCard>> GetLoyaltyCardsAsync(Guid userId, CancellationToken ct);
    Task<List<Address>> GetAddressesAsync(Guid userId, CancellationToken ct);
    Task<Address?> FindAddressAsync(Guid id, CancellationToken ct);
    Task AddAddressAsync(Address address, CancellationToken ct);
    Task RemoveAddressAsync(Address address, CancellationToken ct);
    Task SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct);
    Task<List<Guid>> GetFavoriteCoffeeIdsAsync(Guid userId, CancellationToken ct);
    Task<List<Coffee>> GetFavoriteCoffeesAsync(Guid userId, CancellationToken ct);
    Task AddFavoriteAsync(Guid userId, Guid coffeeId, CancellationToken ct);
    Task RemoveFavoriteAsync(Guid userId, Guid coffeeId, CancellationToken ct);
    Task<List<Guid>> GetFavoriteStoreIdsAsync(Guid userId, CancellationToken ct);
    Task<List<Store>> GetFavoriteStoresAsync(Guid userId, CancellationToken ct);
    Task AddStoreFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct);
    Task RemoveStoreFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct);
    Task<List<CoffeeReview>> GetCoffeeReviewsAsync(Guid coffeeId, int limit, CancellationToken ct);
    Task<List<Guid>> GetReviewedCoffeeIdsAsync(Guid orderId, CancellationToken ct);
    Task<bool> OrderContainsCoffeeAsync(Guid orderId, Guid coffeeId, CancellationToken ct);
    Task AddReviewAsync(CoffeeReview review, CancellationToken ct);
    Task<Dictionary<Guid, string?>> GetProfileNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct);
    Task<List<ChatMessage>> GetOrderMessagesAsync(Guid orderId, int limit, DateTime? before, CancellationToken ct);
    Task AddChatMessageAsync(ChatMessage message, CancellationToken ct);
    Task RegisterDriverAsync(Guid userId, string? fullName, string? phone, string? vehicle, CancellationToken ct);
    Task SetDriverAvailabilityAsync(Guid userId, bool isAvailable, CancellationToken ct);
    Task<List<Driver>> GetAvailableDriversAsync(CancellationToken ct);
    Task UpdateProfileAsync(Guid userId, string? fullName, string? avatarUrl, CancellationToken ct);
    Task DeleteAccountAsync(Guid userId, CancellationToken ct);
    Task<List<SellerEarningRow>> GetSellerEarningRowsAsync(Guid storeId, DateTime sinceUtc, CancellationToken ct);
    Task UpsertPushTokenAsync(Guid userId, string token, CancellationToken ct);
    Task DeleteMyPushTokensAsync(Guid userId, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
