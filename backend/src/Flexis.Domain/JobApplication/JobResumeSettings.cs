namespace Flexis.Domain.JobApplication;

public sealed class JobResumeSettings
{
    private JobResumeSettings()
    {
        OwnerOptionsJson = "[]";
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string OwnerOptionsJson { get; private set; }

    public string? JobMasterSpreadsheetId { get; private set; }

    public string? JobMasterUrl { get; private set; }

    public static JobResumeSettings Create(Guid userId)
    {
        return new JobResumeSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
    }

    public void SetOwnerOptionsJson(string json)
    {
        OwnerOptionsJson = json;
    }

    public void SetJobMaster(string spreadsheetId, string url)
    {
        JobMasterSpreadsheetId = spreadsheetId;
        JobMasterUrl = url;
    }
}
