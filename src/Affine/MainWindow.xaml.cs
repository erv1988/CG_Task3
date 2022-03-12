using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace Affine
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ModelVisual3D modelVisual3D = null;

        Point3D[] pyramideVertices = new Point3D[]
        {
            new Point3D(0, 0, 1.22),
            new Point3D(0, 1.73, -0.4 ),
            new Point3D(-1, -0.577, -0.4),
            new Point3D(1, -0.577, -0.4),
        };
        int[] pyramidIndices = new int[]
        {
            0,1,3,
            0,3,2,
            0,2,1,
            1,2,3
        };

        PerspectiveCamera perspectiveCamera = new PerspectiveCamera(
            new Point3D(0, 0, 5),
            new Vector3D(0, 0, -1),
            new Vector3D(0, -1, 0),
            60);

        OrthographicCamera orthographicCamera = new OrthographicCamera(
            new Point3D(0, 0, 5),
            new Vector3D(0, 0, -1),
            new Vector3D(0, -1, 0),
            5);

        public MainWindow()
        {
            InitializeComponent();

            v3d.Camera = perspectiveCamera;

            objectPoints.Text = string.Join("\n", pyramideVertices.Select(x => string.Format("{0} {1} {2}", x.X, x.Y, x.Z)));
            objectIndices.Text = string.Join(" ", pyramidIndices.Select(x => x.ToString()));
        }


        public Point3D[] ReadPoints()
        {
            List<Point3D> points = new List<Point3D>();
            string[] lines = objectPoints.Text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string line in lines)
            {
                string[] pp = line.Trim().Split(new char[] { ' ' });
                points.Add(new Point3D(double.Parse(pp[0]), double.Parse(pp[1]), double.Parse(pp[2])));
            }
            return points.ToArray();
        }

        public int[] ReadIndices()
        {
            return objectIndices.Text
                .Split(new char[] { ' ', ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.Parse(x.Trim()))
                .ToArray();
        }


        void CreateLine(Point3D from, Point3D to, double thickness, out Point3D[] pts, out int[] idxs, int shiftIdx = 0)
        {
            Vector3D a = to - from;
            Vector3D b = a + new Vector3D(1, 1, 1);
            Vector3D n1 = Vector3D.CrossProduct(a, b);
            n1.Normalize();
            Vector3D n2 = Vector3D.CrossProduct(a, n1);
            n2.Normalize();
            n1 *= thickness/2;
            n2 *= thickness / 2;
            pts = new Point3D[]
            {
                from + n1,
                to + n1,
                from + n2,
                to + n2,
                from-n1,
                to-n1,
                from-n2,
                to-n2,
            };
            idxs = new int[] {
                2,1,0,
                3,1,2,
                4,3,2,
                5,3,4,
                6,5,4,
                7,5,6,
                0,7,6,
                1,7,0
                };
            for (int n = 0; n < idxs.Length; ++n)
                idxs[n] += shiftIdx;
        }

        GeometryModel3D CreateLine(Point3D from, Point3D to, double thickness, Brush brush)
        {
            Vector3D a = to - from;
            Vector3D b = a + new Vector3D(1, 1, 1);
            Vector3D n1 = Vector3D.CrossProduct(a, b);
            n1.Normalize();
            Vector3D n2 = Vector3D.CrossProduct(a, n1);
            n2.Normalize();
            n1 *= thickness / 2;
            n2 *= thickness / 2;
            var pts = new Point3D[]
            {
                from + n1,
                to + n1,
                from + n2,
                to + n2,
                from-n1,
                to-n1,
                from-n2,
                to-n2,
            };
            var idxs = new int[] {
                2,1,0,
                3,1,2,
                4,3,2,
                5,3,4,
                6,5,4,
                7,5,6,
                0,7,6,
                1,7,0
                };
            MeshGeometry3D mesh = new MeshGeometry3D()
            {
                Positions = new Point3DCollection(pts),
                TriangleIndices = new Int32Collection(idxs)
            };
            GeometryModel3D model = new GeometryModel3D(mesh, new DiffuseMaterial(brush));
            ModelVisual3D visual3D = new ModelVisual3D() { Content = model };
            return model;
        }


        public void Update3d()
        {
            try
            {
                Point3D[] points = ReadPoints();
                int[] indices = ReadIndices();
                Vector3D translate = new Vector3D(double.Parse(Tx.Text), double.Parse(Ty.Text), double.Parse(Tz.Text));
                double rotateAngle = double.Parse(Phi.Text) ;
                Vector3D rotateAxis = new Vector3D(double.Parse(Ax.Text), double.Parse(Ay.Text), double.Parse(Az.Text));
                Vector3D scale = new Vector3D(double.Parse(Sx.Text), double.Parse(Sy.Text), double.Parse(Sz.Text));
                Point3D cameraPos = new Point3D(double.Parse(Cx.Text), double.Parse(Cy.Text), double.Parse(Cz.Text));


                Random random = new Random(Environment.TickCount);

                // Обновление окна 3D
                {
                    v3d.BeginInit();
                    v3d.Children.Clear();

                    HashSet<string> dict = new HashSet<string>();
                    Model3DGroup model3DGroup = new Model3DGroup();


                    for (int i = 0; i < indices.Length;)
                    {
                        if (!dict.Contains(string.Format("{0}-{1}", i, i + 1)) && !dict.Contains(string.Format("{0}-{1}", i + 1, i)))
                        {
                            byte r = (byte)(random.Next(256));
                            byte g = (byte)(random.Next(256));
                            byte b = (byte)(512 - r - g);
                            Color c = Color.FromRgb(r, g, b);
                            var item = CreateLine(points[indices[i]], points[indices[i + 1]], 0.05, new SolidColorBrush(c));
                            dict.Add(string.Format("{0}-{1}", i, i + 1));
                            model3DGroup.Children.Add(item);
                        }

                        if (!dict.Contains(string.Format("{0}-{1}", i + 1, i + 2)) && !dict.Contains(string.Format("{0}-{1}", i + 2, i + 1)))
                        {
                            byte r = (byte)(random.Next(256));
                            byte g = (byte)(random.Next(256));
                            byte b = (byte)(512 - r - g);
                            Color c = Color.FromRgb(r, g, b);
                            var item = CreateLine(points[indices[i + 1]], points[indices[i + 2]], 0.05, new SolidColorBrush(c));
                            dict.Add(string.Format("{0}-{1}", i + 1, i + 2));
                            model3DGroup.Children.Add(item);
                        }

                        if (!dict.Contains(string.Format("{0}-{1}", i, i + 2)) && !dict.Contains(string.Format("{0}-{1}", i + 2, i)))
                        {
                            byte r = (byte)(random.Next(256));
                            byte g = (byte)(random.Next(256));
                            byte b = (byte)(512 - r - g);
                            Color c = Color.FromRgb(r, g, b);
                            var item = CreateLine(points[indices[i]], points[indices[i + 2]], 0.05, new SolidColorBrush(c));
                            dict.Add(string.Format("{0}-{1}", i, i + 2));
                            model3DGroup.Children.Add(item);
                        }
                        i += 3;
                    }

                    RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(rotateAxis, rotateAngle));
                    ScaleTransform3D scaleTransform = new ScaleTransform3D(scale);
                    TranslateTransform3D translateTransform = new TranslateTransform3D(translate);

                    Transform3DGroup transform3DGroup = new Transform3DGroup();
                    transform3DGroup.Children.Add(scaleTransform);
                    transform3DGroup.Children.Add(rotateTransform);
                    transform3DGroup.Children.Add(translateTransform);
                    model3DGroup.Transform = transform3DGroup;

                    v3d.Children.Add(new ModelVisual3D() { Content = model3DGroup });

                    Vector3D direction = new Vector3D(1, 1, 1);
                    if (rbProjectionOrtho.IsChecked.Value)
                    {
                        v3d.Camera = orthographicCamera;
                        orthographicCamera.Position = cameraPos;
                        orthographicCamera.LookDirection = new Point3D(0, 0, 0) - cameraPos;
                        if (cameraPos.X == 0 && cameraPos.Y == 0)
                            orthographicCamera.UpDirection = new Vector3D(0, 1, 0);
                        else
                            orthographicCamera.UpDirection = new Vector3D(0, 0, 1);
                        direction = orthographicCamera.LookDirection;
                    }
                    if (rbProjectionPersp.IsChecked.Value)
                    {
                        v3d.Camera = perspectiveCamera;
                        perspectiveCamera.Position = cameraPos;
                        perspectiveCamera.LookDirection = new Point3D(0, 0, 0) - cameraPos;
                        if (cameraPos.X == 0 && cameraPos.Y == 0)
                            perspectiveCamera.UpDirection = new Vector3D(0, 1, 0);
                        else
                            perspectiveCamera.UpDirection = new Vector3D(0, 0, 1);
                        direction = perspectiveCamera.LookDirection;
                    }

                    DirectionalLight light = new DirectionalLight(Colors.White, direction);
                    v3d.Children.Add(new ModelVisual3D() { Content = light });

                    v3d.EndInit();
                }

                // Обновление окна 2D
                {
                    // Сделана отрисовка для ортогональной проекции для камеры, установленной в положение "вид сверху"
                    // dir = (0,0,-1)
                    // необходимо добавить:
                    // - расчет матриц ортогональной и перспективной проекции относительно положения камеры
                    // - расчет матриц афинных преобразований: вращение вокруг оси, масштаб, перемещение
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void Update3DClick(object sender, RoutedEventArgs e)
        {
            Update3d();
        }

    }
}
