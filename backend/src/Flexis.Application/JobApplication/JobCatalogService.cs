using Flexis.Application.Common;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobCatalogService
{
    private readonly IJobCatalogRepository _items;

    public JobCatalogService(IJobCatalogRepository items)
    {
        _items = items;
    }

    public async Task<IReadOnlyList<JobCatalogItemDto>> ListAsync(
        Guid userId,
        JobCatalogKind kind,
        CancellationToken cancellationToken)
    {
        var items = await _items.ListAsync(userId, kind, cancellationToken);
        return items.Select(ToDto).ToArray();
    }

    public async Task<JobCatalogItemDto> CreateAsync(
        Guid userId,
        JobCatalogKind kind,
        JobCatalogWriteRequest request,
        CancellationToken cancellationToken)
    {
        var title = JobCatalogRules.NormalizeTitle(request.Title);
        var url = JobCatalogRules.NormalizeUrl(request.Url);
        if (await _items.TitleExistsAsync(userId, kind, title, null, cancellationToken))
        {
            throw new ConflictException($"A {KindLabel(kind)} with that title already exists.");
        }

        var item = JobCatalogItem.Create(userId, kind, title, url);
        await _items.AddAsync(item, cancellationToken);
        await _items.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<JobCatalogItemDto> UpdateAsync(
        Guid userId,
        Guid id,
        JobCatalogWriteRequest request,
        CancellationToken cancellationToken)
    {
        var title = JobCatalogRules.NormalizeTitle(request.Title);
        var url = JobCatalogRules.NormalizeUrl(request.Url);
        var item = await _items.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");

        if (await _items.TitleExistsAsync(userId, item.Kind, title, item.Id, cancellationToken))
        {
            throw new ConflictException($"A {KindLabel(item.Kind)} with that title already exists.");
        }

        item.SetDetails(title, url);
        await _items.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");
        _items.Remove(item);
        await _items.SaveChangesAsync(cancellationToken);
    }

    private static JobCatalogItemDto ToDto(JobCatalogItem item)
    {
        return new JobCatalogItemDto(item.Id, item.Title, item.CreatedAt, item.Url);
    }

    private static string KindLabel(JobCatalogKind itemKind)
    {
        return itemKind == JobCatalogKind.Profile ? "profile" : "source";
    }
}
