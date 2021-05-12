namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TE"></typeparam>
    public class Vertex<TE> : Vertex<Vertex<TE>, TE> where TE : Edge<Vertex<TE>, TE>
    {
        /// <summary>
        /// 
        /// </summary>
        public Vertex()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public Vertex(Vector3 p) : base(p)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    public class Edge<TV> : Edge<TV, Edge<TV>> where TV : Vertex<TV, Edge<TV>>
    {
        /// <summary>
        /// 
        /// </summary>
        public Edge()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        public Edge(TV s, TV e) : base(s, e)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Vertex : Vertex<Vertex, Edge>
    {
        /// <summary>
        /// 
        /// </summary>
        public Vertex()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public Vertex(Vector3 p) : base(p)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Edge : Edge<Vertex, Edge>
    {
        /// <summary>
        /// 
        /// </summary>
        public Edge()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        public Edge(Vertex s, Vertex e) : base(s, e)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataVertex<T> : Vertex<DataVertex<T>, Edge<DataVertex<T>>>
    {
        /// <summary>
        /// 
        /// </summary>
        public DataVertex()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public DataVertex(Vector3 p) : base(p)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataEdge<T> : Edge<Vertex<DataEdge<T>>, DataEdge<T>>
    {
        /// <summary>
        /// 
        /// </summary>
        public DataEdge()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        public DataEdge(Vertex<DataEdge<T>> s, Vertex<DataEdge<T>> e) : base(s, e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    /// <typeparam name="TE"></typeparam>
    public class DataVertex<TV, TE> : Vertex<DataVertex<TV, TE>, DataEdge<TV, TE>>
    {
        /// <summary>
        /// 
        /// </summary>
        public DataVertex()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public DataVertex(Vector3 p) : base(p)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public TV Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    /// <typeparam name="TE"></typeparam>
    public class DataEdge<TV, TE> : Edge<DataVertex<TV, TE>, DataEdge<TV, TE>>
    {
        /// <summary>
        /// 
        /// </summary>
        public DataEdge()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        public DataEdge(DataVertex<TV, TE> s, DataVertex<TV, TE> e) : base(s, e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public TE Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Graph : Graph<Vertex, Edge>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <returns></returns>
        public static Graph<DataVertex<TV, TE>, DataEdge<TV, TE>> WithData<TV, TE>()
        {
            return new();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Graph<DataVertex<T>, Edge<DataVertex<T>>> WithVertexData<T>()
        {
            return new();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Graph<Vertex<DataEdge<T>>, DataEdge<T>> WithEdgeData<T>()
        {
            return new();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TE"></typeparam>
        /// <returns></returns>
        public static Graph<Vertex<TE>, TE> WithEdge<TE>() where TE : Edge<Vertex<TE>, TE>, new()
        {
            return new();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        public static Graph<TV, Edge<TV>> WithVertex<TV>() where TV : Vertex<TV, Edge<TV>>, new()
        {
            return new();
        }
    }
}
