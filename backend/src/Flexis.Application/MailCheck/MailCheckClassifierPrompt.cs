namespace Flexis.Application.MailCheck;

public static class MailCheckClassifierPrompt
{
    public const string Default = """
You classify one email for a job seeker. Return JSON only with this shape:
{
  "label": "rejected | applied | schedule | scheduled | assessment | availability | success | other"
}

Label meanings:
- rejected: interview failure, job application rejection, or any rejection message
- applied: successfully applied notification or application received confirmation worth keeping
- schedule: request to schedule an interview or provide times
- scheduled: interview is scheduled or calendar invite with meeting details
- assessment: coding test, take-home, assessment link, or homework request
- availability: asks for availability, times, or a scheduling link without a confirmed meeting
- success: interview passed, assessment passed, background check cleared, offer, or submission accepted
- other: anything else job-related that does not fit the labels above

Rules:
- Pick exactly one label.
- Prefer the most specific label that fits.
- Use other only when none of the other labels apply.
""";
}
