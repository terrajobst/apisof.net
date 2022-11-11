#define CRAWL_CUSTOM_ATTRIBUTES
#define CRAWL_IL

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Arroyo;
using Arroyo.Signatures;

namespace Terrajobst.UsageCrawling;

public sealed class AssemblyCrawlerArroyo
{
    private readonly Dictionary<ApiKey, int> _results = new();

    public CrawlerResults GetResults()
    {
        return new CrawlerResults(_results);
    }

    public void Crawl(MetadataFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        switch (file)
        {
            case MetadataAssembly assembly:
                Crawl(assembly);
                break;
            case MetadataModule module:
                Crawl(module);
                break;
            default:
                throw new Exception($"Unexpected metadata file: {file}");
        }
    }

    private void Crawl(MetadataAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        CrawlAttributes(assembly.GetCustomAttributes(includeProcessed: true));
        Crawl(assembly.MainModule);
    }

    private void Crawl(MetadataModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        CrawlAttributes(module.GetCustomAttributes(includeProcessed: true));

        foreach (var type in module.Types)
            CrawlType(type);
    }

    private void CrawlAttributes(MetadataCustomAttributeEnumerator attributes)
    {
#if CRAWL_CUSTOM_ATTRIBUTES
        foreach (var attribute in attributes)
        {
            Record(attribute.Constructor.ContainingType);
            Record(attribute.Constructor);

            var typeDocId = attribute.Constructor.ContainingType.GetDocumentationId()!;
            var typeComponent = typeDocId[2..];

            foreach (var fixedArgument in attribute.FixedArguments)
                CrawlTypedValue(fixedArgument);
            
            foreach (var namedArgument in attribute.NamedArguments)
            {
                var prefix = namedArgument.Kind switch {
                    CustomAttributeNamedArgumentKind.Field => "F",
                    CustomAttributeNamedArgumentKind.Property => "P",
                    _ => throw new Exception($"Unexpected kind {namedArgument.Kind}")
                };
                var documentationId = $"{prefix}:{typeComponent}.{namedArgument.Name}"; 
                Record(documentationId);
                CrawlTypedValue(namedArgument.Value);
            }
        }
#endif
    }

    private void CrawlTypedValue(MetadataTypedValue typedValue)
    {
        if (typedValue.Value is MetadataType t1)
            Record(t1);

        foreach (var value in typedValue.Values)
            CrawlTypedValue(value);
    }

    private void CrawlType(MetadataNamedType type)
    {
        CrawlAttributes(type.GetCustomAttributes(includeProcessed: true));

        if (type.BaseType is not null)
            Record(type.BaseType);

        foreach (var i in type.InterfaceImplementations)
            Record(i.Interface);

        foreach (var member in type.Members)
            CrawlMember(member);
    }

    private void CrawlMember(IMetadataTypeMember typeMember)
    {
        switch (typeMember)
        {
            case MetadataNamedType t:
                CrawlType(t);
                break;
            case MetadataMethod m:
                CrawlMethod(m);
                break;
            case MetadataField f:
                CrawlField(f);
                break;
            case MetadataProperty p:
                CrawlProperty(p);
                break;
            case MetadataEvent e:
                CrawlEvent(e);
                break;
        }
    }

    private void CrawlMethod(MetadataMethod m)
    {
        CrawlAttributes(m.GetCustomAttributes(includeProcessed: true));
        CrawlParameters(m.Parameters);
        Record(m.ReturnType);

#if CRAWL_IL
        foreach (var op in m.GetOperations())
        {
            switch (op.Argument)
            {
                case MetadataType opT:
                    Record(opT);
                    break;
                case MetadataMember opM:
                    Record(opM);
                    break;
            }
        }
#endif
    }

    private void CrawlParameters(ImmutableArray<MetadataParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            CrawlAttributes(parameter.GetCustomAttributes(includeProcessed: true));
            Record(parameter.ParameterType);
        }
    }

    private void CrawlField(MetadataField f)
    {
        CrawlAttributes(f.GetCustomAttributes(includeProcessed: true));
        Record(f.FieldType);
    }

    private void CrawlProperty(MetadataProperty p)
    {
        CrawlAttributes(p.GetCustomAttributes(includeProcessed: true));
        CrawlParameters(p.Parameters);
        Record(p.PropertyType);
    }

    private void CrawlEvent(MetadataEvent e)
    {
        CrawlAttributes(e.GetCustomAttributes(includeProcessed: true));
        Record(e.EventType);
    }

    private bool IsDefinition(MetadataType type)
    {
        return type is MetadataNamedType;
    }

    private bool IsDefinition(MetadataMember typeOrMember)
    {
        switch (typeOrMember)
        {
            case MetadataEvent:
            case MetadataField:
            case MetadataMethod:
            case MetadataProperty:
                return true;
            case MetadataFieldReference:
            case MetadataMethodInstance:
            case MetadataMethodReference:
                return false;
            default:
                throw new Exception($"Unexpected member: {typeOrMember}");
        }
    }

    private void Record(MetadataType type)
    {
        if (IsDefinition(type))
            return;
        
        switch (type)
        {
            case MetadataArrayType array:
                Record(array.ElementType);
                break;
            case MetadataByReferenceType byRef:
                Record(byRef.ElementType);
                break;
            case MetadataModifiedType modifiedType:
                Record(modifiedType.UnmodifiedType);
                Record(modifiedType.CustomModifier.Type);
                break;
            case MetadataPinnedType pinnedType:
                Record(pinnedType.ElementType);
                break;
            case MetadataPointerType pointerType:
                Record(pointerType.ElementType);
                break;
            case MetadataFunctionPointerType functionPointerType:
            {
                Record(functionPointerType.Signature.ReturnType);
                foreach (var parameter in functionPointerType.Signature.Parameters)
                    Record(parameter.ToRawType());
                break;
            }
            case MetadataNamedTypeInstance generic:
            {
                Record(generic.GenericType);
                foreach (var argument in generic.TypeArguments)
                    Record(argument);
                break;
            }
            case MetadataNamedTypeReference:
            case MetadataTypeParameterReference:
            {
                var id = type.GetDocumentationId();
                if (id is not null)
                    Record(id);
                break;
            }
            default:
                throw new Exception($"Unexpected type: {type}");
        }
    }

    private void Record(MetadataMember member)
    {
        if (IsDefinition(member))
            return;

        if (member is MetadataMethodInstance methodInstance)
        {
            Record(methodInstance.Method);
            foreach (var argument in methodInstance.TypeArguments)
                Record(argument);
            return;
        }
        
        var id = member.GetDocumentationId();
        if (id is not null)
            Record(id);
    }

    private void Record(string documentationId)
    {
        ArgumentNullException.ThrowIfNull(documentationId);

        var key = new ApiKey(documentationId);
        _results.TryGetValue(key, out var count);
        _results[key] = count + 1;
    }
}