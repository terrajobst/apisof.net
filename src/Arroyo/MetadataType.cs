using System.CodeDom.Compiler;
using Arroyo.Signatures;

namespace Arroyo;

public abstract class MetadataType : MetadataItem
{
    private protected MetadataType()
    {
    }

    public override string ToString()
    {
        return Dumper.Dump(this);
    }

    private static class Dumper
    {
        public static string Dump(MetadataType type)
        {
            using var sw = new StringWriter();
            using (var writer = new IndentedTextWriter(sw))
                Dump(writer, type);
            return sw.ToString();
        }

        private static void Dump(IndentedTextWriter writer, MetadataType type)
        {
            switch (type)
            {
                case MetadataNamedType metadataNamedType:
                    DumpMetadataNamedType(writer, metadataNamedType);
                    break;
                case MetadataNamedTypeInstance metadataNamedTypeInstance:
                    DumpMetadataNamedTypeInstance(writer, metadataNamedTypeInstance);
                    break;
                case MetadataNamedTypeReference metadataNamedTypeReference:
                    DumpMetadataNamedTypeReference(writer, metadataNamedTypeReference);
                    break;
                case MetadataTypeParameter metadataTypeParameter:
                    DumpMetadataTypeParameter(writer, metadataTypeParameter);
                    break;
                case MetadataArrayType metadataArrayType:
                    DumpMetadataArrayType(writer, metadataArrayType);
                    break;
                case MetadataByReferenceType metadataByReferenceType:
                    DumpMetadataByReferenceType(writer, metadataByReferenceType);
                    break;
                case MetadataFunctionPointerType metadataFunctionPointerType:
                    DumpMetadataFunctionPointerType(writer, metadataFunctionPointerType);
                    break;
                case MetadataModifiedType metadataModifiedType:
                    DumpMetadataModifiedType(writer, metadataModifiedType);
                    break;
                case MetadataPinnedType metadataPinnedType:
                    DumpMetadataPinnedType(writer, metadataPinnedType);
                    break;
                case MetadataPointerType metadataPointerType:
                    DumpMetadataPointerType(writer, metadataPointerType);
                    break;
                case MetadataTypeParameterReference metadataTypeParameterReference:
                    DumpMetadataTypeParameterReference(writer, metadataTypeParameterReference);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static void DumpMetadataNamedType(IndentedTextWriter writer, MetadataNamedType type)
        {
            writer.WriteLine($"NamedType {type.GetFullName()}");
        }

        private static void DumpMetadataNamedTypeInstance(IndentedTextWriter writer, MetadataNamedTypeInstance type)
        {
            writer.WriteLine("NamedTypeInstance");
            writer.Indent++;
            Dump(writer, type.GenericType);
            foreach (var arg in type.TypeArguments)
                Dump(writer, arg);
            writer.Indent--;
        }

        private static void DumpMetadataNamedTypeReference(IndentedTextWriter writer, MetadataNamedTypeReference type)
        {
            writer.WriteLine($"NamedType {type.GetFullName()}");
        }

        private static void DumpMetadataTypeParameter(IndentedTextWriter writer, MetadataTypeParameter type)
        {
            writer.WriteLine($"TypeParameter {type.Name}");
        }

        private static void DumpMetadataArrayType(IndentedTextWriter writer, MetadataArrayType type)
        {
            var lowerBounds = string.Join(", ", type.LowerBounds);
            var sizes = string.Join(", ", type.Sizes);

            writer.WriteLine($"Array Rank={type.Rank}, Sizes={sizes}, LowerBounds={lowerBounds}");
            writer.Indent++;
            Dump(writer, type.ElementType);
            writer.Indent--;
        }

        private static void DumpMetadataByReferenceType(IndentedTextWriter writer, MetadataByReferenceType type)
        {
            writer.WriteLine("ByReference");
            writer.Indent++;
            Dump(writer, type.ElementType);
            writer.Indent--;
        }

        private static void DumpMetadataFunctionPointerType(IndentedTextWriter writer, MetadataFunctionPointerType type)
        {
            var cc = type.Signature.CallingConvention;
            writer.WriteLine($"FunctionPointer CallingConvention={cc}");
            writer.Indent++;
            Dump(writer, type.Signature.ReturnType);
            foreach (var p in type.Signature.Parameters)
                DumpSignatureParameter(writer, p);
            writer.Indent--;
        }

        private static void DumpMetadataModifiedType(IndentedTextWriter writer, MetadataModifiedType type)
        {
            writer.WriteLine("Modified");
            writer.Indent++;
            Dump(writer, type.UnmodifiedType);
            Dump(writer, type.CustomModifier.Type);
            writer.Indent--;
        }

        private static void DumpMetadataPinnedType(IndentedTextWriter writer, MetadataPinnedType type)
        {
            writer.WriteLine("Pinned");
            writer.Indent++;
            Dump(writer, type.ElementType);
            writer.Indent--;
        }

        private static void DumpMetadataPointerType(IndentedTextWriter writer, MetadataPointerType type)
        {
            writer.WriteLine("Pointer");
            writer.Indent++;
            Dump(writer, type.ElementType);
            writer.Indent--;
        }

        private static void DumpMetadataTypeParameterReference(IndentedTextWriter writer, MetadataTypeParameterReference type)
        {
            var syntax = type.IsType ? "!" : "!!";
            writer.WriteLine($"TypeParameterReference {syntax}{type.Index}");
        }

        private static void DumpSignatureParameter(IndentedTextWriter writer, MetadataSignatureParameter signatureParameter)
        {
            Dump(signatureParameter.ToRawType());
        }
    }
}