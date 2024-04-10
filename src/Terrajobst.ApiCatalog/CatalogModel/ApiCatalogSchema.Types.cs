using System.Diagnostics;

namespace Terrajobst.ApiCatalog;

internal static partial class ApiCatalogSchema
{
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
        private readonly ApiCatalogHeapOrTable _heapOrTable;
        private Field? _lastField;

        public LayoutBuilder(ApiCatalogHeapOrTable heapOrTable)
        {
            _heapOrTable = heapOrTable;
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
            return Remember(new GuidField(_heapOrTable, Size));
        }

        public Field<int> DefineInt32()
        {
            return Remember(new Int32Field(_heapOrTable, Size));
        }

        public Field<float> DefineSingle()
        {
            return Remember(new SingleField(_heapOrTable, Size));
        }

        public Field<bool> DefineBoolean()
        {
            return Remember(new BooleanField(_heapOrTable, Size));
        }

        public Field<string> DefineString()
        {
            return Remember(new StringField(_heapOrTable, Size));
        }

        public Field<DateOnly> DefineDate()
        {
            return Remember(new DateField(_heapOrTable, Size));
        }

        public Field<ApiKind> DefineApiKind()
        {
            return Remember(new ApiKindField(_heapOrTable, Size));
        }

        public Field<FrameworkModel> DefineFramework()
        {
            return Remember(new FrameworkField(_heapOrTable, Size));
        }

        public Field<PackageModel> DefinePackage()
        {
            return Remember(new PackageField(_heapOrTable, Size));
        }

        public Field<AssemblyModel> DefineAssembly()
        {
            return Remember(new AssemblyField(_heapOrTable, Size));
        }

        public Field<UsageSourceModel> DefineUsageSource()
        {
            return Remember(new UsageSourceField(_heapOrTable, Size));
        }

        public Field<ApiModel> DefineApi()
        {
            return Remember(new ApiField(_heapOrTable, Size));
        }

        public Field<ApiModel?> DefineOptionalApi()
        {
            return Remember(new OptionalApiField(_heapOrTable, Size));
        }

        public Field<ArrayEnumerator<T>> DefineArray<T>(Field<T> field)
            where T: notnull
        {
            return Remember(new ArrayField<T>(_heapOrTable, Size, field));
        }

        public Field<ArrayOfStructuresEnumerator<T>> DefineArray<T>(T layout)
            where T: StructureLayout
        {
            return Remember(new ArrayOfStructuresField<T>(_heapOrTable, Size, layout));
        }
    }

    public abstract class Field(int start)
    {
        public int Start { get; } = start;

        public abstract int Length { get; }

        public int End => Start + Length;
    }

    public abstract class Field<T>(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field(start)
    {
        protected ApiCatalogHeapOrTable HeapOrTable { get; } = heapOrTable;

        public T Read(ApiCatalogModel catalog, int rowIndex)
        {
            ThrowIfNull(catalog);
            ThrowIfRowIndexOutOfRange(catalog, HeapOrTable, rowIndex);

            catalog.GetMemory(HeapOrTable, out var memory, out var rowSize);

            var offset = rowIndex * rowSize + Start;
            return Read(catalog, memory, offset);
        }

        protected abstract T Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset);
    }

    public sealed class GuidField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<Guid>(heapOrTable, start)
    {
        public override int Length => 16;

        protected override Guid Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadGuid(offset);
        }
    }

    public sealed class SingleField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<float>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override float Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadSingle(offset);
        }
    }

    public sealed class Int32Field(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<int>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override int Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadInt32(offset);
        }
    }

    public sealed class BooleanField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<bool>(heapOrTable, start)
    {
        public override int Length => 1;

        protected override bool Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return memory.ReadByte(offset) == 1;
        }
    }

    public sealed class StringField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<string>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override string Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var stringOffset = memory.ReadInt32(offset);
            return catalog.GetString(stringOffset);
        }
    }

    public sealed class DateField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<DateOnly>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override DateOnly Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var dayNumber = memory.ReadInt32(offset);
            return DateOnly.FromDayNumber(dayNumber);
        }
    }

    public sealed class ApiKindField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<ApiKind>(heapOrTable, start)
    {
        public override int Length => 1;

        protected override ApiKind Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var value = memory.ReadByte(offset);
            return (ApiKind)value;
        }
    }

    public sealed class FrameworkField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<FrameworkModel>(heapOrTable, start)
    {
        public FrameworkField() : this(ApiCatalogHeapOrTable.BlobHeap, 0)
        {
        }

        public override int Length => 4;

        protected override FrameworkModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new FrameworkModel(catalog, handle);
        }
    }

    public sealed class PackageField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<PackageModel>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override PackageModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new PackageModel(catalog, handle);
        }
    }

    public sealed class AssemblyField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<AssemblyModel>(heapOrTable, start)
    {
        public AssemblyField()
            : this(ApiCatalogHeapOrTable.BlobHeap, 0)
        {
        }

        public override int Length => 4;

        protected override AssemblyModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new AssemblyModel(catalog, handle);
        }
    }

    public sealed class UsageSourceField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<UsageSourceModel>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override UsageSourceModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new UsageSourceModel(catalog, handle);
        }
    }

    public sealed class ApiField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<ApiModel>(heapOrTable, start)
    {
        public ApiField() : this(ApiCatalogHeapOrTable.BlobHeap, 0)
        {
        }

        public override int Length => 4;

        protected override ApiModel Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var handle = memory.ReadInt32(offset);
            return new ApiModel(catalog, handle);
        }
    }

    public sealed class OptionalApiField(ApiCatalogHeapOrTable heapOrTable, int start)
        : Field<ApiModel?>(heapOrTable, start)
    {
        public override int Length => 4;

        protected override ApiModel? Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            var apiOffset = memory.ReadInt32(offset);
            if (apiOffset == -1)
                return null;

            return new ApiModel(catalog, apiOffset);
        }
    }

    public sealed class ArrayField<T>(ApiCatalogHeapOrTable heapOrTable, int start, Field<T> elementDefinition)
        : Field<ArrayEnumerator<T>>(heapOrTable, start)
        where T: notnull
    {
        public override int Length => 4;

        protected override ArrayEnumerator<T> Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return new ArrayEnumerator<T>(catalog, elementDefinition, HeapOrTable, offset);
        }
    }

    public sealed class ArrayOfStructuresField<T>(ApiCatalogHeapOrTable heapOrTable, int start, T layout)
        : Field<ArrayOfStructuresEnumerator<T>>(heapOrTable, start)
        where T: StructureLayout
    {
        public override int Length => 4;

        protected override ArrayOfStructuresEnumerator<T> Read(ApiCatalogModel catalog, ReadOnlySpan<byte> memory, int offset)
        {
            return new ArrayOfStructuresEnumerator<T>(catalog, layout, HeapOrTable, offset);
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
                return _offset / _rowSize;
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
                               ApiCatalogHeapOrTable heapOrTable,
                               int offset)
        {
            _catalog = catalog;
            _arrayElement = arrayElement;

            catalog.GetMemory(heapOrTable, out var memory, out _);
            
            var blobOffset = memory.ReadInt32(offset);
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
                                           ApiCatalogHeapOrTable heapOrTable,
                                           int offset)
        {
            _catalog = catalog;
            _layout = layout;

            catalog.GetMemory(heapOrTable, out var memory, out _);
            
            var blobOffset = memory.ReadInt32(offset);
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