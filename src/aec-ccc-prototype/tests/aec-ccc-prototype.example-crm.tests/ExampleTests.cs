namespace aec_ccc_prototype.example_crm.tests;

[TestFixture]
public class ExampleTests
{
    [Test]
    public void SampleTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void SampleTest_WithAssertions()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.That(value, Is.GreaterThan(0));
        Assert.That(value, Is.EqualTo(42));
    }
}
