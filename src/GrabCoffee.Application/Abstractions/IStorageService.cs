namespace GrabCoffee.Application.Abstractions;

public interface IStorageService
{
    IReadOnlySet<string> AllowedBuckets { get; }

    Task<string> UploadAsync(
        string bucket,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken,
        string? publicId = null);
}
