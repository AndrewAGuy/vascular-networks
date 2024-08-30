using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;

namespace Vascular.Structure;

/// <summary>
/// Represents a collection of sources
/// </summary>
public class SourceCollection : IEnumerable<Source>
{
    private readonly List<Source> sources = new();
    private readonly Dictionary<Source, int> indices = new();

    private Network network = null!;

    /// <summary>
    ///
    /// </summary>
    public int Count => sources.Count;

    /// <summary>
    ///
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public Source this[int i]
    {
        get => sources[i];
        set
        {
            sources[i] = value;
            indices[value] = i;
            value.SetNetwork(network);
        }
    }

    /// <summary>
    /// Gets the index associated with the specified source.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public int this[Source s] => indices[s];

    /// <summary>
    ///
    /// </summary>
    public SourceCollection()
    {
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <param name="S"></param>
    public SourceCollection(Network n, IEnumerable<Source> S)
    {
        Initialize(n, S);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="S"></param>
    public SourceCollection(IEnumerable<Source> S)
    {
        sources = new(S);
        indices = Enumerable
            .Range(0, sources.Count)
            .ToDictionary(i => sources[i], i => i);
    }

    internal void SetNetwork(Network n)
    {
        network = n;
        foreach (var s in sources)
        {
            s.SetNetwork(n);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="s"></param>
    public void Add(Source s)
    {
        indices[s] = sources.Count;
        sources.Add(s);
        s.SetNetwork(network);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="S"></param>
    public void Add(IEnumerable<Source> S)
    {
        foreach (var s in S)
        {
            Add(s);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <param name="S"></param>
    public void Initialize(Network n, IEnumerable<Source> S)
    {
        indices.Clear();
        sources.Clear();
        network = n;
        Add(S);
    }

    /// <inheritdoc/>
    public IEnumerator<Source> GetEnumerator() => sources.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => sources.GetEnumerator();
}
