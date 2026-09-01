namespace Flexis.Domain.JobApplication;

public sealed class JobProfileResumeSettings
{
    private JobProfileResumeSettings()
    {
        Prompt = string.Empty;
        Owner = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid ProfileId { get; private set; }

    public string Prompt { get; private set; }

    public int? ResumeStyle { get; private set; }

    public string Owner { get; private set; }

    public static JobProfileResumeSettings Create(Guid profileId)
    {
        return new JobProfileResumeSettings
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId
        };
    }

    public void Update(string prompt, int? resumeStyle, string owner)
    {
        Prompt = prompt;
        ResumeStyle = resumeStyle;
        Owner = owner;
    }

    public bool HasResumeConfig()
    {
        return !string.IsNullOrWhiteSpace(Prompt)
            || ResumeStyle.HasValue
            || !string.IsNullOrWhiteSpace(Owner);
    }
}
