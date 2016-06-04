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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Timers;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media.Media3D;

namespace SolarSystemSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Simulator simulator = new Simulator();
        List<CelestialBody> bodies = new List<CelestialBody>();

        Dictionary<long, Ellipse> ellipses = new Dictionary<long, Ellipse>();

        private Vector3D camera = new Vector3D(0, -350E6, 60E6);
        private Vector3D cameraTarget = new Vector3D(0, 0, 0);
        private double fieldOfView = 70;
        private Point mouseOrigin;
        private long targetUniqueID = 0;

        private static Vector3D origin = new Vector3D(0, 0, 0);
        private static Vector3D unitVectorX = new Vector3D(1, 0, 0);
        private static Vector3D unitVectorY = new Vector3D(0, 1, 0);
        private static Vector3D unitVectorZ = new Vector3D(0, 0, 1);

        public const double AU = 149597870.7;

        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));

            Loaded += delegate
            {
                myCanvas.Background = Brushes.Black;

                bodies.Add(new CelestialBody("Sun", 1.98855E30, origin, origin, 695700, 20, Brushes.Yellow));
                bodies.Add(new CelestialBody("Earth", 5.97237E24, new Vector3D(149598023, 0, 0), new Vector3D(0, 29780, 0), 6371.0, 1E7, Brushes.Blue));
                bodies.Add(new CelestialBody("Moon", 7.342E22, new Vector3D(149598023 + 384399, 0, 0), new Vector3D(0, 29780 + 1022, 0), 1737.1, 1E7, Brushes.White));
                bodies.Add(new CelestialBody("Mercury", 3.3011E23, new Vector3D(57909050, 0, 0), new Vector3D(0, 47362, 0), 2439.7, 1E7, Brushes.Red));
                bodies.Add(new CelestialBody("Venus", 4.8675E24, new Vector3D(-108208E3, 0, 0), new Vector3D(0, -35020, 0), 6051.8, 1E7, Brushes.Green));
                bodies.Add(new CelestialBody("Mars", 6.4171E24, new Vector3D(0, 1.523679 * AU, 0), new Vector3D(-24077, 0, 0), 3389.5, 1E7, Brushes.OrangeRed));
                bodies.Add(new CelestialBody("Jupiter", 1.8986E27, new Vector3D(0, 5.20260 * AU, 0), new Vector3D(-13070, 0, 0), 71492, 1E6, Brushes.GhostWhite));
                bodies.Add(new CelestialBody("Europa", 5.799844E22, new Vector3D(0, 5.20260 * AU + 670900, 0), new Vector3D(-13070 - 13740, 0, 0), 1560.8, 1E6, Brushes.LightYellow));
                bodies.Add(new CelestialBody("Saturn", 5.6836E26, new Vector3D(9.554909 * AU, 0, 0), new Vector3D(0, 9690, 0), 58232, 1E6, Brushes.Gray));
                bodies.Add(new CelestialBody("Uranus", 8.6810E25, new Vector3D(0, -19.2184 * AU, 0), new Vector3D(6800, 0, 0), 25362, 1E8, Brushes.Cyan));
                bodies.Add(new CelestialBody("Neptune", 1.0243E26, new Vector3D(0, -30.110387 * AU, 0), new Vector3D(5430, 0, 0), 24622, 1E8, Brushes.Blue));


                /*
                PathFigure myPathFigure = new PathFigure();
                myPathFigure.StartPoint = new Point(100, 100);
                myPathFigure.Segments.Add(
                            new ArcSegment(new Point(100, 100), new Size(100, 100), 0, true, SweepDirection.Clockwise, true));

                myPathFigure.Segments.Add(
                    new ArcSegment(new Point(0, 0), new Size(100, 100), 0, false, SweepDirection.Clockwise, true));
                myPathFigure.Segments.Add(new LineSegment(new Point(150, 150), true));

                PathGeometry myPathGeometry = new PathGeometry();
                myPathGeometry.Figures.Add(myPathFigure);

                Path myPath = new Path();
                myPath.Stroke = Brushes.Yellow;
                myPath.StrokeThickness = 2;
                myPath.Data = myPathGeometry;

                myCanvas.Children.Add(myPath);
                */

                //myEllipse.Stroke = Brushes.Yellow;
                //myEllipse.StrokeThickness = 1;
                //myCanvas.Children.Add(myEllipse);

                foreach (CelestialBody body in bodies)
                {
                    Ellipse ellipse = new Ellipse();
                    ellipse.MouseLeftButtonDown += Ellipse_Click;
                    ellipses.Add(body.uniqueID, ellipse);
                    myCanvas.Children.Add(ellipse);
                }

                this.DataContext = simulator;
                UpdateUI();

                Timer aTimer = new Timer(10);
                aTimer.Elapsed += new ElapsedEventHandler(RunEvent);
                aTimer.Enabled = true;
            };
        }

        private void UpdateCanvas()
        {
            List<CelestialBody> bodiesCopy = new List<CelestialBody>(bodies.Count);
            foreach (CelestialBody body in bodies) bodiesCopy.Add(body.GetCopy());

            // First we rotate the plane to place camera to in line with negative side of y-axes. Then we tilt the plane to bring camera in line with Z-axis.
            // This way we have X-axis to the right, Y-axis to the up and Z-axis is depth with positive values being closer to camera.
            // Doing this allows for easy manipulation and visualization of objects in any camera orientation.

            Matrix rotation = Matrix.GetRotationMatrix(unitVectorX, GetCameraXYRotation() - Math.PI / 2.0) *
                Matrix.GetRotationMatrix(unitVectorZ, -GetCameraZRotation() - Math.PI / 2.0);

            double cameraDistance = (camera - cameraTarget).Length;

            // This scales real coordinates to screen coordinates
            double screenScaling = myCanvas.ActualWidth / (Math.Tan(fieldOfView * Math.PI / 360.0) * cameraDistance * 2.0);

            if (bodiesCopy.Exists(n => n.uniqueID == targetUniqueID)) // Lock camera on target if one exists
            {
                Vector3D position = bodiesCopy.Find(n => n.uniqueID == targetUniqueID).position;
                camera = camera + position - cameraTarget;
                cameraTarget = position;
            }

            bodiesCopy.Sort(delegate (CelestialBody a, CelestialBody b)  // Sort for distance so we know drawing order
            {
                return (camera - b.position).Length.CompareTo((camera - a.position).Length);
            });

            foreach (CelestialBody body in bodiesCopy) body.position = body.position - cameraTarget; // Translate position

            for (int i = 0; i < bodiesCopy.Count; i++)
            {
                CelestialBody body = bodiesCopy[i];
                Vector3D rotatedBody = rotation * body.position;

                // To create the illusion of depth, objects are drawn closer to origin if they are further away.
                // This also allows drawing them in correct scale.
                double distanceScaling = cameraDistance / (cameraDistance - rotatedBody.Z);

                if (distanceScaling > 0.0)
                {
                    ellipses[body.uniqueID].Visibility = Visibility.Visible;
                    Canvas.SetZIndex(ellipses[body.uniqueID], i);
                    DrawCircle(ellipses[body.uniqueID], rotatedBody.X, rotatedBody.Y, body.radius * Math.Pow(body.magnification, 1.0 / 3.0), distanceScaling * screenScaling, body.color);
                }
                else
                {
                    ellipses[body.uniqueID].Visibility = Visibility.Collapsed;
                }
            }
        }

        public void RunEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() => UpdateUI(), DispatcherPriority.Normal);
            }
            catch (TaskCanceledException) { }
        }

        private void UpdateUI()
        {
            if (simulator.isRunning)
            {
                bodies = simulator.GetBodies();
                UpdateSpeedWarning();
                UpdateAttributes(bodies);
            }

            if (bodies.Count != ellipses.Count)
            {
                List<long> keys = ellipses.Keys.ToList();
                foreach (int key in keys)
                {
                    if (!bodies.Exists(n => n.uniqueID == key))
                    {
                        myCanvas.Children.Remove(ellipses[key]);
                        ellipses.Remove(key);
                    }
                }
            }
            
            UpdateCamera();
            UpdateCanvas();
        }

        private void UpdateSpeedWarning()
        {
            if (simulator.StepLength >= 2000) textBlock_step.Foreground = Brushes.Red;
            else if (simulator.StepLength >= 500) textBlock_step.Foreground = Brushes.DarkOrange;
            else textBlock_step.Foreground = Brushes.Black;
        }

        private void UpdateAttributes(List<CelestialBody> bodies)
        {
            if (simulator.isRunning)
            {
                if (targetUniqueID > 0 && bodies.Exists(n => n.uniqueID == targetUniqueID))
                {
                    CelestialBody target = bodies.Find(n => n.uniqueID == targetUniqueID);

                    textBlock_name.Text = target.name;
                    textBlock_mass.Text = target.mass.ToString("G3");
                    textBlock_radius.Text = target.radius.ToString("G3");

                    textBlock_posX.Text = target.position.X.ToString("G3");
                    textBlock_posY.Text = target.position.Y.ToString("G3");
                    textBlock_posZ.Text = target.position.Z.ToString("G3");

                    textBlock_speedX.Text = target.velocity.X.ToString("G3");
                    textBlock_speedY.Text = target.velocity.Y.ToString("G3");
                    textBlock_speedZ.Text = target.velocity.Z.ToString("G3");

                    textBlock_colorR.Text = target.color.Color.R.ToString("G3");
                    textBlock_colorG.Text = target.color.Color.G.ToString("G3");
                    textBlock_colorB.Text = target.color.Color.B.ToString("G3");

                    textBlock_magnification.Text = target.magnification.ToString();
                }
                else
                {
                    targetUniqueID = 0;
                    foreach(UIElement element in AttributeTextBlocks.Children)
                    {
                        ((TextBlock)element).Text = String.Empty;
                    }
                }

                textBlock_simulationLength.Text = simulator.SimulationTime.ToString("0.00");
                textBlock_step.Text = simulator.StepLength.ToString("0.00");
                textBlock_targetSpeed.Text = simulator.TargetSpeed.ToString("0.00");
            }
            else
            {
                if (targetUniqueID > 0 && bodies.Exists(n => n.uniqueID == targetUniqueID))
                {
                    CelestialBody target = bodies.Find(n => n.uniqueID == targetUniqueID);

                    textBox_name.Text = target.name;
                    textBox_mass.Text = target.mass.ToString();
                    textBox_radius.Text = target.radius.ToString();

                    textBox_posX.Text = target.position.X.ToString();
                    textBox_posY.Text = target.position.Y.ToString();
                    textBox_posZ.Text = target.position.Z.ToString();

                    textBox_speedX.Text = target.velocity.X.ToString();
                    textBox_speedY.Text = target.velocity.Y.ToString();
                    textBox_speedZ.Text = target.velocity.Z.ToString();

                    textBox_colorR.Text = target.color.Color.R.ToString();
                    textBox_colorG.Text = target.color.Color.G.ToString();
                    textBox_colorB.Text = target.color.Color.B.ToString();

                    textBox_magnification.Text = target.magnification.ToString();
                }
                else
                {
                    targetUniqueID = 0;
                    foreach (UIElement element in AttributeTextBoxes.Children)
                    {
                        ((TextBox)element).Text = String.Empty;
                    }

                    textBox_simulationLength.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                    textBox_step.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                    textBox_targetSpeed.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                }
            }         
        }

        private void DrawCircle(Ellipse ellipse, double x, double y, double radius, double scale, SolidColorBrush color)
        {
            ellipse.Stroke = Brushes.Black;
            ellipse.StrokeThickness = 0.7;
            ellipse.Fill = color;     

            double OriginX = myCanvas.ActualWidth / 2;
            double OriginY = myCanvas.ActualHeight / 2;

            double screenRadius = radius * scale;
            ellipse.Width = ellipse.Height = screenRadius * 2;

            double coordX = x * scale + OriginX - screenRadius;
            double coordY = -y * scale + OriginY - screenRadius; // Y-axis should grow towards top
            
            Canvas.SetLeft(ellipse, coordX);
            Canvas.SetTop(ellipse, coordY); 
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvas();
        }

        #region MenuItems
        private void MenuItem_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        private double GetCameraZRotation()
        {
            Vector3D cTrans = camera - cameraTarget;
            return Math.Atan2(cTrans.Y, cTrans.X);
        }

        private double GetCameraXYRotation()
        {
            Vector3D cTrans = camera - cameraTarget;
            double baseX = Math.Sqrt(cTrans.X * cTrans.X + cTrans.Y * cTrans.Y);
            return Math.Atan2(baseX * cTrans.Z, baseX * baseX);
        }

        private void UpdateCamera()
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                Vector mouseDistance = Mouse.GetPosition(myCanvas) - mouseOrigin;
                mouseOrigin = Mouse.GetPosition(myCanvas);

                if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftCtrl)) && targetUniqueID == 0)
                {
                    Matrix rotation = Matrix.GetRotationMatrix(unitVectorZ, GetCameraZRotation());
                    Vector3D xTrans = rotation * unitVectorY;
                    Vector3D yTrans = Keyboard.IsKeyDown(Key.LeftShift) ? rotation * unitVectorX : -unitVectorZ;
                    Vector3D totalTrans = (camera - cameraTarget).Length * (xTrans * mouseDistance.X + yTrans * mouseDistance.Y) / 3000.0;

                    camera -= totalTrans;
                    cameraTarget -= totalTrans;
                }
                else
                {
                    if (mouseDistance.X != 0)
                    {
                        // Rotate camera along Z-axis
                        double angleX = mouseDistance.X * Math.PI / 1800;
                        camera = Matrix.GetRotationMatrix(unitVectorZ, -angleX) * (camera - cameraTarget) + cameraTarget;
                    }

                    if (mouseDistance.Y != 0)
                    {
                        double angleY = -mouseDistance.Y * Math.PI / 1800;

                        // Find new rotation axis to adjust height
                        Vector3D rotationAxis = Matrix.GetRotationMatrix(unitVectorZ, GetCameraZRotation()) * unitVectorY;

                        // Set limit how far up or down camera can go
                        if (Math.Abs(GetCameraXYRotation() - angleY) < 89.9 * Math.PI / 180)
                        {
                            camera = Matrix.GetRotationMatrix(rotationAxis, angleY) * (camera - cameraTarget) + cameraTarget;
                        }
                        else
                        {
                            camera = Matrix.GetRotationMatrix(rotationAxis, GetCameraXYRotation() + Math.Sign(angleY) * 89.9 * Math.PI / 180) * (camera - cameraTarget) + cameraTarget;
                        }
                    }
                }
            }
        }

        private void myCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0 && (camera - cameraTarget).Length > 100) camera = 0.92 * (camera - cameraTarget) + cameraTarget;
            else if (e.Delta < 0 && (camera - cameraTarget).Length < 2E14) camera = 1.087 * (camera - cameraTarget) + cameraTarget;
        }

        private async void button_start_Click(object sender, RoutedEventArgs e)
        {
            if (!simulator.isRunning)
            {
                button_start.Content = "Stop";
                AttributeTextBoxes.Visibility = Visibility.Collapsed;
                AttributeTextBlocks.Visibility = Visibility.Visible;
                UpdateAttributes(bodies);

                Task task1 = Task.Run(() => simulator.StartSimulation(bodies));
                Task task2 = Task.Run(() => simulator.MonitorTime());

                await task1.ContinueWith((n) => Dispatcher.Invoke(() => {
                    button_start.Content = "Start";
                    AttributeTextBoxes.Visibility = Visibility.Visible;
                    AttributeTextBlocks.Visibility = Visibility.Collapsed;
                    UpdateAttributes(simulator.GetBodies());
                }, DispatcherPriority.Normal));
            }
            else
            {
                simulator.isRunning = false;
            }
        }

        private void myCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseOrigin = Mouse.GetPosition(myCanvas);
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Keyboard.ClearFocus();
            }
        }

        private void button_mode_Click(object sender, RoutedEventArgs e)
        {
            simulator.HasSpeedTarget = !simulator.HasSpeedTarget;
            textBlock_speed.FontWeight = simulator.HasSpeedTarget ? FontWeights.Bold : FontWeights.Normal;
            textBlock_stepLength.FontWeight = simulator.HasSpeedTarget ? FontWeights.Normal : FontWeights.Bold;
        }

        private void Ellipse_Click(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<long, Ellipse> pair in ellipses)
            {
                if (pair.Value == sender as Ellipse) targetUniqueID = pair.Key;
            }

            UpdateUI();
            UpdateAttributes(bodies);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                targetUniqueID = 0;
                UpdateAttributes(bodies);
            }
        }
    }
}
