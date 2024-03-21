#nullable enable
using System.Collections;
using System.Timers;
using ApisOfDotNet.Services;
using Timer = System.Timers.Timer;

namespace ApisOfDotNet.Shared;

public sealed class FilteredView<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _source;
    private readonly Func<T, FilterText, bool> _predicate;
    private IReadOnlyList<T> _filteredSource;
    private string? _filter;

    public FilteredView(IEnumerable<T> source, Func<T, FilterText, bool> predicate, int max)
    {
        ThrowIfNull(source);
        ThrowIfNull(predicate);
        ThrowIfNegativeOrZero(max);
        
        _source = source;
        _predicate = predicate;
        Max = max;
        ApplyFilter();
    }

    public int Total => _filteredSource.Count;
    
    public bool Limit { get; set; }

    public int Max { get; }

    public bool HasMore => Limit && Total > Max;

    public string? Filter
    {
        get
        {
            return _filter;
        }
        set
        {
            if (_filter != value)
            {
                _filter = value;
                ApplyFilter();
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void ApplyFilter()
    {
        Limit = true;
        var filterText = FilterText.Create(_filter);
        _filteredSource = string.IsNullOrEmpty(_filter)
            ? _source.ToArray()
            : _source.Where(i => _predicate(i, filterText)).ToArray();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Limit
            ? _filteredSource.Take(Max).GetEnumerator()
            : _filteredSource.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public event EventHandler? FilterChanged;
}

public static class FilteredView
{
    public static FilteredView<T> ToFilteredView<T>(this IEnumerable<T> source, Func<T, FilterText, bool> predicate, int max = 50)
    {
        ThrowIfNull(source);
        ThrowIfNull(predicate);
        ThrowIfNegativeOrZero(max);

        return new FilteredView<T>(source, predicate, max);
    }

    public static void LinkFilterToQuery<T>(this FilteredView<T> filteredView, QueryManager queryManager, string parameterName = "q")
    {
        ThrowIfNull(filteredView);
        ThrowIfNull(queryManager);
        ThrowIfNullOrEmpty(parameterName);

        var filterTimer = new Timer {
            AutoReset = true,
            Interval = 150,
            Enabled = false
        };

        var queryFilter = queryManager.GetQueryParameter(parameterName);
        if (!string.IsNullOrEmpty(queryFilter))
            filteredView.Filter = queryFilter;

        filterTimer.Elapsed += FilterTimerOnElapsed;
        filteredView.FilterChanged += FilterHandler;
        queryManager.QueryChanged += QueryHandler;

        void FilterHandler(object? sender, EventArgs e)
        {
            filterTimer.Stop();
            filterTimer.Start();
        }

        void FilterTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            filterTimer.Stop();

            queryManager.QueryChanged -= QueryHandler;
            queryManager.SetQueryParameter(parameterName, filteredView.Filter);
            queryManager.QueryChanged += QueryHandler;
        }

        void QueryHandler(object? sender, IReadOnlySet<string> e)
        {
            if (!e.Contains(parameterName))
                return;

            filteredView.FilterChanged -= FilterHandler;
            filteredView.Filter = queryManager.GetQueryParameter(parameterName);
            filteredView.FilterChanged += FilterHandler;
        }
    }
}