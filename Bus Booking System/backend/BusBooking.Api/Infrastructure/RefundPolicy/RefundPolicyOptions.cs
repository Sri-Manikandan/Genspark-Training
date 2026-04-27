namespace BusBooking.Api.Infrastructure.RefundPolicy;

public class RefundPolicyOptions
{
    public const string SectionName = "RefundPolicy";

    public List<RefundPolicyTier> Tiers { get; set; } = new();
    public int BlockBelowHours { get; set; } = 12;
}

public class RefundPolicyTier
{
    public int MinHoursBeforeDeparture { get; set; }
    public int RefundPercent { get; set; }
}
