namespace BusBooking.Api.Infrastructure.Seeding;

public class DemoDataSeedOptions
{
    public const string SectionName = "DemoSeed";

    public bool Enabled { get; set; } = false;

    public string OperatorEmail { get; set; } = "operator@demo.local";
    public string OperatorPassword { get; set; } = "Operator123!";
    public string OperatorName { get; set; } = "Demo Operator";
    public string OperatorCompany { get; set; } = "Demo Bus Co.";

    public string CustomerEmail { get; set; } = "customer@demo.local";
    public string CustomerPassword { get; set; } = "Customer123!";
    public string CustomerName { get; set; } = "Demo Customer";
}
