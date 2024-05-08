internal sealed class ApiEntry
{
    public ApiEntry(Guid guid, string namespaceName, string typeName, string memberName)
    {
        Guid = guid;
        NamespaceName = namespaceName;
        TypeName = typeName;
        MemberName = memberName;
    }

    public Guid Guid { get; }
    public string NamespaceName { get; }
    public string TypeName { get; }
    public string MemberName { get; }
}

internal sealed class ApiDatabase
{
    private Dictionary<Guid, ApiEntry> _apis;

    private ApiDatabase(Dictionary<Guid, ApiEntry> apis)
    {
        _apis = apis;
    }

    public ApiEntry? GetEntry(Guid guid)
    {
        _apis.TryGetValue(guid, out var result);
        return result;
    }

    public IEnumerable<ApiEntry> Entries => _apis.Values;

    public static ApiDatabase Load()
    {
        var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        using var stream = typeof(Program).Assembly.GetManifestResourceStream($"{appName}.aspnet22-downgrade-breaks.dat")!;
        using var reader = new BinaryReader(stream);

        var apis = new Dictionary<Guid, ApiEntry>();

        // Test
        var stringCount = reader.ReadInt32();
        var strings = new string[stringCount];
        for (var i = 0; i < strings.Length; i++)
            strings[i] = reader.ReadString();

        var apiCount = reader.ReadInt32();
        for (var i = 0; i < apiCount; i++)
        {
            var guidBytes = reader.ReadBytes(16);
            var nIndex = reader.ReadInt32();
            var tIndex = reader.ReadInt32();
            var mIndex = reader.ReadInt32();

            var guid = new Guid(guidBytes);
            var n = strings[nIndex];
            var t = strings[tIndex];
            var m = strings[mIndex];
            var api = new ApiEntry(guid, n, t, m);
            apis.Add(api.Guid, api);
        }

        return new ApiDatabase(apis);
    }
}
