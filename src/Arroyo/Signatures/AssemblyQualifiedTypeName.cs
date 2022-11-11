using System.Diagnostics;
using System.Text;

namespace Arroyo.Signatures;

// NOTE: Taken from Roslyn
internal readonly struct AssemblyQualifiedTypeName
{
    internal readonly string? AssemblyName;
    internal readonly string? TopLevelType;
    internal readonly string[]? NestedTypes;
    internal readonly AssemblyQualifiedTypeName[]? TypeArguments;
    internal readonly int PointerCount;
    internal readonly int[]? ArrayRanks;

    internal AssemblyQualifiedTypeName(string? topLevelType,
                                       string[]? nestedTypes,
                                       AssemblyQualifiedTypeName[]? typeArguments,
                                       int pointerCount,
                                       int[]? arrayRanks,
                                       string? assemblyName)
    {
        TopLevelType = topLevelType;
        NestedTypes = nestedTypes;
        TypeArguments = typeArguments;
        PointerCount = pointerCount;
        ArrayRanks = arrayRanks;
        AssemblyName = assemblyName;
    }

    internal static AssemblyQualifiedTypeName DecodeFromSerializedForm(string s)
    {
        var decoder = new SerializedTypeDecoder(s);
        return decoder.DecodeTypeName();
    }

    private struct SerializedTypeDecoder
    {
        public const char GenericTypeNameManglingChar = '`';

        private static readonly char[] s_typeNameDelimiters = { '+', ',', '[', ']', '*' };
        private readonly string _input;
        private int _offset;

        internal SerializedTypeDecoder(string s)
        {
            _input = s;
            _offset = 0;
        }

        private void Advance()
        {
            if (!EndOfInput)
            {
                _offset++;
            }
        }

        private void AdvanceTo(int i)
        {
            if (i <= _input.Length)
            {
                _offset = i;
            }
        }

        private bool EndOfInput
        {
            get
            {
                return _offset >= _input.Length;
            }
        }

        private char Current
        {
            get
            {
                return _input[_offset];
            }
        }

        /// <summary>
        /// Decodes a type name.  A type name is a string which is terminated by the end of the string or one of the
        /// delimiters '+', ',', '[', ']'. '+' separates nested classes. '[' and ']'
        /// enclosed generic type arguments.  ',' separates types.
        /// </summary>
        internal AssemblyQualifiedTypeName DecodeTypeName(bool isTypeArgument = false,
            bool isTypeArgumentWithAssemblyName = false)
        {
            Debug.Assert(!isTypeArgumentWithAssemblyName || isTypeArgument);

            string? topLevelType = null;
            List<string>? nestedTypesBuilder = null;
            AssemblyQualifiedTypeName[]? typeArguments = null;
            int pointerCount = 0;
            List<int>? arrayRanksBuilder = null;
            string? assemblyName = null;
            bool decodingTopLevelType = true;
            bool isGenericTypeName = false;

            StringBuilder typeNameBuilder = new StringBuilder();

            while (!EndOfInput)
            {
                int i = _input.IndexOfAny(s_typeNameDelimiters, _offset);
                if (i >= 0)
                {
                    char c = _input[i];

                    // Found name, which could be a generic name with arity.
                    // Generic type parameter count, if any, are handled in DecodeGenericName.
                    string decodedString = DecodeGenericName(i);
                    Debug.Assert(decodedString != null);

                    // Type name is generic if the decoded name of the top level type OR any of the outer types of a nested type had the '`' character.
                    isGenericTypeName =
                        isGenericTypeName || decodedString.IndexOf(GenericTypeNameManglingChar) >= 0;
                    typeNameBuilder.Append(decodedString);

                    switch (c)
                    {
                        case '*':
                            if (arrayRanksBuilder != null)
                            {
                                // Error case, array shape must be specified at the end of the type name.
                                // Process as a regular character and continue.
                                typeNameBuilder.Append(c);
                            }
                            else
                            {
                                pointerCount++;
                            }

                            Advance();
                            break;

                        case '+':
                            if (arrayRanksBuilder != null || pointerCount > 0)
                            {
                                // Error case, array shape must be specified at the end of the type name.
                                // Process as a regular character and continue.
                                typeNameBuilder.Append(c);
                            }
                            else
                            {
                                // Type followed by nested type. Handle nested class separator and collect the nested types.
                                HandleDecodedTypeName(typeNameBuilder.ToString(), decodingTopLevelType,
                                    ref topLevelType, ref nestedTypesBuilder);
                                typeNameBuilder.Clear();
                                decodingTopLevelType = false;
                            }

                            Advance();
                            break;

                        case '[':
                            // Is type followed by generic type arguments?
                            if (isGenericTypeName && typeArguments == null)
                            {
                                Advance();
                                if (arrayRanksBuilder != null || pointerCount > 0)
                                {
                                    // Error case, array shape must be specified at the end of the type name.
                                    // Process as a regular character and continue.
                                    typeNameBuilder.Append(c);
                                }
                                else
                                {
                                    // Decode type arguments.
                                    typeArguments = DecodeTypeArguments();
                                }
                            }
                            else
                            {
                                // Decode array shape.
                                DecodeArrayShape(typeNameBuilder, ref arrayRanksBuilder);
                            }

                            break;

                        case ']':
                            if (isTypeArgument)
                            {
                                // End of type arguments.  This occurs when the last type argument is a type in the
                                // current assembly.
                                goto ExitDecodeTypeName;
                            }
                            else
                            {
                                // Error case, process as a regular character and continue.
                                typeNameBuilder.Append(c);
                                Advance();
                                break;
                            }

                        case ',':
                            // A comma may separate a type name from its assembly name or a type argument from
                            // another type argument.
                            // If processing non-type argument or a type argument with assembly name,
                            // process the characters after the comma as an assembly name.
                            if (!isTypeArgument || isTypeArgumentWithAssemblyName)
                            {
                                Advance();
                                if (!EndOfInput && Char.IsWhiteSpace(Current))
                                {
                                    Advance();
                                }

                                assemblyName = DecodeAssemblyName(isTypeArgumentWithAssemblyName);
                            }

                            goto ExitDecodeTypeName;

                        default:
                            throw new Exception($"Unexpected character {c}");
                    }
                }
                else
                {
                    typeNameBuilder.Append(DecodeGenericName(_input.Length));
                    goto ExitDecodeTypeName;
                }
            }

        ExitDecodeTypeName:
            HandleDecodedTypeName(typeNameBuilder.ToString(), decodingTopLevelType, ref topLevelType,
                ref nestedTypesBuilder);

            return new AssemblyQualifiedTypeName(
                topLevelType,
                nestedTypesBuilder?.ToArray(),
                typeArguments,
                pointerCount,
                arrayRanksBuilder?.ToArray(),
                assemblyName);
        }

        private static void HandleDecodedTypeName(string decodedTypeName, bool decodingTopLevelType,
            ref string? topLevelType, ref List<string>? nestedTypesBuilder)
        {
            if (decodedTypeName.Length != 0)
            {
                if (decodingTopLevelType)
                {
                    Debug.Assert(topLevelType == null);
                    topLevelType = decodedTypeName;
                }
                else
                {
                    if (nestedTypesBuilder == null)
                    {
                        nestedTypesBuilder = new List<string>();
                    }

                    nestedTypesBuilder.Add(decodedTypeName);
                }
            }
        }

        /// <summary>
        /// Decodes a generic name.  This is a type name followed optionally by a type parameter count
        /// </summary>
        private string DecodeGenericName(int i)
        {
            Debug.Assert(i == _input.Length || s_typeNameDelimiters.Contains(_input[i]));

            var length = i - _offset;
            if (length == 0)
            {
                return String.Empty;
            }

            // Save start of name. The name should be the emitted name including the '`'  and arity.
            int start = _offset;
            AdvanceTo(i);

            // Get the emitted name.
            return _input.Substring(start, _offset - start);
        }

        private AssemblyQualifiedTypeName[]? DecodeTypeArguments()
        {
            if (EndOfInput)
            {
                return null;
            }

            var typeBuilder = new List<AssemblyQualifiedTypeName>();

            while (!EndOfInput)
            {
                typeBuilder.Add(DecodeTypeArgument());

                if (!EndOfInput)
                {
                    switch (Current)
                    {
                        case ',':
                            // More type arguments follow
                            Advance();
                            if (!EndOfInput && Char.IsWhiteSpace(Current))
                            {
                                Advance();
                            }

                            break;

                        case ']':
                            // End of type arguments
                            Advance();
                            return typeBuilder.ToArray();

                        default:
                            throw new Exception($"Unexpected value {EndOfInput}");
                    }
                }
            }

            return typeBuilder.ToArray();
        }

        private AssemblyQualifiedTypeName DecodeTypeArgument()
        {
            bool isTypeArgumentWithAssemblyName = false;
            if (Current == '[')
            {
                isTypeArgumentWithAssemblyName = true;
                Advance();
            }

            AssemblyQualifiedTypeName result = DecodeTypeName(isTypeArgument: true,
                isTypeArgumentWithAssemblyName: isTypeArgumentWithAssemblyName);

            if (isTypeArgumentWithAssemblyName)
            {
                if (!EndOfInput && Current == ']')
                {
                    Advance();
                }
            }

            return result;
        }

        private string? DecodeAssemblyName(bool isTypeArgumentWithAssemblyName)
        {
            if (EndOfInput)
            {
                return null;
            }

            int i;
            if (isTypeArgumentWithAssemblyName)
            {
                i = _input.IndexOf(']', _offset);
                if (i < 0)
                {
                    i = _input.Length;
                }
            }
            else
            {
                i = _input.Length;
            }

            string name = _input.Substring(_offset, i - _offset);
            AdvanceTo(i);
            return name;
        }

        /// <summary>
        /// Rank equal 0 is used to denote an SzArray, rank equal 1 denotes multi-dimensional array of rank 1.
        /// </summary>
        private void DecodeArrayShape(StringBuilder typeNameBuilder, ref List<int>? arrayRanksBuilder)
        {
            Debug.Assert(Current == '[');

            int start = _offset;
            int rank = 1;
            bool isMultiDimensionalIfRankOne = false;
            Advance();

            while (!EndOfInput)
            {
                switch (Current)
                {
                    case ',':
                        rank++;
                        Advance();
                        break;

                    case ']':
                        if (arrayRanksBuilder == null)
                        {
                            arrayRanksBuilder = new List<int>();
                        }

                        arrayRanksBuilder.Add(rank == 1 && !isMultiDimensionalIfRankOne ? 0 : rank);
                        Advance();
                        return;

                    case '*':
                        if (rank != 1)
                        {
                            goto default;
                        }

                        Advance();
                        if (Current != ']')
                        {
                            // Error case, process as regular characters
                            typeNameBuilder.Append(_input.Substring(start, _offset - start));
                            return;
                        }

                        isMultiDimensionalIfRankOne = true;
                        break;

                    default:
                        // Error case, process as regular characters
                        Advance();
                        typeNameBuilder.Append(_input.Substring(start, _offset - start));
                        return;
                }
            }

            // Error case, process as regular characters
            typeNameBuilder.Append(_input.Substring(start, _offset - start));
        }
    }
}