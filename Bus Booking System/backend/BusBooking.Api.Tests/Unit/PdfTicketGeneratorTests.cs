using System.Text;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Pdf;
using FluentAssertions;
using QuestPDF;
using QuestPDF.Infrastructure;

namespace BusBooking.Api.Tests.Unit;

public class PdfTicketGeneratorTests
{
    public PdfTicketGeneratorTests()
    {
        Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Generate_ReturnsPdf()
    {
        var gen = new PdfTicketGenerator();
        var dto = new BookingDetailDto(
            Guid.NewGuid(),
            "BK-ABCDEFGH",
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            "Bangalore", "Chennai",
            "Volvo Multi-axle", "SpeedyBus",
            new TimeOnly(22, 0), new TimeOnly(6, 0),
            900m, 50m, 950m, 1, "confirmed",
            null, DateTime.UtcNow,
            null, null, null, null,
            new[] { new BookingSeatDto("A1", "Asha", 30, "female") });

        var bytes = gen.Generate(dto);

        bytes.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(1_000);
    }
}

