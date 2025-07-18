namespace aec_ccc_prototype.kernel.tests;

[TestFixture]
public class KernelTests
{
    [Test]
    public void SampleKernelTest_ShouldPass()
    {
        // Arrange
        var expected = "kernel";

        // Act
        var actual = "kernel";

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void SampleKernelTest_WithCollections()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act & Assert
        Assert.That(collection, Has.Count.EqualTo(3));
        Assert.That(collection, Contains.Item(2));
    }
}
