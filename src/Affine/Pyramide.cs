using System.Windows.Media.Media3D;

namespace Affine
{
    public static class Pyramide
    {
        public static Point3D[] pyramideVertices = new Point3D[]
        {
            new Point3D(0, 0, 1.22),
            new Point3D(0, 1.73, -0.4 ),
            new Point3D(-1, -0.577, -0.4),
            new Point3D(1, -0.577, -0.4),
        };
        public static int[] pyramidIndices = new int[]
        {
            0,1,3,
            0,3,2,
            0,2,1,
            1,2,3
        };
    }
}
