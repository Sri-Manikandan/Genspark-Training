using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/v1/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;
    private readonly IValidator<CreateBookingRequest> _createValidator;
    private readonly IValidator<VerifyPaymentRequest> _verifyValidator;
    private readonly ICurrentUserAccessor _currentUser;

    public BookingsController(
        IBookingService bookings,
        IValidator<CreateBookingRequest> createValidator,
        IValidator<VerifyPaymentRequest> verifyValidator,
        ICurrentUserAccessor currentUser)
    {
        _bookings = bookings;
        _createValidator = createValidator;
        _verifyValidator = verifyValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<CreateBookingResponseDto>> Create([FromBody] CreateBookingRequest req, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(req, ct);
        var result = await _bookings.CreateAsync(_currentUser.UserId, req, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/verify-payment")]
    public async Task<ActionResult<BookingDetailDto>> VerifyPayment(
        Guid id,
        [FromBody] VerifyPaymentRequest req,
        CancellationToken ct)
    {
        await _verifyValidator.ValidateAndThrowAsync(req, ct);
        var result = await _bookings.VerifyPaymentAsync(_currentUser.UserId, id, req, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await _bookings.GetAsync(_currentUser.UserId, id, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/ticket")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var pdf = await _bookings.GetTicketPdfAsync(_currentUser.UserId, id, ct);
        return File(pdf, "application/pdf", $"ticket-{id}.pdf");
    }
}

