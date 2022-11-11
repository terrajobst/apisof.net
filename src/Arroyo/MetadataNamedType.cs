using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataNamedType : MetadataNamedTypeReference, IMetadataNamespaceMember, IMetadataTypeMember
{
    private readonly MetadataModule _containingModule;
    private readonly MetadataNamespace _containingNamespace;
    private readonly MetadataNamedType? _containingType;
    private readonly TypeDefinitionHandle _handle;
    private readonly TypeDefinition _definition;

    private string? _name;
    private TypeKind _kind = MetadataExtensions.UninitializedTypeKind;
    private MetadataType? _baseType = MetadataExtensions.UninitializedType;
    private bool _isRefLikeType;
    private bool _isReadOnly;
    private ImmutableArray<MetadataInterfaceImplementation> _interfaceImplementations;
    private ImmutableArray<MetadataTypeParameter> _genericParameters;
    private ImmutableArray<IMetadataTypeMember> _members;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    private Dictionary<int, IMetadataTypeMember>? _memberByToken;

    internal MetadataNamedType(MetadataModule containingModule,
                               MetadataNamespace containingNamespace,
                               TypeDefinitionHandle handle)
        : this(containingModule, containingNamespace, null, handle)
    {
    }

    private MetadataNamedType(MetadataModule containingModule,
                              MetadataNamedType containingType,
                              TypeDefinitionHandle handle)
        : this(containingModule, containingType.ContainingNamespace, containingType, handle)
    {
    }

    private MetadataNamedType(MetadataModule containingModule,
                              MetadataNamespace containingNamespace,
                              MetadataNamedType? containingType,
                              TypeDefinitionHandle handle)
    {
        _containingModule = containingModule;
        _containingNamespace = containingNamespace;
        _containingType = containingType;
        _handle = handle;
        _definition = containingModule.MetadataReader.GetTypeDefinition(handle);

        // TODO: _typeDefinition.GetDeclarativeSecurityAttributes()
        // TODO: _typeDefinition.GetLayout()
        // TODO: _typeDefinition.GetMethodImplementations()
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public override MetadataFile ContainingFile
    {
        get { return ContainingModule; }
    }

    private protected override MetadataNamedTypeReference? ContainingTypeCore
    {
        get { return ContainingType; }
    }

    public MetadataModule ContainingModule
    {
        get { return _containingModule; }
    }

    public MetadataNamespace ContainingNamespace
    {
        get { return _containingNamespace; }
    }

    public new MetadataNamedType? ContainingType
    {
        get { return _containingType; }
    }

    public MetadataAccessibility Accessibility
    {
        get { return _definition.Attributes.GetAccessibility(); }
    }

    public bool IsAbstract
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.Abstract); }
    }

    public bool IsSealed
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.Sealed); }
    }

    public bool IsSpecialName
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.SpecialName); }
    }

    public bool IsRuntimeSpecialName
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.RTSpecialName); }
    }

    public bool IsImport
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.Import); }
    }

    public bool IsSerializable
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.Serializable); }
    }

    public bool IsWindowsRuntime
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.WindowsRuntime); }
    }

    public bool HasSecurity
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.HasSecurity); }
    }

    public bool IsBeforeFieldInit
    {
        get { return _definition.Attributes.IsFlagSet(TypeAttributes.BeforeFieldInit); }
    }

    public bool IsClass
    {
        get { return (_definition.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class; }
    }

    public bool IsInterface
    {
        get { return (_definition.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface; }
    }

    public bool IsExplicitLayout
    {
        get { return (_definition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout; }
    }

    public bool IsSequentialLayout
    {
        get { return (_definition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout; }
    }

    public bool IsAutoLayout
    {
        get { return (_definition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout; }
    }

    public bool IsAnsiClass
    {
        get { return (_definition.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass; }
    }

    public bool IsUnicodeClass
    {
        get { return (_definition.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass; }
    }

    public bool IsAutoClass
    {
        get { return (_definition.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass; }
    }

    public bool IsCustomFormatClass
    {
        get { return (_definition.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass; }
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

    public override string NamespaceName
    {
        get { return _containingNamespace.FullName; }
    }

    public override int GenericArity
    {
        get { return GenericParameters.Length; }
    }

    public TypeKind Kind
    {
        get
        {
            if (_kind == MetadataExtensions.UninitializedTypeKind)
                _kind = ComputeKind();

            return _kind;
        }
    }

    public MetadataType? BaseType
    {
        get
        {
            if (_baseType == MetadataExtensions.UninitializedType)
            {
                var baseType = ComputeBaseType();
                Interlocked.CompareExchange(ref _baseType, baseType, MetadataExtensions.UninitializedType);
            }

            return _baseType;
        }
    }

    public bool IsRefLikeType
    {
        get
        {
            EnsureCustomAttributesAreLoaded();
            return _isRefLikeType;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            EnsureCustomAttributesAreLoaded();
            return _isReadOnly;
        }
    }

    public ImmutableArray<MetadataInterfaceImplementation> InterfaceImplementations
    {
        get
        {
            if (_interfaceImplementations.IsDefault)
            {
                var interfaceImplementations = ComputeInterfaceImplementations();
                ImmutableInterlocked.InterlockedInitialize(ref _interfaceImplementations, interfaceImplementations);
            }

            return _interfaceImplementations;
        }
    }

    public IEnumerable<MetadataEvent> GetEvents()
    {
        return Members.OfType<MetadataEvent>();
    }

    public IEnumerable<MetadataField> GetFields()
    {
        return Members.OfType<MetadataField>();
    }

    public ImmutableArray<MetadataTypeParameter> GenericParameters
    {
        get
        {
            if (_genericParameters.IsDefault)
            {
                var genericParameters = ComputeGenericParameters();
                ImmutableInterlocked.InterlockedInitialize(ref _genericParameters, genericParameters);
            }

            return _genericParameters;
        }
    }

    public IEnumerable<MetadataMethod> GetMethods()
    {
        return Members.OfType<MetadataMethod>();
    }

    public IEnumerable<MetadataNamedType> GetNestedTypes()
    {
        return Members.OfType<MetadataNamedType>();
    }

    public IEnumerable<MetadataProperty> GetProperties()
    {
        return Members.OfType<MetadataProperty>();
    }

    public ImmutableArray<IMetadataTypeMember> Members
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

    public MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        EnsureCustomAttributesAreLoaded();
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    public ImmutableArray<MetadataTypeParameter> GetAllGenericParameters()
    {
        if (_containingType is null)
            return GenericParameters;

        var builder = ImmutableArray.CreateBuilder<MetadataTypeParameter>();
        var t = this;
        while (t is not null)
        {
            var index = 0;
            foreach (var p in t.GenericParameters)
                builder.Insert(index++, p);

            t = t.ContainingType;
        }

        return builder.ToImmutable();
    }

    internal MetadataMethod GetMethod(MethodDefinitionHandle handle)
    {
        var token = MetadataTokens.GetToken(handle);
        return (MetadataMethod)GetFieldOrMethod(token);
    }

    internal MetadataField GetField(FieldDefinitionHandle handle)
    {
        var token = MetadataTokens.GetToken(handle);
        return (MetadataField)GetFieldOrMethod(token);
    }

    private IMetadataTypeMember GetFieldOrMethod(int token)
    {
        if (_memberByToken is null)
        {
            var memberByToken = Members.Where(m => m is MetadataField or MetadataMethod).ToDictionary(m => m.Token);
            Interlocked.CompareExchange(ref _memberByToken, memberByToken, null);
        }

        return _memberByToken[token];
    }

    private string ComputeName()
    {
        return SpecialNames.GetTypeName(_containingModule.MetadataReader.GetString(_definition.Name));
    }

    private TypeKind ComputeKind()
    {
        if (IsInterface)
            return TypeKind.Interface;

        if (IsSealed)
        {
            var baseTypeReference = BaseType as MetadataNamedTypeReference;
            var baseTypeFullName = baseTypeReference?.GetFullName();

            if (baseTypeFullName is not null)
            {
                switch (baseTypeFullName)
                {
                    case SpecialNames.SystemValueType:
                        return TypeKind.Struct;
                    case SpecialNames.SystemEnum:
                        return TypeKind.Enum;
                    case SpecialNames.SystemMulticastDelegate:
                        return TypeKind.Delegate;
                }
            }
        }

        return TypeKind.Class;
    }

    private MetadataType? ComputeBaseType()
    {
        if (_definition.BaseType.IsNil)
            return null;

        var genericContext = new MetadataGenericContext(this);
        return ContainingModule.GetTypeReference(_definition.BaseType, genericContext);
    }

    private ImmutableArray<MetadataInterfaceImplementation> ComputeInterfaceImplementations()
    {
        var interfaceImplementations = _definition.GetInterfaceImplementations();
        var result = ImmutableArray.CreateBuilder<MetadataInterfaceImplementation>(interfaceImplementations.Count);

        foreach (var interfaceImplementationHandle in interfaceImplementations)
        {
            var metadataGenericParameter = new MetadataInterfaceImplementation(this, interfaceImplementationHandle);
            result.Add(metadataGenericParameter);
        }

        return result.ToImmutable();
    }

    private ImmutableArray<MetadataTypeParameter> ComputeGenericParameters()
    {
        var startingIndex = 0;

        var c = _containingType;
        while (c is not null)
        {
            startingIndex += c.GenericArity;
            c = c.ContainingType;
        }

        var genericParameters = _definition.GetGenericParameters();
        var result = ImmutableArray.CreateBuilder<MetadataTypeParameter>(genericParameters.Count - startingIndex);

        for (var i = startingIndex; i < genericParameters.Count; i++)
        {
            var genericParameterHandle = genericParameters[i];
            var metadataGenericParameter = new MetadataTypeParameterForType(this, genericParameterHandle);
            result.Add(metadataGenericParameter);
        }

        return result.ToImmutable();
    }

    private ImmutableArray<IMetadataTypeMember> ComputeMembers()
    {
        var fieldDefinitions = _definition.GetFields();
        var methodDefinitions = _definition.GetMethods();
        var propertyDefinitions = _definition.GetProperties();
        var eventDefinitions = _definition.GetEvents();
        var typeDefinitions = _definition.GetNestedTypes();
        var memberCount = fieldDefinitions.Count +
                          methodDefinitions.Count +
                          propertyDefinitions.Count +
                          eventDefinitions.Count +
                          typeDefinitions.Length;

        var result = ImmutableArray.CreateBuilder<IMetadataTypeMember>(memberCount);

        // Fields

        foreach (var fieldDefinitionHandle in fieldDefinitions)
        {
            var metadataFieldDefinition = new MetadataField(this, fieldDefinitionHandle);
            result.Add(metadataFieldDefinition);
        }

        // Methods

        var methodByToken = new Dictionary<int, MetadataMethod>();

        foreach (var handle in methodDefinitions)
        {
            var token = MetadataTokens.GetToken(handle);
            var method = new MetadataMethod(this, handle);
            methodByToken.Add(token, method);
            result.Add(method);
        }

        // Properties

        foreach (var handle in propertyDefinitions)
        {
            var property = new MetadataProperty(this, handle, methodByToken);
            result.Add(property);
        }

        // Events

        foreach (var eventDefinitionHandle in eventDefinitions)
        {
            var metadataEventDefinition = new MetadataEvent(this, eventDefinitionHandle, methodByToken);
            result.Add(metadataEventDefinition);
        }

        // Nested types

        foreach (var typeDefinitionHandle in typeDefinitions)
        {
            var metadataFieldDefinition = new MetadataNamedType(_containingModule, this, typeDefinitionHandle);
            result.Add(metadataFieldDefinition);
        }

        return result.MoveToImmutable();
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var customAttributes = _definition.GetCustomAttributes();
        return _containingModule.GetCustomAttributes(customAttributes);
    }

    private void EnsureCustomAttributesAreLoaded()
    {
        if (!_customAttributes.IsDefault)
            return;

        var customAttributes = ComputeCustomAttributes();
        var isRefLikeType = false;
        var isReadOnly = false;

        foreach (var customAttribute in customAttributes)
        {
            if (customAttribute.FixedArguments.Length == 0 &&
                customAttribute.NamedArguments.Length == 0 &&
                customAttribute.Constructor.ContainingType is MetadataNamedTypeReference attributeType)
            {
                if (attributeType.Name == "IsByRefLikeAttribute" &&
                    attributeType.NamespaceName == "System.Runtime.CompilerServices")
                {
                    if (Kind == TypeKind.Struct)
                    {
                        isRefLikeType = true;
                        customAttribute.MarkProcessed();
                    }
                }
                else if (attributeType.Name == "IsReadOnlyAttribute" &&
                         attributeType.NamespaceName == "System.Runtime.CompilerServices")
                {
                    if (Kind == TypeKind.Struct)
                    {
                        isReadOnly = true;
                        customAttribute.MarkProcessed();
                    }
                }
            }
        }

        _isRefLikeType = isRefLikeType;
        _isReadOnly = isReadOnly;

        ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
    }
}