namespace Flexis.Domain.JobApplication;

public sealed class JobProfileBannedCompany
{
    private JobProfileBannedCompany()
    {
        CompanyName = string.Empty;
        MatchKey = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid ProfileId { get; private set; }

    public string CompanyName { get; private set; }

    public string MatchKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static JobProfileBannedCompany Create(Guid profileId, string companyName, string matchKey)
    {
        return new JobProfileBannedCompany
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
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
