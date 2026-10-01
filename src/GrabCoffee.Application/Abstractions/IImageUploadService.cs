namespace GrabCoffee.Application.Abstractions;

public interface IImageUploadService
{
    // Stable URL per user (re-upload overwrites, like the old Storage upsert).
    Task<string> UploadAvatarAsync(Guid userId, Stream content, string fileName, CancellationToken ct);

    // Unique URL per upload under the seller's store folder.
    Task<string> UploadCoffeeImageAsync(Guid storeId, Stream content, string fileName, CancellationToken ct);
}
