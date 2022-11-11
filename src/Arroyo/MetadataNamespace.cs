using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Arroyo;

public sealed class MetadataNamespace : MetadataItem, IMetadataNamespaceMember
{
    private readonly MetadataModule _containingModule;
    private readonly MetadataNamespace? _parent;
    private readonly NamespaceDefinitionHandle _handle;
    private readonly NamespaceDefinition _definition;

    private string? _name;
    private string? _fullName;
    private ImmutableArray<IMetadataNamespaceMember> _members;

    internal MetadataNamespace(MetadataModule containingModule,
                               MetadataNamespace? parent,
                               NamespaceDefinitionHandle handle,
                               NamespaceDefinition definition)
    {
        _containingModule = containingModule;
        _parent = parent;
        _handle = handle;
        _definition = definition;

        // TODO: _namespaceDefinition.ExportedTypes
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public MetadataModule ContainingModule
    {
        get { return _containingModule; }
    }

    public MetadataNamespace? Parent
    {
        get { return _parent; }
    }

    public string Name
    {
        get
        {
            if (_name is null)
            {
                var name = ComputeName();
                Interlocked.CompareExchange(ref _name, name, null);
            }

            return _name;
        }
    }

    public string FullName
    {
        get
        {
            if (_fullName is null)
            {
                var fullName = ComputeFullName();
                Interlocked.CompareExchange(ref _fullName, fullName, null);
            }

            return _fullName;
        }
    }

    public ImmutableArray<IMetadataNamespaceMember> Members
    {
        get
        {
            if (_members.IsDefault)
            {
                var members = ComputeMembers();
                ImmutableInterlocked.InterlockedInitialize(ref _members, members);
            }

            return _members;
        }
    }

    public IEnumerable<MetadataNamespace> GetNamespaces()
    {
        return Members.OfType<MetadataNamespace>();
    }

    public IEnumerable<MetadataNamedType> GetTypes()
    {
        return Members.OfType<MetadataNamedType>();
    }

    private string ComputeName()
    {
        return _containingModule.MetadataReader.GetString(_definition.Name);
    }

    private string ComputeFullName()
    {
        var stack = new Stack<MetadataNamespace>();
        var current = this;
        while (current is not null && current.Parent is not null)
        {
            stack.Push(current);
            current = current.Parent;
        }

        var sb = new StringBuilder();

        while (stack.Count > 0)
        {
            current = stack.Pop();

            if (sb.Length > 0)
                sb.Append('.');

            sb.Append(current.Name);
        }

        return sb.ToString();
    }

    private ImmutableArray<IMetadataNamespaceMember> ComputeMembers()
    {
        var namespaceDefinitions = _definition.NamespaceDefinitions;
        var typeDefinitions = _definition.TypeDefinitions;
        var result = ImmutableArray.CreateBuilder<IMetadataNamespaceMember>(namespaceDefinitions.Length + typeDefinitions.Length);

        foreach (var namespaceDefinitionHandle in namespaceDefinitions)
        {
            var namespaceDefinition = _containingModule.MetadataReader.GetNamespaceDefinition(namespaceDefinitionHandle);
            var metadataNamespaceDefinition = new MetadataNamespace(_containingModule, this, namespaceDefinitionHandle, namespaceDefinition);
            result.Add(metadataNamespaceDefinition);
        }

        foreach (var typeDefinitionHandle in typeDefinitions)
        {
            var metadataTypeDefinition = new MetadataNamedType(_containingModule, this, typeDefinitionHandle);
            result.Add(metadataTypeDefinition);
        }

        return result.MoveToImmutable();
    }
}