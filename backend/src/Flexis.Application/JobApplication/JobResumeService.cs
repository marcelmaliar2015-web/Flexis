using System.Text.Json;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobResumeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IJobResumeRepository _resume;
    private readonly IJobCatalogRepository _catalog;
    private readonly GoogleAccessTokenService _tokens;
    private readonly GoogleDriveLayoutService _driveLayout;
    private readonly IGoogleDriveGateway _drive;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly JobApplicationActivity _activity;

    public JobResumeService(
        IJobResumeRepository resume,
        IJobCatalogRepository catalog,
        GoogleAccessTokenService tokens,
        GoogleDriveLayoutService driveLayout,
        IGoogleDriveGateway drive,
        IGoogleSheetsWorkspace sheets,
        JobApplicationActivity activity)
    {
        _resume = resume;
        _catalog = catalog;
        _tokens = tokens;
        _driveLayout = driveLayout;
        _drive = drive;
        _sheets = sheets;
        _activity = activity;
    }

    public async Task<JobResumeBoardDto> GetBoardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        var profiles = await _catalog.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var profileSettings = (await _resume.ListProfileSettingsByUserIdAsync(userId, cancellationToken))
            .ToDictionary(item => item.ProfileId);
        var ownerOptions = ReadOwnerOptions(settings.OwnerOptionsJson);
        var rows = profiles
            .Select(profile =>
            {
                profileSettings.TryGetValue(profile.Id, out var resume);
                return new JobResumeProfileRowDto(
                    profile.Id,
                    profile.Title,
                    profile.Url,
                    resume?.Prompt ?? string.Empty,
                    resume?.ResumeStyle,
                    resume?.Owner ?? string.Empty);
            })
            .OrderBy(profile => profile.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new JobResumeBoardDto(settings.JobMasterUrl, ownerOptions, rows);
    }

    public async Task<JobResumeBoardDto> UpdateOwnerOptionsAsync(
        Guid userId,
        JobResumeOwnerOptionsWriteRequest request,
        CancellationToken cancellationToken)
    {
        var options = JobResumeRules.NormalizeOwnerOptions(request.OwnerOptions);
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        settings.SetOwnerOptionsJson(JsonSerializer.Serialize(options, JsonOptions));
        await _resume.SaveChangesAsync(cancellationToken);
        await SyncJobMasterAsync(userId, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "resume",
            "update-owner-options",
            "Updated resume owner options",
            $"Saved {options.Count} owner option(s) for resume generation.",
            cancellationToken);
        return await GetBoardAsync(userId, cancellationToken);
    }

    public async Task<JobResumeBoardDto> UpdateProfileResumeAsync(
        Guid userId,
        Guid profileId,
        JobResumeProfileWriteRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _catalog.GetByIdAsync(userId, profileId, cancellationToken)
            ?? throw new NotFoundException("Profile was not found.");
        if (profile.Kind != JobCatalogKind.Profile)
        {
            throw new NotFoundException("Profile was not found.");
        }

        var prompt = JobResumeRules.NormalizePrompt(request.Prompt);
        var resumeStyle = JobResumeRules.NormalizeResumeStyle(request.ResumeStyle);
        var owner = JobResumeRules.NormalizeOwner(request.Owner);
        if (owner.Length > 0)
        {
            var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
            var options = ReadOwnerOptions(settings.OwnerOptionsJson);
            if (!options.Any(option => string.Equals(option, owner, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationFailedException("Choose an owner from the saved owner options.");
            }
        }

        var profileSettings = await _resume.GetProfileSettingsAsync(profileId, cancellationToken);
        if (profileSettings is null)
        {
            profileSettings = JobProfileResumeSettings.Create(profileId);
            profileSettings.Update(prompt, resumeStyle, owner);
            await _resume.AddProfileSettingsAsync(profileSettings, cancellationToken);
        }
        else
        {
            profileSettings.Update(prompt, resumeStyle, owner);
        }

        await _resume.SaveChangesAsync(cancellationToken);
        await SyncJobMasterAsync(userId, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "resume",
            "update-profile-resume",
            $"Updated resume settings on {profile.Title}",
            $"Saved prompt, resume style, and owner for profile {profile.Title}.",
            cancellationToken);
        return await GetBoardAsync(userId, cancellationToken);
    }

    public async Task SyncJobMasterAsync(Guid userId, CancellationToken cancellationToken)
    {
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var folders = await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        var spreadsheetId = settings.JobMasterSpreadsheetId;
        if (!string.IsNullOrWhiteSpace(spreadsheetId)
            && !await _drive.SpreadsheetIsActiveAsync(access.AccessToken, spreadsheetId, cancellationToken))
        {
            spreadsheetId = null;
        }

        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            spreadsheetId = await _drive.FindSpreadsheetAsync(
                access.AccessToken,
                FlexisDriveLayout.JobMasterFileName,
                folders.RootFolderId,
                cancellationToken);
        }

        var workbook = await _sheets.EnsureJobMasterWorkbookAsync(
            access.AccessToken,
            folders.RootFolderId,
            spreadsheetId,
            cancellationToken);
        await _drive.MoveFileToFolderAsync(
            access.AccessToken,
            workbook.SpreadsheetId,
            folders.RootFolderId,
            cancellationToken);
        settings.SetJobMaster(workbook.SpreadsheetId, workbook.SpreadsheetUrl);
        await _resume.SaveChangesAsync(cancellationToken);

        var profiles = await _catalog.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var profileSettings = (await _resume.ListProfileSettingsByUserIdAsync(userId, cancellationToken))
            .ToDictionary(item => item.ProfileId);
        var rows = profiles
            .Select(profile =>
            {
                if (!profileSettings.TryGetValue(profile.Id, out var resume) || !resume.HasResumeConfig())
                {
                    return null;
                }

                return new JobMasterProfileRow(
                    profile.Title,
                    profile.Title,
                    profile.Url,
                    resume.Prompt,
                    resume.ResumeStyle,
                    resume.Owner);
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await _sheets.SyncJobMasterProfileManagementAsync(
            access.AccessToken,
            workbook.SpreadsheetId,
            rows,
            cancellationToken);
    }

    private async Task<JobResumeSettings> GetOrCreateSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _resume.GetSettingsByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = JobResumeSettings.Create(userId);
        await _resume.AddSettingsAsync(created, cancellationToken);
        await _resume.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static IReadOnlyList<string> ReadOwnerOptions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
