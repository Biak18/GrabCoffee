using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GrabCoffee.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace GrabCoffee.Infrastructure.Storage;

public sealed class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

public sealed class CloudinaryStorageService : IImageUploadService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryStorageService(IConfiguration config)
    {
        var section = config.GetSection("Cloudinary");
        var options = section.Get<CloudinaryOptions>()
            ?? throw new InvalidOperationException("Cloudinary configuration is missing.");
        if (string.IsNullOrWhiteSpace(options.CloudName)
            || string.IsNullOrWhiteSpace(options.ApiKey)
            || string.IsNullOrWhiteSpace(options.ApiSecret))
            throw new InvalidOperationException(
                "Cloudinary:CloudName, Cloudinary:ApiKey and Cloudinary:ApiSecret are required.");

        _cloudinary = new Cloudinary(new Account(options.CloudName, options.ApiKey, options.ApiSecret));
    }

    public async Task<string> UploadAvatarAsync(Guid userId, Stream content, string fileName, CancellationToken ct)
    {
        var result = await _cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = $"grabcoffee/avatars/{userId}",
            Overwrite = true,
        }, ct);

        return RequireSecureUrl(result);
    }

    public async Task<string> UploadCoffeeImageAsync(Guid storeId, Stream content, string fileName, CancellationToken ct)
    {
        var result = await _cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = $"grabcoffee/coffees/{storeId}/{Guid.NewGuid()}",
            Overwrite = false,
        }, ct);

        return RequireSecureUrl(result);
    }

    private static string RequireSecureUrl(ImageUploadResult result)
    {
        if (result.StatusCode == System.Net.HttpStatusCode.OK
            && result.SecureUrl is not null)
            return result.SecureUrl.ToString();

        throw new InvalidOperationException($"Image upload failed: {result.Error?.Message ?? result.StatusCode.ToString()}");
    }
}
