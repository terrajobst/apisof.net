using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataEvent : MetadataMember, IMetadataTypeMember
{
    private readonly MetadataNamedType _containingType;
    private readonly EventDefinitionHandle _handle;
    private readonly EventAttributes _attributes;
    private readonly MetadataMethod _adder;
    private readonly MetadataMethod _remover;
    private readonly MetadataMethod? _raiser;

    private string? _name;
    private MetadataAccessibility _accessibility = MetadataExtensions.UninitializedAccessibility;
    private MetadataType? _eventType;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataEvent(MetadataNamedType containingType,
                           EventDefinitionHandle handle,
                           Dictionary<int, MetadataMethod> methodByToken)
    {
        _containingType = containingType;
        _handle = handle;
        var definition = containingType.ContainingModule.MetadataReader.GetEventDefinition(handle);
        _attributes = definition.Attributes;

        var accessors = definition.GetAccessors();

        {
            var token = MetadataTokens.GetToken(accessors.Adder);
            _adder = methodByToken[token];
            _adder.SetAssociatedMember(this);
        }

        {
            var token = MetadataTokens.GetToken(accessors.Remover);
            _remover = methodByToken[token];
            _remover.SetAssociatedMember(this);
        }

        if (!accessors.Raiser.IsNil)
        {
            var token = MetadataTokens.GetToken(accessors.Raiser);
            methodByToken.TryGetValue(token, out _raiser);
            _raiser?.SetAssociatedMember(this);
        }
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    private protected override MetadataType ContainingTypeCore
    {
        get { return _containingType; }
    }

    public bool IsSpecialName
    {
        get { return _attributes.IsFlagSet(EventAttributes.SpecialName); }
    }

    public bool IsRuntimeSpecialName
    {
        get { return _attributes.IsFlagSet(EventAttributes.RTSpecialName); }
    }

    public new MetadataNamedType ContainingType
    {
        get { return _containingType; }
    }

    public override string Name
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

    public MetadataMethod Adder
    {
        get { return _adder; }
    }

    public MetadataMethod Remover
    {
        get { return _remover; }
    }

    public MetadataMethod? Raiser
    {
        get { return _raiser; }
    }

    public IEnumerable<MetadataMethod> Accessors
    {
        get
        {
            if (_adder is not null)
                yield return _adder;

            if (_remover is not null)
                yield return _remover;

            if (_raiser is not null)
                yield return _raiser;
        }
    }

    public MetadataAccessibility Accessibility
    {
        get
        {
            if (_accessibility == MetadataExtensions.UninitializedAccessibility)
                _accessibility = MetadataExtensions.GetAccessibilityFromAccessors(_adder, _remover);

            return _accessibility;
        }
    }

    public bool IsStatic
    {
        get { return _adder.IsStatic && _remover.IsStatic; }
    }

    public bool IsAbstract
    {
        get { return _adder.IsAbstract || _remover.IsAbstract; }
    }

    public bool IsVirtual
    {
        get { return !IsOverride && !IsAbstract && (_adder.IsVirtual || _remover.IsVirtual); }
    }

    public bool IsOverride
    {
        get { return _adder.IsOverride || _remover.IsOverride; }
    }

    public bool IsSealed
    {
        get { return _adder.IsSealed || _remover.IsSealed; }
    }

    public bool IsExtern
    {
        get { return _adder.IsExtern || _remover.IsExtern; }
    }

    public MetadataType EventType
    {
        get
        {
            if (_eventType is null)
            {
                var eventType = ComputeEventType();
                Interlocked.CompareExchange(ref _eventType, eventType, null);
            }

            return _eventType;
        }
    }

    public MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        if (_customAttributes.IsDefault)
        {
            var customAttributes = ComputeCustomAttributes();
            ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        }

        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    private string ComputeName()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetEventDefinition(_handle);
        return _containingType.ContainingModule.MetadataReader.GetString(definition.Name);
    }

    private MetadataType ComputeEventType()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetEventDefinition(_handle);
        var genericContext = new MetadataGenericContext(ContainingType);
        return ContainingType.ContainingModule.GetTypeReference(definition.Type, genericContext);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetEventDefinition(_handle);
        var customAttributes = definition.GetCustomAttributes();
        return _containingType.ContainingModule.GetCustomAttributes(customAttributes);
    }
}