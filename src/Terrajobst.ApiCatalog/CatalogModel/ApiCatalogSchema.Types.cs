using System.Diagnostics;

namespace Terrajobst.ApiCatalog;

internal static partial class ApiCatalogSchema
{
    internal delegate ReadOnlySpan<byte> MemorySelector(ApiCatalogModel catalog);

    internal abstract class Layout
    {
        public abstract int Size { get; }
    }

    internal abstract class StructureLayout : Layout
    {
    }

    internal abstract class TableLayout : Layout
    {
    }

    internal sealed class LayoutBuilder
    {
        private readonly MemorySelector _memorySelector;
        private Field? _lastField;

        public LayoutBuilder(MemorySelector memorySelector)
        {
            _memorySelector = memorySelector;
        }

        public int Size => _lastField?.End ?? 0;

        private T Remember<T>(T field)
            where T: Field
        {
            _lastField = field;
            return field;
        }

        public Field<Guid> DefineGuid()
        {
            return Remember(new GuidField(_memorySelector, Size));
        }

        public Field<int> DefineInt32()
        {
            return Remember(new Int32Field(_memorySelector, Size));
        }

        public Field<bool> DefineBoolean()
        {
            return Remember(new BooleanField(_memorySelector, Size));
        }

        public Field<string> DefineString()
        {
            return Remember(new StringField(_memorySelector, Size));
        }

        public Field<ApiKind> DefineApiKind()
        {
            return Remember(new ApiKindField(_memorySelector, Size));
        }

        public Field<FrameworkModel> DefineFramework()
        {
            return Remember(new FrameworkField(_memorySelector, Size));
        }

        public Field<PackageModel> DefinePackage()
        {
            return Remember(new PackageField(_memorySelector, Size));
        }

        public Field<AssemblyModel> DefineAssembly()
        {
            return Remember(new AssemblyField(_memorySelector, Size));
        }

        public Field<ApiModel> DefineApi()
        {
            return Remember(new ApiField(_memorySelector, Size));
        }

        public Field<ApiModel?> DefineOptionalApi()
        {
            return Remember(new OptionalApiField(_memorySelector, Size));
        }

        public Field<ArrayEnumerator<T>> DefineArray<T>(Field<T> field)
            where T: notnull
        {
            return Remember(new ArrayField<T>(_memorySelector, Size, field));
        }

        public Field<ArrayOfStructuresEnumerator<T>> DefineArray<T>(T layout)
            where T: StructureLayout
        {
            return Remember(new ArrayOfStructuresField<T>(_memorySelector, Size, layout));
        }
    }

    public abstract class Field(int start)
    {
        public int Start { get; } = start;

        public abstract int Length { get; }

        public int End => Start + Length;
    }

