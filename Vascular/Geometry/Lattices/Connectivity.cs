using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Lattices
{
    public static class Connectivity
    {
        public static Vector3[] CubeFaces
        {
            get
            {
                return new Vector3[6]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(0, 0, -1)
                };
            }
        }

        public static Vector3[] CubeFacesEdges
        {
            get
            {
                return new Vector3[18]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(0, 0, -1),
                    new Vector3(1, 1, 0),
                    new Vector3(1, 0, 1),
                    new Vector3(0, 1, 1),
                    new Vector3(1, -1, 0),
                    new Vector3(1, 0, -1),
                    new Vector3(0, 1, -1),
                    new Vector3(-1, -1, 0),
                    new Vector3(-1, 0, -1),
                    new Vector3(0, -1, -1),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, -1, 1)
                };
            }
        }

        public static Vector3[] CubeFacesEdgesVertices
        {
            get
            {
                return new Vector3[26]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(0, 0, -1),
                    new Vector3(1, 1, 0),
                    new Vector3(1, 0, 1),
                    new Vector3(0, 1, 1),
                    new Vector3(1, -1, 0),
                    new Vector3(1, 0, -1),
                    new Vector3(0, 1, -1),
                    new Vector3(-1, -1, 0),
                    new Vector3(-1, 0, -1),
                    new Vector3(0, -1, -1),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, -1, 1),
                    new Vector3(1, 1, 1),
                    new Vector3(-1, 1, 1),
                    new Vector3(1, -1, 1),
                    new Vector3(1, 1, -1),
                    new Vector3(-1, -1, -1),
                    new Vector3(1, -1, -1),
                    new Vector3(-1, 1, -1),
                    new Vector3(-1, -1, 1)
                };
            }
        }

        public static Vector3[] Tetrahedron
        {
            get
            {
                return new Vector3[12]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(1, -1, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, -1, 1),
                    new Vector3(0, 0, -1),
                    new Vector3(1, 0, -1),
                    new Vector3(0, 1, -1)
                };
            }
        }

        public static Vector3[] SquareEdges
        {
            get
            {
                return new Vector3[4]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0)
                };
            }
        }

        public static Vector3[] SquareEdgesVertices
        {
            get
            {
                return new Vector3[8]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(1, -1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, -1, 0)
                };
            }
        }

        public static Vector3[] Triangle
        {
            get
            {
                return new Vector3[6]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(1, -1, 0)
                };
            }
        }

        public static Vector3[] HexagonalPrismFaces
        {
            get
            {
                return new Vector3[8]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(1, -1, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, -1)
                };
            }
        }

        public static Vector3[] HexagonalPrismFacesEdges
        {
            get
            {
                return new Vector3[20]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(1, -1, 0),
                    new Vector3(1, 0, 1),
                    new Vector3(0, 1, 1),
                    new Vector3(-1, 1, 1),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, -1, 1),
                    new Vector3(1, -1, 1),
                    new Vector3(1, 0, -1),
                    new Vector3(0, 1, -1),
                    new Vector3(-1, 1, -1),
                    new Vector3(-1, 0, -1),
                    new Vector3(0, -1, -1),
                    new Vector3(1, -1, -1),
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, -1)
                };
            }
        }

        public static Vector3[] BodyCentredCubic
        {
            get
            {
                return new Vector3[14]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(-1, -1, 2),
                    new Vector3(1, 1, -2),
                    new Vector3(0, 0, 1),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, -1, 1),
                    new Vector3(-1, -1, 1),
                    new Vector3(0, 0, -1),
                    new Vector3(1, 0, -1),
                    new Vector3(0, 1, -1),
                    new Vector3(1, 1, -1)
                };
            }
        }
    }
}
