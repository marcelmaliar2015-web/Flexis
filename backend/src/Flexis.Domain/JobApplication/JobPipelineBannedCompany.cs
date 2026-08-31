namespace Flexis.Domain.JobApplication;

public sealed class JobPipelineBannedCompany
{
    private JobPipelineBannedCompany()
    {
        CompanyName = string.Empty;
        MatchKey = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid PipelineEntryId { get; private set; }

    public string CompanyName { get; private set; }

    public string MatchKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static JobPipelineBannedCompany Create(Guid pipelineEntryId, string companyName, string matchKey)
    {
        return new JobPipelineBannedCompany
        {
            Id = Guid.NewGuid(),
            PipelineEntryId = pipelineEntryId,
            CompanyName = companyName,
            MatchKey = matchKey,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Replace(string companyName, string matchKey)
    {
        CompanyName = companyName;
        MatchKey = matchKey;
    }
}
