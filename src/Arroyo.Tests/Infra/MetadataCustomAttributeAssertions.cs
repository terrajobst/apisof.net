namespace Arroyo.Tests.Infra;

internal sealed class MetadataCustomAttributeAssertions
{
    public MetadataCustomAttributeAssertions(MetadataCustomAttribute instance)
    {
        Subject = instance;
    }
    
    public MetadataCustomAttribute Subject { get; }

    [CustomAssertion]
    public AndConstraint<MetadataCustomAttributeAssertions> HaveFixedArguments(params Action<MetadataTypedValue>[] elementInspectors)
    {
        Subject.FixedArguments.Should().SatisfyRespectively(elementInspectors);
        return new AndConstraint<MetadataCustomAttributeAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<MetadataCustomAttributeAssertions> HaveNoNamedArguments(string because = "", params object[] becauseArgs)
    {
        Subject.NamedArguments.Should().BeEmpty(because, becauseArgs);
        return new AndConstraint<MetadataCustomAttributeAssertions>(this);
    }
}