using BusBooking.Api.Tests.Support;
using FluentAssertions;

namespace BusBooking.Api.Tests.Unit;

public class RazorpaySignatureTests
{
    [Fact]
    public void VerifySignature_AcceptsValid()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_1", "pay_1", sig).Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_RejectsTamperedSignature()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_1", "pay_1", sig + "00").Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_RejectsWrongOrderId()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_2", "pay_1", sig).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_RejectsWrongPaymentId()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_1", "pay_2", sig).Should().BeFalse();
    }

    [Fact]
    public void BuildSignature_IsDeterministic()
    {
        var a = FakeRazorpayClient.BuildSignature("order_abc", "pay_xyz");
        var b = FakeRazorpayClient.BuildSignature("order_abc", "pay_xyz");
        a.Should().Be(b);
    }
}
