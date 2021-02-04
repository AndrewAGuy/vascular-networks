using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    public class SchreinerCost
    {
        //private struct PositionState
        //{
        //    public PositionState(Branch branch, double phi, double chi, double lambda)
        //    {
        //        this.Branch = branch;
        //        this.Phi = phi;
        //        this.Chi = chi;
        //        this.Lambda = lambda;
        //    }
        //    public Branch Branch { get; private set; }
        //    public double Phi { get; private set; }
        //    public double Chi { get; private set; }
        //    public double Lambda { get; private set; }
        //}

        public double Lambda { get; set; } = 1;
        public double Rho { get; set; } = 2;

        public Dictionary<Branch, double> EffectiveLengths { get; private set; } = new Dictionary<Branch, double>();
        //private Dictionary<Bifurcation, BifurcationData> bifurcations = new Dictionary<Bifurcation, BifurcationData>();
        public Branch Root { get; set; }

        public Dictionary<IMobileNode, Vector3> Gradient { get; private set; } = new Dictionary<IMobileNode, Vector3>();

        public void Update()
        {
            this.EffectiveLengths.Clear();
            //bifurcations.Clear();
            Update(this.Root);
        }

        private void Update(Branch branch)
        {
            var effectiveLength = Math.Pow(branch.Length, this.Lambda);
            foreach (var child in branch.Children)
            {
                Update(child);
                var fraction = child.Radius / branch.Radius;
                effectiveLength += this.EffectiveLengths[child] * Math.Pow(fraction, this.Rho);
            }
            this.EffectiveLengths[branch] = effectiveLength;

            //if (branch.End is Bifurcation bifurcation)
            //{
            //    bifurcations[bifurcation] = new BifurcationData(bifurcation);
            //}
        }

        public void Calculate()
        {
            this.Gradient.Clear();
            Calculate(this.Root, 1, 0, 0);
        }

        public bool Transients { get; set; }
        public Predicate<Branch> Terminate { get; set; } = b => false;

        private void Calculate(Branch branch, double F, double X, double L)
        {
            if (this.Terminate(branch))
            {
                return;
            }
            if (branch.End is Bifurcation bifurcation)
            {
                var data = new BifurcationData(bifurcation);
                var l1 = branch.Children[0].Length;
                var l2 = branch.Children[1].Length;
                var ls1 = this.EffectiveLengths[branch.Children[0]];
                var ls2 = this.EffectiveLengths[branch.Children[1]];
                var (f1, f2) = bifurcation.Fractions;
                var F1 = F * Math.Pow(f1, this.Rho);
                var F2 = F * Math.Pow(f2, this.Rho);
                var X11 = data.dRp_dR1 * X + this.Rho * Math.Pow(f1, this.Rho - 1) * data.df1_dR1;
                var X12 = data.dRp_dR2 * X + this.Rho * Math.Pow(f1, this.Rho - 1) * data.df1_dR2;
                var X21 = data.dRp_dR1 * X + this.Rho * Math.Pow(f2, this.Rho - 1) * data.df2_dR1;
                var X22 = data.dRp_dR2 * X + this.Rho * Math.Pow(f2, this.Rho - 1) * data.df2_dR2;
                var L1 = data.dRp_dR1 * L + Math.Pow(l1, this.Lambda) * F1 * X11 + ls2 * F2 * X21;
                var L2 = data.dRp_dR2 * L + Math.Pow(l2, this.Lambda) * F2 * X22 + ls1 * F1 * X12;

                var dRp_dx = 8 * branch.Network.Viscosity / Math.PI * (data.dLp_dx + data.dRp_dR1 * data.dL1_dx + data.dRp_dR2 * data.dL2_dx);
                this.Gradient[bifurcation] = F * this.Lambda * Math.Pow(branch.Length, this.Lambda - 1) * data.dLp_dx + L * dRp_dx
                    + F1 * this.Lambda * Math.Pow(l1, this.Lambda - 1) * data.dL1_dx + L1 * 8 * branch.Network.Viscosity / Math.PI * data.dL1_dx
                    + F2 * this.Lambda * Math.Pow(l2, this.Lambda - 1) * data.dL2_dx + L2 * 8 * branch.Network.Viscosity / Math.PI * data.dL2_dx;

                Calculate(branch.Children[0], F1, X11, L1);
                Calculate(branch.Children[1], F2, X22, L2);
            }

            if (this.Transients)
            {
                var m = F * this.Lambda * Math.Pow(branch.Length, this.Lambda - 1) + L * 8 * branch.Network.Viscosity / Math.PI;
                foreach (var t in branch.Transients)
                {
                    var transient = t as Transient;
                    var lp = transient.Parent.Length;
                    var dp = transient.Parent.Direction;
                    var lc = transient.Child.Length;
                    var dc = transient.Child.Direction;
                    var lg = dp / lp - dc / lc;
                    this.Gradient[transient] = m * lg;
                }
            }
        }

        private struct BifurcationData
        {
            public BifurcationData(Bifurcation bf)
            {
                var sr = bf.Network.Splitting;
                var bx = bf.Position;
                var px = bf.Upstream.Start.Position;
                var c1x = bf.Downstream[0].End.Position;
                var c2x = bf.Downstream[1].End.Position;
                this.Dp = bx - px;
                this.D1 = bx - c1x;
                this.D2 = bx - c2x;
                this.Lp = this.Dp.Length;
                this.L1 = this.D1.Length;
                this.L2 = this.D2.Length;
                this.dLp_dx = this.Dp / this.Lp;
                this.dL1_dx = this.D1 / this.L1;
                this.dL2_dx = this.D2 / this.L2;
                var v = bf.Network.Viscosity;
                var rs1 = 8 * v * this.L1 / Math.PI + bf.Downstream[0].End.ReducedResistance;
                var rs2 = 8 * v * this.L2 / Math.PI + bf.Downstream[1].End.ReducedResistance;
                (this.df1_dR1, this.df2_dR1) = sr.ReducedResistanceGradient(rs1, bf.Downstream[0].Flow, rs2, bf.Downstream[1].Flow);
                (this.df1_dR2, this.df2_dR2) = sr.ReducedResistanceGradient(rs2, bf.Downstream[1].Flow, rs1, bf.Downstream[0].Flow);
                var (f1, f2) = bf.Fractions;
                var u1 = Math.Pow(f1, 4) / rs1;
                var u2 = Math.Pow(f2, 4) / rs2;
                var u = u1 + u2;
                var dRp_du = -Math.Pow(u, -2);
                var du_df1 = 4 * u1 / f1;
                var du_dR1 = -u1 / rs1 + du_df1 * this.df1_dR1 + 4 * u2 / f2 * this.df2_dR1;
                var du_dR2 = -u2 / rs2 + du_df1 * this.df1_dR2 + 4 * u2 / f2 * this.df2_dR2;
                this.dRp_dR1 = dRp_du * du_dR1;
                this.dRp_dR2 = dRp_du * du_dR2;
            }
            public double dRp_dR1 { get; }
            public double df1_dR1 { get; }
            public double df2_dR1 { get; }
            public double dRp_dR2 { get; }
            public double df1_dR2 { get; }
            public double df2_dR2 { get; }
            public double Lp { get; }
            public double L1 { get; }
            public double L2 { get; }
            public Vector3 Dp { get; }
            public Vector3 D1 { get; }
            public Vector3 D2 { get; }
            public Vector3 dLp_dx { get; }
            public Vector3 dL1_dx { get; }
            public Vector3 dL2_dx { get; }
        }

        //public Dictionary<IMobileNode, Vector3> PositionGradient(Branch root, double lambda, double rho, bool transients = false)
        //{
        //    var gradients = new Dictionary<IMobileNode, Vector3>(Terminal.CountDownstream(root));
        //    var effectiveLengths = CalculateEffectiveLengths(root, lambda, rho);

        //    var states = new Stack<PositionState>();
        //    states.Push(new PositionState(root, 1, 0, 0));
        //    while (true)
        //    {

        //    }
        //}

        //public static Dictionary<Branch, double> CalculateEffectiveLengths(Branch root, double lambda, double rho)
        //{

        //}
    }
}
