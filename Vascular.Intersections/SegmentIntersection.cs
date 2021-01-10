using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Structure;

namespace Vascular.Intersections
{
    public class SegmentIntersection
    {
        public SegmentIntersection(Segment a, Segment b, CubeGrayCode ng, double s2 = 1.0e-8, double t2 = 1.0e-12, BranchRelationship br = BranchRelationship.None)
        {
            this.RelationshipAB = br;
            this.A = a;
            this.B = b;
            var dirA = a.End.Position - a.Start.Position;
            var dirB = b.End.Position - b.Start.Position;
            var nor = dirA ^ dirB;
            // Since |nor| = |dirA||dirB|sin(-) the test for codirectionality is really a test of angle
            // This takes slightly longer to compute, but allows more flexibility in input lengths compared to just using |nor| < t
            var nor2 = nor.LengthSquared;
            var sin2 = nor2 / (dirA.LengthSquared * dirB.LengthSquared);
            if (sin2 < s2)
            {
                Indefinite(dirA, dirB, ng, t2);
            }
            else
            {
                Definite(dirA, dirB, nor / Math.Sqrt(nor2));
            }
            if (this.Intersecting)
            {
                this.Overlap = this.A.Radius + this.B.Radius - Math.Sqrt(this.Distance2);
            }
        }

        public Segment A { get; private set; }
        public Segment B { get; private set; }
        public bool Indeterminate { get; private set; }
        public bool Intersecting { get; private set; }
        public Vector3 NormalAB { get; private set; }
        public double Distance2 { get; private set; }
        public double Overlap { get; private set; }
        public Vector3 ClosestA { get; private set; }
        public Vector3 ClosestB { get; private set; }
        public double FractionA { get; private set; }
        public double FractionB { get; private set; }
        public double StartA { get; private set; }
        public double StartB { get; private set; }
        public double EndA { get; private set; }
        public double EndB { get; private set; }
        public BranchRelationship RelationshipAB { get; private set; }

        public BranchRelationship UpdateRelationshipDetail()
        {
            var branchA = this.A.Branch;
            var branchB = this.B.Branch;
            switch (this.RelationshipAB)
            {
                case BranchRelationship.Upstream:
                    if (ReferenceEquals(branchA.End, branchB.Start))
                    {
                        this.RelationshipAB = BranchRelationship.Parent;
                    }
                    break;
                case BranchRelationship.Downstream:
                    if (ReferenceEquals(branchA.Start, branchB.End))
                    {
                        this.RelationshipAB = BranchRelationship.Child;
                    }
                    break;
                case BranchRelationship.None:
                    if (ReferenceEquals(branchA.Start, branchB.Start))
                    {
                        this.RelationshipAB = BranchRelationship.Sibling;
                    }
                    break;
            }
            return this.RelationshipAB;
        }

        private void Indefinite(Vector3 dirA, Vector3 dirB, CubeGrayCode ng, double tol2)
        {
            this.Indeterminate = true;
            var aS = this.A.Start.Position;
            var aE = this.A.End.Position;
            var bS = this.B.Start.Position;
            var bE = this.B.End.Position;
            // Get point on A closest to B's start
            var (S, E) = LinearAlgebra.LineFactors(aS, dirA, bS, bE);
            var aToBS = bS - (aS + S * dirA);
            // Check distance between line segments, first chance to reject intersection
            var sep2 = aToBS.LengthSquared;
            var rT2 = Math.Pow(this.A.Radius + this.B.Radius, 2);
            if (sep2 > rT2)
            {
                this.Intersecting = false;
                return;
            }
            this.Distance2 = sep2;
            // If coaxial, generate randomly, otherwise pushing direction has already been found
            this.NormalAB = sep2 < tol2 ? ng.GenerateArbitraryNormal(dirA, tol2) : aToBS / Math.Sqrt(sep2);
            var rem2 = rT2 - sep2;
            var (s, e) = LinearAlgebra.LineFactors(bS, dirB, aS, aE);
            var deltaA = Math.Sqrt(rem2 / dirA.LengthSquared);
            var deltaB = Math.Sqrt(rem2 / dirB.LengthSquared);
            // Get the first and last possible length fractions of intersection on the infinite length lines
            this.StartA = Math.Min(S, E) - deltaA;
            this.EndA = Math.Max(S, E) + deltaA;
            this.StartB = Math.Min(s, e) - deltaB;
            this.EndB = Math.Max(s, e) + deltaB;
            // This case is never going to be the hot path, so all checks done here since performance loss is negligible
            this.Intersecting = this.StartA <= 1 && this.EndA >= 0 && this.StartB <= 1 && this.EndB >= 0;
        }

        private void Definite(Vector3 dirA, Vector3 dirB, Vector3 nor)
        {
            this.Indeterminate = false;
            // Solve c1 + a*d1 + r*n = c2 + b*d2, where n is normalised so |r| is true distance
            var aS = this.A.Start.Position;
            var aE = this.A.End.Position;
            var bS = this.B.Start.Position;
            var bE = this.B.End.Position;
            var s = LinearAlgebra.SolveMatrix3x3(dirA, -dirB, nor, bS - aS);
            this.NormalAB = s.z >= 0 ? nor : -nor;
            // If closest point of infinite lines lies outside segment, clamp either or both
            // If only one clamped, account for this by projecting nearest point onto other segment and clamping
            if (s.x > 1)
            {
                this.FractionA = 1;
                this.FractionB = s.y > 1 ? 1 : s.y < 0 ? 0 : LinearAlgebra.LineFactor(bS, dirB, aE).Clamp(0, 1);
            }
            else if (s.x < 0)
            {
                this.FractionA = 0;
                this.FractionB = s.y > 1 ? 1 : s.y < 0 ? 0 : LinearAlgebra.LineFactor(bS, dirB, aS).Clamp(0, 1);
            }
            else
            {
                if (s.y > 1)
                {
                    this.FractionB = 1;
                    this.FractionA = LinearAlgebra.LineFactor(aS, dirA, bE).Clamp(0, 1);
                }
                else if (s.y < 0)
                {
                    this.FractionB = 0;
                    this.FractionA = LinearAlgebra.LineFactor(aS, dirA, bS).Clamp(0, 1);
                }
                else
                {
                    this.FractionA = s.x;
                    this.FractionB = s.y;
                }
            }
            this.ClosestA = aS + this.FractionA * dirA;
            this.ClosestB = bS + this.FractionB * dirB;
            this.Distance2 = Vector3.DistanceSquared(this.ClosestA, this.ClosestB);
            var rT2 = Math.Pow(this.A.Radius + this.B.Radius, 2);
            this.Intersecting = this.Distance2 <= rT2;
        }
    }
}
