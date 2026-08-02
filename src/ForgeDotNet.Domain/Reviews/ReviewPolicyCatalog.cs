namespace ForgeDotNet.Domain.Reviews;

public static class ReviewPolicyCatalog
{
    public static ReviewPolicy Version1 { get; } = new(
        "forge-reviews",
        1,
        "reviews-v1-20260729",
        "Europe/Paris",
        Array.AsReadOnly(new[] { 1, 3, 7, 14, 30 }),
        Array.AsReadOnly(new[] { 1, 7, 14, 30 }));
}
