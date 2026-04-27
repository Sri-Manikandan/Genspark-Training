using Xunit;

namespace BusBooking.Api.Tests.Support;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
}
