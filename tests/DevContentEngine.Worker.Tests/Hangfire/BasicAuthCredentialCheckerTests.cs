using DevContentEngine.Worker.Hangfire;
using FluentAssertions;

namespace DevContentEngine.Worker.Tests.Hangfire;

public class BasicAuthCredentialCheckerTests
{
    private const string Username = "admin";
    private const string Password = "s3cr3t";

    private static string BuildHeader(string username, string password) =>
        "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));

    [Fact]
    public void Matches_returns_true_for_the_correct_credentials()
    {
        var header = BuildHeader(Username, Password);

        BasicAuthCredentialChecker.Matches(header, Username, Password).Should().BeTrue();
    }

    [Fact]
    public void Matches_returns_false_for_the_wrong_password()
    {
        var header = BuildHeader(Username, "wrong-password");

        BasicAuthCredentialChecker.Matches(header, Username, Password).Should().BeFalse();
    }

    [Fact]
    public void Matches_returns_false_for_the_wrong_username()
    {
        var header = BuildHeader("someone-else", Password);

        BasicAuthCredentialChecker.Matches(header, Username, Password).Should().BeFalse();
    }

    [Fact]
    public void Matches_returns_false_when_the_header_is_missing()
    {
        BasicAuthCredentialChecker.Matches(null, Username, Password).Should().BeFalse();
    }

    [Fact]
    public void Matches_returns_false_when_the_scheme_is_not_Basic()
    {
        BasicAuthCredentialChecker.Matches("Bearer sometoken", Username, Password).Should().BeFalse();
    }

    [Fact]
    public void Matches_returns_false_when_the_base64_payload_is_malformed()
    {
        BasicAuthCredentialChecker.Matches("Basic not-valid-base64!!", Username, Password).Should().BeFalse();
    }

    [Fact]
    public void Matches_returns_false_when_the_decoded_payload_has_no_colon_separator()
    {
        var header = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("no-separator-here"));

        BasicAuthCredentialChecker.Matches(header, Username, Password).Should().BeFalse();
    }
}
