using DevContentEngine.Domain.Services;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Services;

public class ImagePromptGeneratorTests
{
    [Fact]
    public void Build_includes_the_title_verbatim()
    {
        var prompt = ImagePromptGenerator.Build(
            "nleceguic/rush-order", "C#", "A React PWA and a Windows Forms app calling an API gateway.");

        prompt.Should().Contain("Bold white title reading 'nleceguic/rush-order'.");
    }

    [Fact]
    public void Build_includes_the_main_technology_as_the_subtitle_verbatim()
    {
        var prompt = ImagePromptGenerator.Build(
            "nleceguic/rush-order", "C#", "A React PWA and a Windows Forms app calling an API gateway.");

        prompt.Should().Contain("Small light-gray subtitle reading 'C#'.");
    }

    [Fact]
    public void Build_includes_the_diagram_description_extracted_from_the_post_verbatim()
    {
        var prompt = ImagePromptGenerator.Build(
            "nleceguic/rush-order",
            "C#",
            "A React PWA and a Windows Forms app calling an API gateway that fans out to Orders, " +
            "Payments and Notifications services, all backed by a PostgreSQL database.");

        prompt.Should().Contain(
            "The diagram should visually represent: A React PWA and a Windows Forms app calling an API " +
            "gateway that fans out to Orders, Payments and Notifications services, all backed by a " +
            "PostgreSQL database.");
    }

    [Fact]
    public void Build_never_adds_technologies_or_components_beyond_what_was_provided_in_the_description()
    {
        const string groundedDescription = "A single Worker service reading commits from a PostgreSQL database.";

        var prompt = ImagePromptGenerator.Build("dev-content-engine", "PostgreSQL", groundedDescription);

        prompt.Should().Contain(groundedDescription);
        prompt.Should().NotContain("Kubernetes");
        prompt.Should().NotContain("Redis");
        prompt.Should().NotContain("Kafka");
        prompt.Should().NotContain("microservices");
    }

    [Fact]
    public void Build_appends_a_trailing_period_to_the_description_when_it_is_missing()
    {
        var prompt = ImagePromptGenerator.Build("dev-content-engine", "C#", "A single Worker service");

        prompt.Should().Contain("The diagram should visually represent: A single Worker service. High contrast");
    }

    [Fact]
    public void Build_does_not_duplicate_the_trailing_period_when_the_description_already_has_one()
    {
        var prompt = ImagePromptGenerator.Build("dev-content-engine", "C#", "A single Worker service.");

        prompt.Should().Contain("The diagram should visually represent: A single Worker service. High contrast");
        prompt.Should().NotContain("service..");
    }

    [Fact]
    public void Build_matches_the_reference_dark_navy_architecture_diagram_template()
    {
        var prompt = ImagePromptGenerator.Build("dev-content-engine", "C#", "A single Worker service.");

        prompt.Should().StartWith("Dark navy/indigo tech background with a subtle dot-grid texture.");
        prompt.Should().Contain("electric blue-to-violet neon glow");
        prompt.Should().Contain("rounded rectangle nodes connected by thin directional arrows");
        prompt.Should().Contain("not photorealistic");
        prompt.Should().Contain("Small circular logo badge top-left.");
        prompt.Should().Contain("suitable as a LinkedIn post cover image, 1200x630.");
    }

    [Theory]
    [InlineData("", "C#", "Some description")]
    [InlineData(" ", "C#", "Some description")]
    [InlineData(null, "C#", "Some description")]
    public void Build_throws_when_the_title_is_missing(string? title, string subtitle, string description)
    {
        var act = () => ImagePromptGenerator.Build(title!, subtitle, description);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Title", "", "Some description")]
    [InlineData("Title", " ", "Some description")]
    [InlineData("Title", null, "Some description")]
    public void Build_throws_when_the_subtitle_is_missing(string title, string? subtitle, string description)
    {
        var act = () => ImagePromptGenerator.Build(title, subtitle!, description);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Title", "C#", "")]
    [InlineData("Title", "C#", " ")]
    [InlineData("Title", "C#", null)]
    public void Build_throws_when_the_diagram_description_is_missing(string title, string subtitle, string? description)
    {
        var act = () => ImagePromptGenerator.Build(title, subtitle, description!);

        act.Should().Throw<ArgumentException>();
    }
}
