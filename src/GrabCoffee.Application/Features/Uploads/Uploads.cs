using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Stores;
using MediatR;

namespace GrabCoffee.Application.Features.Uploads;

public sealed record UploadedFile(byte[] Content, string FileName, string? ContentType);

public sealed record UploadAvatarCommand(UploadedFile File) : IRequest<string>;

public sealed record UploadCoffeeImageCommand(UploadedFile File) : IRequest<string>;

public abstract class UploadValidator<T> : AbstractValidator<T>
    where T : class
{
    protected static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    protected const int MaxBytes = 5_000_000;

    protected void ValidateFile(Func<T, UploadedFile> selector)
    {
        RuleFor(x => selector(x).Content.Length)
            .GreaterThan(0).WithMessage("File is empty.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("File must be 5 MB or smaller.");
        RuleFor(x => Path.GetExtension(selector(x).FileName).ToLowerInvariant())
            .Must(ext => AllowedExtensions.Contains(ext))
            .WithMessage("Only .jpg, .jpeg, .png or .webp images are allowed.");
    }
}

public sealed class UploadAvatarValidator : UploadValidator<UploadAvatarCommand>
{
    public UploadAvatarValidator()
    {
        ValidateFile(x => x.File);
    }
}

public sealed class UploadCoffeeImageValidator : UploadValidator<UploadCoffeeImageCommand>
{
    public UploadCoffeeImageValidator()
    {
        ValidateFile(x => x.File);
    }
}

public sealed class UploadAvatarHandler(IStorageService storage, ICurrentUser currentUser)
    : IRequestHandler<UploadAvatarCommand, string>
{
    public async Task<string> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        using var stream = new MemoryStream(request.File.Content, writable: false);
        // Stable public id per user: re-upload overwrites the old avatar.
        return await storage.UploadAsync(
            "avatars",
            request.File.FileName,
            stream,
            request.File.ContentType ?? "image/jpeg",
            ct,
            publicId: $"grabcoffee/avatars/{userId}");
    }
}

public sealed class UploadCoffeeImageHandler(
    IStorageService storage, IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UploadCoffeeImageCommand, string>
{
    public async Task<string> Handle(UploadCoffeeImageCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        using var stream = new MemoryStream(request.File.Content, writable: false);
        return await storage.UploadAsync(
            "coffee-images",
            request.File.FileName,
            stream,
            request.File.ContentType ?? "image/jpeg",
            ct);
    }
}