    public abstract class Field<T>(MemorySelector memorySelector, int start)
        : Field(start)
    {
        protected MemorySelector MemorySelector { get; } = memorySelector;

        public T Read(ApiCatalogModel catalog, int rowOffset)
        {
            var memory = MemorySelector(catalog);
            var offset = rowOffset + Start;
            return Read(catalog, memory, offset);
        }

        protected abstract T Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset);
    }

    public sealed class GuidField(MemorySelector memorySelector, int start)
        : Field<Guid>(memorySelector, start)
    {
        public override int Length => 16;

        protected override Guid Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadGuid(offset);
        }
    }

    public sealed class Int32Field(MemorySelector memorySelector, int start)
        : Field<int>(memorySelector, start)
    {
        public override int Length => 4;

        protected override int Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadInt32(offset);
        }
    }

    public sealed class BooleanField(MemorySelector memorySelector, int start)
        : Field<bool>(memorySelector, start)
    {
        public override int Length => 1;

        protected override bool Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadByte(offset) == 1;
        }
    }

    public sealed class StringField(MemorySelector memorySelector, int start)
        : Field<string>(memorySelector, start)
    {
        public override int Length => 4;

        protected override string Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return catalog.GetString(handle);
        }
    }

    public sealed class ApiKindField(MemorySelector memorySelector, int start)
        : Field<ApiKind>(memorySelector, start)
    {
        public override int Length => 1;

        protected override ApiKind Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var value = memory.ReadByte(offset);
            return (ApiKind)value;
        }
    }

    public sealed class FrameworkField(MemorySelector memorySelector, int start)
        : Field<FrameworkModel>(memorySelector, start)
    {
        public FrameworkField() : this(c => c.BlobHeap, 0)
        {
        }

        public override int Length => 4;

        protected override FrameworkModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new FrameworkModel(catalog, handle);
        }
    }

    public sealed class PackageField(MemorySelector memorySelector, int start)
        : Field<PackageModel>(memorySelector, start)
    {
        public override int Length => 4;

        protected override PackageModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new PackageModel(catalog, handle);
        }
    }

    public sealed class AssemblyField(MemorySelector memorySelector, int start)
        : Field<AssemblyModel>(memorySelector, start)
    {
        public AssemblyField()
            : this(c => c.BlobHeap, 0)
        {
        }

        public override int Length => 4;

        protected override AssemblyModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new AssemblyModel(catalog, handle);
        }
    }

    public sealed class ApiField(MemorySelector memorySelector, int start)
        : Field<ApiModel>(memorySelector, start)
    {
        public ApiField() : this(c => c.BlobHeap, 0)
        {
        }

        public override int Length => 4;

        protected override ApiModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new ApiModel(catalog, handle);
        }
    }

    public sealed class OptionalApiField(MemorySelector memorySelector, int start)
        : Field<ApiModel?>(memorySelector, start)
    {
        public override int Length => 4;

        protected override ApiModel? Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            if (handle == -1)
                return null;

            return new ApiModel(catalog, handle);
        }
    }

    public sealed class ArrayField<T>(MemorySelector memorySelector, int start, Field<T> elementDefinition)
        : Field<ArrayEnumerator<T>>(memorySelector, start)
        where T: notnull
    {
        public override int Length => 4;

        protected override ArrayEnumerator<T> Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return new ArrayEnumerator<T>(catalog, elementDefinition, MemorySelector, offset);
        }
    }

    public sealed class ArrayOfStructuresField<T>(MemorySelector memorySelector, int start, T layout)
        : Field<ArrayOfStructuresEnumerator<T>>(memorySelector, start)
        where T: StructureLayout
    {
        public override int Length => 4;

        protected override ArrayOfStructuresEnumerator<T> Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return new ArrayOfStructuresEnumerator<T>(catalog, layout, MemorySelector, offset);
        }
    }

    internal struct TableRowEnumerator
    {
        private readonly int _rowSize;
        private int _count;
        private int _offset;

        public TableRowEnumerator(ReadOnlySpan<byte> table, int rowSize)
        {
            _rowSize = rowSize;

            var rowCount = table.Length / rowSize;
            Debug.Assert(table.Length % rowSize == 0);

            _count = -rowCount;
            _offset = 0;
        }

        public bool MoveNext()
        {
            if (_count == 0)
                return false;

            if (_count < 0)
            {
                _count = -_count;
                return true;
            }

            _offset += _rowSize;
            _count--;
            return _count > 0;
        }

        public int Current
        {
            get
            {
                return _offset;
            }
        }
    }

    internal struct ArrayEnumerator<T>
        where T: notnull
    {
        private readonly ApiCatalogModel _catalog;
        private readonly Field<T> _arrayElement;
        private int _count;
        private int _offset;

        public ArrayEnumerator(ApiCatalogModel catalog,
                               Field<T> arrayElement,
                               MemorySelector memorySelector,
                               int offset)
        {
            _catalog = catalog;
            _arrayElement = arrayElement;

            var blobOffset = memorySelector(catalog).ReadInt32(offset);
            if (blobOffset >= 0)
            {
                _count = -catalog.BlobHeap.ReadInt32(blobOffset);
                _offset = blobOffset + 4;
            }
        }

        public ApiCatalogModel Catalog
        {
            get { return _catalog; }
        }

        public bool MoveNext()
        {
            if (_count == 0)
                return false;

            if (_count < 0)
            {
                _count = -_count;
                return true;
            }

            _offset += _arrayElement.Length;
            _count--;
            return _count > 0;
        }

        public T Current
        {
            get
            {
                return _arrayElement.Read(_catalog, _offset);
            }
        }
    }

    internal struct ArrayOfStructuresEnumerator<T>
        where T: StructureLayout
    {
        private readonly ApiCatalogModel _catalog;
        private readonly T _layout;
        private int _count;
        private int _offset;

        public ArrayOfStructuresEnumerator(ApiCatalogModel catalog,
                                           T layout,
                                           MemorySelector memorySelector,
                                           int offset)
        {
            _catalog = catalog;
            _layout = layout;

            var blobOffset = memorySelector(catalog).ReadInt32(offset);
            if (blobOffset >= 0)
            {
                _count = -catalog.BlobHeap.ReadInt32(blobOffset);
                _offset = blobOffset + 4;
            }
        }

        public ApiCatalogModel Catalog
        {
            get { return _catalog; }
        }

        public T Layout
        {
            get { return _layout; }
        }

        public bool MoveNext()
        {
            if (_count == 0)
                return false;

            if (_count < 0)
            {
                _count = -_count;
                return true;
            }

            _offset += _layout.Size;
            _count--;
            return _count > 0;
        }

        public int Current
        {
            get
            {
                return _offset;
            }
        }
    }
}