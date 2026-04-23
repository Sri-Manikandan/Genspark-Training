using BusBooking.Api.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusBooking.Api.Infrastructure.Pdf;

public class PdfTicketGenerator : IPdfTicketGenerator
{
    public byte[] Generate(BookingDetailDto b)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(ts => ts.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("BusBooking — e-Ticket").FontSize(20).Bold();
                    col.Item().Text($"Booking code: {b.BookingCode}").SemiBold();
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"{b.SourceCity} → {b.DestinationCity}").FontSize(14).SemiBold();
                    col.Item().Text($"{b.TripDate:yyyy-MM-dd}  ·  {b.DepartureTime:HH\\:mm} → {b.ArrivalTime:HH\\:mm}");
                    col.Item().Text($"{b.BusName} · {b.OperatorName}");
                    col.Item().PaddingTop(8).Text("Passengers").Bold();

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(60);
                            c.RelativeColumn();
                            c.ConstantColumn(60);
                            c.ConstantColumn(80);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Text("Seat").Bold();
                            h.Cell().Text("Name").Bold();
                            h.Cell().Text("Age").Bold();
                            h.Cell().Text("Gender").Bold();
                        });
                        foreach (var s in b.Seats)
                        {
                            t.Cell().Text(s.SeatNumber);
                            t.Cell().Text(s.PassengerName);
                            t.Cell().Text(s.PassengerAge.ToString());
                            t.Cell().Text(s.PassengerGender);
                        }
                    });

                    col.Item().PaddingTop(12).Text($"Fare: ₹{b.TotalFare:0.00}");
                    col.Item().Text($"Platform fee: ₹{b.PlatformFee:0.00}");
                    col.Item().Text($"Total paid: ₹{b.TotalAmount:0.00}").Bold();
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated ").FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.UtcNow.ToString("u")).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }
}

