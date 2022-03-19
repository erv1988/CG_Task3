using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using D = System.Drawing;

// Про матрицы подробно: http://www.opengl-tutorial.org/ru/beginners-tutorials/tutorial-3-matrices/ 


namespace Affine
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

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

            objectPoints.Text = string.Join("\n", Pyramide.pyramideVertices.Select(x => string.Format("{0} {1} {2}", x.X, x.Y, x.Z)));
            objectIndices.Text = string.Join(" ", Pyramide.pyramidIndices.Select(x => x.ToString()));
        }


        #region Данные с формы

        /// <summary>
        /// Координаты точек фигуры
        /// </summary>
        Point3D[] Points { get { return ReadPoints(); } }

        /// <summary>
        /// Индексы граней фигуры
        /// </summary>
        int[] Indices { get { return ReadIndices(); } }

        /// <summary>
        /// Массив индексов для отображений линий без повторов
        /// </summary>
        int[][] LineIndices
        {
            get
            {
                List<int[]> lineIndices = new List<int[]>();
                HashSet<string> lines = new HashSet<string>();
                var indices = Indices;
                for (int i = 0; i < indices.Length;)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        var s = indices[i + j];
                        var t = indices[i + ((1 + j) % 3)];
                        var u = Math.Min(s, t);
                        var v = Math.Max(s, t);
                        string key = String.Format("{0}-{1}", u, v);
                        if (!lines.Contains(key))
                        {
                            lines.Add(key);
                            lineIndices.Add(new int[] { u, v });
                        }
                    }
                    i += 3;
                }
                return lineIndices.ToArray();
            }
        }

        /// <summary>
        /// Вектор смещения
        /// </summary>
        Vector3D Translate
        {
            get
            {
                return new Vector3D(
                    double.Parse(Tx.Text.Replace('.', ',')),
                    double.Parse(Ty.Text.Replace('.', ',')),
                    double.Parse(Tz.Text.Replace('.', ',')));
            }
        }

        /// <summary>
        /// Угол вращения
        /// </summary>
        double RotateAngle { get { return double.Parse(Phi.Text); } }

        /// <summary>
        /// Ось вращения
        /// </summary>
        Vector3D RotateAxis
        {
            get
            {
                return new Vector3D(
                    double.Parse(Ax.Text.Replace('.', ',')),
                    double.Parse(Ay.Text.Replace('.', ',')),
                    double.Parse(Az.Text.Replace('.', ',')));
            }
        }

        /// <summary>
        /// Вектор масштаба
        /// </summary>
        Vector3D Scale
        {
            get
            {
                return new Vector3D(
                    double.Parse(Sx.Text.Replace('.', ',')), 
                    double.Parse(Sy.Text.Replace('.', ',')), 
                    double.Parse(Sz.Text.Replace('.', ',')));
            }
        }

        /// <summary>
        /// Вектор позиции камеры
        /// </summary>
        Point3D CameraPos
        {
            get
            {
                return new Point3D(
                    double.Parse(Cx.Text.Replace('.', ',')), 
                    double.Parse(Cy.Text.Replace('.', ',')),
                    double.Parse(Cz.Text.Replace('.', ',')));
            }
        }


        /// <summary>
        /// Матрица вида
        /// см. 
        /// </summary>
        private Matrix3D ViewMatrix
        {
            get
            {
                Vector3D up;
                if (CameraPos.X == 0 && CameraPos.Y == 0)
                    up = new Vector3D(0, 1, 0);
                else
                    up = new Vector3D(0, 0, 1);

                return CalcViewMatrix(((Vector3D)CameraPos), new Point3D(0, 0, 0) - CameraPos, up);
            }
        }


        #endregion


        private void update_2d()
        {
            // Обновление окна 2D
            // Сделана отрисовка для ортогональной проекции для камеры, установленной в положение "вид сверху"
            // dir = (0,0,-1)
            // необходимо добавить:
            // - расчет матриц ортогональной и перспективной проекции относительно положения камеры
            // - расчет матриц афинных преобразований: вращение вокруг оси, масштаб, перемещение

            var points = Points;
            var lineIndices = LineIndices;

            int dpi = 300;


            D.Bitmap bitmap = new D.Bitmap(1024, 1024, D.Imaging.PixelFormat.Format24bppRgb);
            using (D.Graphics gfx = D.Graphics.FromImage(bitmap))
            {
                gfx.FillRectangle(D.Brushes.White, 0, 0, bitmap.Width, bitmap.Height);


                /* Пример ортогональной матрицы, соответсвующей
                    OrthographicCamera orthographicCamera = new OrthographicCamera(
                        new Point3D(0, 0, 5),     // ViewMatrix
                        new Vector3D(0, 0, -1),   // ViewMatrix
                        new Vector3D(0, -1, 0),   // ViewMatrix
                        5);                       // ortho

                 */
                Matrix3D viewMatrix = ViewMatrix;
                Matrix3D ortho = new Matrix3D(
                    1.0 / 5, 0, 0, 0,
                    0, 1.0 / 5, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 1);



                Matrix3D proj = viewMatrix * ortho;


                Random random = new Random(Environment.TickCount);
                foreach (var ii in lineIndices)
                {
                    byte r = (byte)(random.Next(256));
                    byte g = (byte)(random.Next(256));
                    byte b = (byte)(512 - r - g);
                    D.Color c = D.Color.FromArgb(r, g, b);

                    var pointA = points[ii[0]];
                    var pointB = points[ii[1]];

                    var pA = proj.Transform(pointA);
                    var pB = proj.Transform(pointB);

                    var x1 = (int)((0.5 + pA.X) * bitmap.Width);
                    var y1 = (int)((0.5 - pA.Y) * bitmap.Height);
                    var x2 = (int)((0.5 + pB.X) * bitmap.Width);
                    var y2 = (int)((0.5 - pB.Y) * bitmap.Height);

                    gfx.DrawLine(new D.Pen(c, 3), new D.Point(x1, y1), new D.Point(x2, y2));

                }
            }
            i3d.Source = GetImageSource(bitmap);
        }


        #region service


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

        private void update_3d()
        {
            Random random = new Random(Environment.TickCount);

            // Обновление окна 3D
            {
                v3d.BeginInit();
                v3d.Children.Clear();

                Model3DGroup model3DGroup = new Model3DGroup();

                var lineIndices = LineIndices;
                var points = Points;
                foreach (var ii in lineIndices)
                {
                    byte r = (byte)(random.Next(256));
                    byte g = (byte)(random.Next(256));
                    byte b = (byte)(512 - r - g);
                    Color c = Color.FromRgb(r, g, b);
                    var item = CreateLine(Points[ii[0]], Points[ii[1]], 0.05, new SolidColorBrush(c));
                    model3DGroup.Children.Add(item);
                }

                RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(RotateAxis, RotateAngle));
                ScaleTransform3D scaleTransform = new ScaleTransform3D(Scale);
                TranslateTransform3D translateTransform = new TranslateTransform3D(Translate);

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
                    orthographicCamera.Position = CameraPos;
                    orthographicCamera.LookDirection = new Point3D(0, 0, 0) - CameraPos;
                    if (CameraPos.X == 0 && CameraPos.Y == 0)
                        orthographicCamera.UpDirection = new Vector3D(0, 1, 0);
                    else
                        orthographicCamera.UpDirection = new Vector3D(0, 0, 1);
                    direction = orthographicCamera.LookDirection;
                }
                if (rbProjectionPersp.IsChecked.Value)
                {
                    v3d.Camera = perspectiveCamera;
                    perspectiveCamera.Position = CameraPos;
                    perspectiveCamera.LookDirection = new Point3D(0, 0, 0) - CameraPos;
                    if (CameraPos.X == 0 && CameraPos.Y == 0)
                        perspectiveCamera.UpDirection = new Vector3D(0, 1, 0);
                    else
                        perspectiveCamera.UpDirection = new Vector3D(0, 0, 1);
                    direction = perspectiveCamera.LookDirection;
                }

                DirectionalLight light = new DirectionalLight(Colors.White, direction);
                v3d.Children.Add(new ModelVisual3D() { Content = light });

                v3d.EndInit();
            }
        }

        public static System.Windows.Media.ImageSource GetImageSource(System.Drawing.Image bitmap)
        {
            if (bitmap == null)
                return null;
            MemoryStream memStream = new MemoryStream();
            bitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnDemand;
            image.StreamSource = memStream;
            image.EndInit();

            return image;
        }


        // Создание матрицы вида
        private Matrix3D CalcViewMatrix(Vector3D position, Vector3D look, Vector3D up)
        {
            // Ось 0Z - направлена назад
            Vector3D zaxis = -look;
            zaxis.Normalize();

            // Ось 0X - направлена вправо
            Vector3D xaxis = Vector3D.CrossProduct(up, zaxis);
            xaxis.Normalize();

            // Ось OY - направлена вниз
            Vector3D yaxis = Vector3D.CrossProduct(zaxis, xaxis);

            // Проецируем положение наблюдателя на оси новой системы координат
            Vector3D positionVec = (Vector3D)position;
            double cx = -Vector3D.DotProduct(xaxis, positionVec);
            double cy = -Vector3D.DotProduct(yaxis, positionVec);
            double cz = -Vector3D.DotProduct(zaxis, positionVec);

            // заполняем матрицу
            Matrix3D viewMatrix = new Matrix3D(
                xaxis.X, yaxis.X, zaxis.X, 0,
                xaxis.Y, yaxis.Y, zaxis.Y, 0,
                xaxis.Z, yaxis.Z, zaxis.Z, 0,
                cx, cy, cz, 1);

            return viewMatrix;
        }



        private void Update3DClick(object sender, RoutedEventArgs e)
        {
            update_3d();
            update_2d();
        }

        #endregion
    }
}
