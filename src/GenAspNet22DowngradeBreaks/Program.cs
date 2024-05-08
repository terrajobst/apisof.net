using System.Runtime.CompilerServices;

using Terrajobst.ApiCatalog;

var catalogPath = @"D:\Code\apisofdotnet\apicatalog.dat";
var csvOutputPath = @"P:\\apisof.net\\data\\aspnet22-downgrade-breaks.csv";
var datOutputPath = @"P:\\apisof.net\\data\\aspnet22-downgrade-breaks.dat";

Console.WriteLine("Loading catalog...");
var catalog = await ApiCatalogModel.LoadAsync(catalogPath);
var aspnet21 = catalog.Frameworks.Single(fx => fx.Name == "aspnet21").NuGetFramework;
var aspnet22 = catalog.Frameworks.Single(fx => fx.Name == "aspnet22").NuGetFramework;
var removals = DiffOptions.IncludeRemoved;

Console.WriteLine("Diffing...");

using var csvWriter = new CsvDiffWriter(csvOutputPath);
using var binaryWriter = new BinaryDiffWriter(datOutputPath);

foreach (var api in catalog.AllApis)
{
    var diffKind = api.GetDiffKind(aspnet22, aspnet21);
    if (diffKind is null || !diffKind.Value.IsIncluded(removals))
        continue;

    var declaration = api.GetDefinition(aspnet22)!.Value;
    if (declaration.IsOverride())
        continue;

    csvWriter.AddRemovedApi(api);
    binaryWriter.AddRemovedApi(api);
}

internal abstract class DiffWriter : IDisposable
{
    public abstract void AddRemovedApi(ApiModel api);

    public virtual void Dispose()
    {
    }
}

internal sealed class CsvDiffWriter : DiffWriter
{
    private readonly CsvWriter _writer;

    public CsvDiffWriter(string outputPath)
    {
        _writer = new CsvWriter(outputPath);
        _writer.Write("ID");
        _writer.Write("Namespace");
        _writer.Write("Type");
        _writer.Write("Member");
        _writer.WriteLine();
    }

    public override void Dispose()
    {
        _writer.Dispose();
    }

    public override void AddRemovedApi(ApiModel api)
    {
        var i = api.Guid.ToString("N");
        var n = api.GetNamespaceName();
        var t = api.GetTypeName();
        var m = api.GetMemberName();

        _writer.Write(i);
        _writer.Write(n);
        _writer.Write(t);
        _writer.Write(m);
        _writer.WriteLine();
    }
}

internal sealed class BinaryDiffWriter : DiffWriter
{
    private readonly string _path;
    private readonly Dictionary<string, int> _stringHeap = new(StringComparer.Ordinal);
    private readonly List<(Guid, int, int, int)> _apis = new();

    public BinaryDiffWriter(string path)
    {
        _path = path;
    }

    public override void AddRemovedApi(ApiModel api)
    {
        var g = api.Guid;
        var n = api.GetNamespaceName();
        var t = api.GetTypeName();
        var m = api.GetMemberName();
        var nIndex = GetStringIndex(n);
        var tIndex = GetStringIndex(t);
        var mIndex = GetStringIndex(m);

        _apis.Add((g, nIndex, tIndex, mIndex));
    }

    private int GetStringIndex(string text)
    {
        if (!_stringHeap.TryGetValue(text, out var index))
        {
            index = _stringHeap.Count;
            _stringHeap.Add(text, index);
        }

        return index;
    }

    public override void Dispose()
    {
        using var stream = File.Create(_path);
        using var writer = new BinaryWriter(stream);

        writer.Write(_stringHeap.Count);
        foreach (var (text, _) in _stringHeap.OrderBy(t => t.Value))
            writer.Write(text);

        writer.Write(_apis.Count);
        foreach (var (guid, nIndex, tIndex, mIndex) in _apis)
        {
            writer.Write(guid.ToByteArray());
            writer.Write(nIndex);
            writer.Write(tIndex);
            writer.Write(mIndex);
        }
    }
}