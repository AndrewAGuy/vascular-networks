using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;

namespace Vascular.Structure
{
    /// <summary>
    /// Links <see cref="INode"/> together to form a tree.
    /// </summary>
    [DataContract]
    public class Segment : IAxialBoundable
    {
        [DataMember]
        private double radius = 0.0;

        /// <summary>
        /// 
        /// </summary>
        public Segment()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public Segment(INode start, INode end)
        {
            this.Start = start;
            this.End = end;
        }

        /// <summary>
        /// The <see cref="Structure.Branch"/> that this belongs to. 
        /// </summary>
        [DataMember]
        public Branch Branch { get; set; } = null;

        /// <summary>
        /// The node at the upstream end.
        /// </summary>
        [DataMember]
        public INode Start { get; set; }

        /// <summary>
        /// The node at the downstream end.
        /// </summary>
        [DataMember]
        public INode End { get; set; }

        /// <summary>
        /// Saves time recalculating.
        /// </summary>
        [DataMember]
        public double Length { get; private set; } = 0.0;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public AxialBounds Bounds { get; private set; } = new AxialBounds();

        /// <summary>
        /// Can be set independently of the branch radius if desired.
        /// </summary>
        public double Radius
        {
            get => radius;
            set
            {
                if (value >= 0.0)
                {
                    radius = value;
                }
            }
        }

        /// <summary>
        /// Pulls the branch radius in.
        /// </summary>
        public void UpdateRadius()
        {
            radius = this.Branch.Radius;
        }

        /// <summary>
        /// 
        /// </summary>
        public double Flow => this.Branch.Flow;

        /// <summary>
        /// 
        /// </summary>
        public void UpdateLength()
        {
            this.Length = Vector3.Distance(this.Start.Position, this.End.Position);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds GenerateBounds()
        {
            return this.Bounds = new AxialBounds(this);
        }

        /// <summary>
        /// Creates the bounds and extends by <paramref name="pad"/>.
        /// </summary>
        /// <param name="pad"></param>
        /// <returns></returns>
        public AxialBounds GenerateBounds(double pad)
        {
            return this.Bounds = new AxialBounds(this).Extend(pad);
        }

        /// <summary>
        /// 
        /// </summary>
        public double Slenderness => this.Length / this.Radius;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public Vector3 AtFraction(double f)
        {
            return (1 - f) * this.Start.Position + f * this.End.Position;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Direction => this.End.Position - this.Start.Position;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double DistanceToSurface(Vector3 v)
        {
            var bS = this.Start.Position;
            var bE = this.End.Position;
            var dir = bE - bS;
            // Get projection of v onto branch line
            var f = LinearAlgebra.LineFactor(bS, dir, v);
            if (f >= 1.0)
            {
                // Outside far end
                return Vector3.Distance(v, bE) - radius;
            }
            else if (f <= 0.0)
            {
                // Outside back end
                return Vector3.Distance(v, bS) - radius;
            }
            else
            {
                // Test cylindrically
                return Vector3.Distance(v, bS + f * dir) - radius;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return this.Bounds;
        }

        /// <summary>
        /// Creates an equivalent segment with dummy nodes and cloned positions, useful for cloning an immutable network.
        /// </summary>
        /// <returns></returns>
        public Segment MakeDummy()
        {
            return new Segment()
            {
                Start = new Dummy()
                {
                    Position = new Vector3(this.Start.Position)
                },
                End = new Dummy()
                {
                    Position = new Vector3(this.End.Position)
                },
                Radius = radius
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Segment MakeDummy(Vector3 a, Vector3 b, double r)
        {
            return new()
            {
                Start = new Dummy()
                {
                    Position = a
                },
                End = new Dummy()
                {
                    Position = b
                },
                Radius = r
            };
        }
    }
}
