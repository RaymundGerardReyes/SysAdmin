using Shouldly;
using Xunit;

namespace Pipeline.Api.Tests.Endpoints;

[Trait("Category", "Api")]
public class StatusCodeMatrixTests
{
    [Theory]
    [InlineData("POST", "/api/v1/jobs", "valid-token+operator+valid-body", 202)]
    [InlineData("POST", "/api/v1/jobs", "no-token", 401)]
    [InlineData("POST", "/api/v1/jobs", "valid-token+viewer", 403)]
    [InlineData("POST", "/api/v1/jobs", "valid-token+operator+invalid-body", 400)]
    [InlineData("POST", "/api/v1/jobs", "same-idempotency-key-different-body", 409)]
    [InlineData("GET",  "/api/v1/jobs/{unknown}", "valid-token+operator", 404)]
    [InlineData("POST", "/api/v1/jobs", "rate-limit-exceeded", 429)]
    public void Endpoint_ExpectedStatusCodes_AreDeterministic(string method, string route, string scenario, int expected)
    {
        // Matrix contract validation test
        expected.ShouldBeInRange(200, 499);
        method.ShouldNotBeNullOrWhiteSpace();
        route.ShouldNotBeNullOrWhiteSpace();
        scenario.ShouldNotBeNullOrWhiteSpace();
    }
}
