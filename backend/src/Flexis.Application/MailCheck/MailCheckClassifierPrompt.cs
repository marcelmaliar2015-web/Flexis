namespace Flexis.Application.MailCheck;

public static class MailCheckClassifierPrompt
{
    public const string Default = """
You classify one email for a job seeker targeting software engineer roles. Read the full subject and body. Return JSON only:
{
  "label": "rejected | applied | schedule | scheduled | assessment | availability | ai_interview | code | success | other | less_important"
}

Label meanings:
- rejected: interview failure, job application rejection, role closed, or any rejection message
- applied: application received, application submitted, or confirmation that you applied successfully
- schedule: recruiter asks you to schedule a live interview with a person, pick a time, or share availability for a new human interview
- scheduled: live interview is confirmed, calendar invite, meeting link, or specific interview date and time is set with a person or panel
- assessment: coding test, HackerRank, Codility, take-home, online assessment link, or homework request (not a login or MFA code)
- availability: asks for your availability or scheduling link for a live interview without a confirmed time yet
- ai_interview: asks you to complete an AI interview, one-way video interview, HireVue, Ascend, Modern Hire, Spark Hire, asynchronous AI screening, or similar automated interview (not a live calendar booking with a recruiter)
- code: one-time password, security code, sign-in code, MFA or 2FA code, email verification code, magic link, verify-email link, password-reset link, or any short-lived code or link whose only purpose is to authenticate, confirm an email address, or unlock an account
- success: interview passed, assessment passed, background check cleared, offer, or submission accepted
- other: job-related mail about an application or interview you are actively pursuing that does not fit the labels above (for example background check steps, onboarding tasks, or HR paperwork for a role in process)
- less_important: low-value noise that is not about an active interview or job action — job posting alerts, job recommendations, recruiter role suggestions you did not apply for, LinkedIn or Indeed digests, advertisements, newsletters, unusual-activity or "new sign-in" notices that do not include a code or verify link, platform announcements, or other mail unrelated to software-engineer hiring steps for roles you are already in process on

Rules:
- Pick exactly one label.
- Prefer the most specific label that fits.
- Use the subject line as a strong signal; use the body for details and links.
- Prefer code whenever the mail delivers a numeric or alphanumeric OTP, "your code is", "verification code", "sign-in code", "security code", "one-time password", magic link, or verify/confirm email link to complete login or account access.
- Prefer code over less_important, other, applied, and success for authentication and verification mail, including codes from employers, ATS portals, Google, Microsoft, LinkedIn, GitHub, or banks.
- Prefer less_important for security or account notices that only warn about activity and do not give a code or verification link to use.
- Prefer assessment for coding challenges and take-homes; never use code for HackerRank, Codility, LeetCode, or similar interview tests.
- Prefer ai_interview over schedule, scheduled, assessment, and availability when the ask is clearly an AI, one-way, or automated interview rather than booking a live call.
- Prefer schedule or scheduled when the mail books or confirms a live conversation with a person (Zoom/Teams with a recruiter, phone screen, onsite).
- Prefer less_important over other for bulk alerts, digests, and suggestions you did not solicit for a specific application.
- Background checks, assessments, scheduling, AI interviews, verification codes, and HR tasks for an application you submitted belong in the specific labels above, not less_important.
- Use other only when the mail is job-related for an active process but none of the specific labels apply.

Examples:
- Subject "Thank you for applying to Acme" and body confirms receipt -> applied
- Subject "Interview invitation" with Calendly or "pick a time" for a recruiter call -> schedule
- Subject "Interview confirmed" with date, time, or Zoom/Teams link with a person -> scheduled
- Subject "Next steps" with HackerRank or coding challenge link -> assessment
- Subject "Availability for interview" asking for times for a live interview -> availability
- Subject "Complete your AI interview" or "HireVue interview invitation" -> ai_interview
- Subject "One-way video interview" or "Ascend interview request" -> ai_interview
- Subject "Your verification code is 482193" or "Sign-in code" with an OTP -> code
- Subject "Security code" or "Two-factor authentication" with digits to enter -> code
- Subject "Verify your email" or "Confirm your account" with a magic or verify link -> code
- Subject "Password reset" with a reset link or code -> code
- Subject "Update on your application" saying not moving forward -> rejected
- Subject "Offer letter" or "Congratulations" after interviews -> success
- Subject "Complete your background check" for a role you applied to -> other
- Subject "10 new jobs for you" or "Recommended roles" -> less_important
- Subject "Recruiter suggested a role" you never applied to -> less_important
- Subject "Microsoft account unusual sign-in" with no code to enter -> less_important
- Subject "New jobs matching your profile" from a job board -> less_important
- Newsletter, LinkedIn alert, or unrelated vendor mail -> less_important
""";
}
