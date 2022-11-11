using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Arroyo.Tests.Infra;

[DebuggerNonUserCode]
internal sealed class MetadataAssemblyAssertions : ReferenceTypeAssertions<MetadataAssembly, MetadataAssemblyAssertions>
{
    public MetadataAssemblyAssertions(MetadataAssembly instance)
        : base(instance)
    {
    }

    protected override string Identifier => "assembly";

    [CustomAssertion]
    public AndWhichConstraint<MetadataAssemblyAssertions, IMetadataTypeMember> ContainSingleMember<T>(
        string qualifiedName, string because = "", params object[] becauseArgs)
        where T: class, IMetadataTypeMember
    {
        var resolution = Resolve<T>(qualifiedName, Subject);
        
        Execute
            .Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(resolution.IsSuccess)
            .FailWith(resolution.Format, resolution.Args);

        var member = resolution.Result.Should().BeAssignableTo<T>().Subject;
        return new AndWhichConstraint<MetadataAssemblyAssertions, IMetadataTypeMember>(this, member);
    }

    [CustomAssertion]
    public AndWhichConstraint<MetadataAssertions<IMetadataTypeMember>, MetadataField> ContainSingleField(
        string qualifiedName, string because = "", params object[] becauseArgs)
    {
        return ContainSingleMember<MetadataField>(qualifiedName, because, becauseArgs).Which.Should().BeOfType<MetadataField>();
    }

    [CustomAssertion]
    public AndWhichConstraint<MetadataAssertions<IMetadataTypeMember>, MetadataMethod> ContainSingleMethod(
        string qualifiedName, string because = "", params object[] becauseArgs)
    {
        return ContainSingleMember<MetadataMethod>(qualifiedName, because, becauseArgs).Which.Should().BeOfType<MetadataMethod>();
    }

    [CustomAssertion]
    public AndWhichConstraint<MetadataAssertions<IMetadataTypeMember>, MetadataProperty> ContainSingleProperty(
        string qualifiedName, string because = "", params object[] becauseArgs)
    {
        return ContainSingleMember<MetadataProperty>(qualifiedName, because, becauseArgs).Which.Should().BeOfType<MetadataProperty>();
    }

    [CustomAssertion]
    public AndWhichConstraint<MetadataAssertions<IMetadataTypeMember>, MetadataEvent> ContainSingleEvent(
        string qualifiedName, string because = "", params object[] becauseArgs)
    {
        return ContainSingleMember<MetadataEvent>(qualifiedName, because, becauseArgs).Which.Should().BeOfType<MetadataEvent>();
    }

    [CustomAssertion]
    public AndWhichConstraint<MetadataAssertions<IMetadataTypeMember>, MetadataNamedType> ContainSingleType(
        string qualifiedName, string because = "", params object[] becauseArgs)
    {
        return ContainSingleMember<MetadataNamedType>(qualifiedName, because, becauseArgs).Which.Should().BeOfType<MetadataNamedType>();
    }

    private static Resolution Resolve<T>(string qualifiedName, MetadataAssembly assembly)
        where T: IMetadataTypeMember
    {
        var parts = qualifiedName.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return Resolution.FailedToParseName(qualifiedName);
        
        var current = (object)assembly.MainModule.NamespaceRoot;
        
        foreach (var part in parts)
        {
            var unescapedPart = part.Replace('#', '.');
            
            switch (current)
            {
                case MetadataNamespace ns:
                {
                    var members = ns.Members.Where(m => m.Name == unescapedPart).ToArray();
                    if (members.Length != 1)
                    {
                        var container = ns.FullName.Length == 0
                            ? null
                            : $"namespace {ns.FullName}";
                        return Resolution.FailedToFindExactMatch(container, unescapedPart, members.Length);
                    }

                    current = members[0];
                    break;
                }
                case MetadataNamedType namedType:
                {
                    var members = namedType.Members.Where(m => m.Name == unescapedPart).OfType<T>().ToArray();
                    if (members.Length != 1)
                    {
                        var typeKind = namedType.Kind.ToString().ToLower();
                        var container = $"{typeKind} {namedType.GetFullName()}";
                        return Resolution.FailedToFindExactMatch(container, unescapedPart, members.Length);
                    }

                    current = members[0];
                    break;
                }
                default:
                    throw new Exception($"Unexpected scope: {current}");
            }
        }
        
        return Resolution.Success(current);
    }
    
    private sealed class Resolution
    {
        public static Resolution FailedToParseName(string qualifiedName)
        {
            return new Resolution("The name {0} was empty", new object[] {qualifiedName}, null);
        }
        
        public static Resolution FailedToFindExactMatch(string? container, string part, int numberMatches)
        {
            var start = container is null
                ? "Expected assembly {context:assembly} to contain"
                : $"Expected {container} to contain";

            if (numberMatches == 0)
                return new Resolution($"{start} a member named {{0}}", new object[] {part}, null);
                
            return new Resolution($"{start} a single member named {{0}} but found {numberMatches}", new object[] { part }, null);
        }

        public static Resolution Success(object result)
        {
            return new Resolution(null, null, result);
        }

        private Resolution(string? format, object[]? args, object? result)
        {
            Format = format;
            Args = args;
            Result = result;
        }

        [MemberNotNullWhen(true, nameof(Result))]
        [MemberNotNullWhen(false, nameof(Format), nameof(Args))]
        public bool IsSuccess
        {
            get { return Result is not null; }
        }

        public string? Format { get; }

        public object[]? Args { get; }

        public object? Result { get; }
    }
}