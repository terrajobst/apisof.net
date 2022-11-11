namespace Arroyo.Tests;

public sealed class SpecialTypesTests
{
    [Fact]
    public void SpecialTypes_LiveInCorlib()
    {
        var module = MetadataFactory.CreateModule("");
        var specialTypes = Enum.GetValues<MetadataSpecialType>();
        
        foreach (var specialType in specialTypes)
        {
            var type = module.GetSpecialType(specialType);
            type.ContainingFile.Name.Should().Be("System.Runtime");
        }
    }
}