using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
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

namespace AzureKinectHelloWorld
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.Run(() => Go());
        }

        private async Task Go()
        {
            try
            {
                using (Device device = Device.Open())
                {

                    device.StartCameras(new DeviceConfiguration
                    {
                        CameraFPS = FPS.FPS30,
                        ColorResolution = ColorResolution.R1080p,
                        ColorFormat = ImageFormat.ColorBGRA32,

                        DepthMode = DepthMode.NFOV_2x2Binned,

                        SynchronizedImagesOnly = true
                    });

                    var calibration = device.GetCalibration();
                    Transformation transformation = calibration.CreateTransformation();

                    Tracker tracker = Tracker.Create(calibration, new TrackerConfiguration()
                    {
                        ProcessingMode = TrackerProcessingMode.Gpu,
                        SensorOrientation = SensorOrientation.Default
                    });

                    while (true)
                    {
                        Capture capture1 = device.GetCapture();

                        tracker.EnqueueCapture(capture1);
                        var frame = tracker.PopResult(TimeSpan.Zero, throwOnTimeout: false);
                        if (frame != null)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                for (uint i = 0; i < frame.NumberOfBodies; i++)
                                {
                                    var bodyId = frame.GetBodyId(i);
                                    var body = frame.GetBody(i);

                                    var handLeft = body.Skeleton.GetJoint(JointId.HandLeft);
                                    var handRight = body.Skeleton.GetJoint(JointId.HandRight);

                                    var pointLeft = calibration.TransformTo2D(handLeft.Position, CalibrationDeviceType.Depth, CalibrationDeviceType.Color);
                                    var pointRight = calibration.TransformTo2D(handRight.Position, CalibrationDeviceType.Depth, CalibrationDeviceType.Color);

                                    try
                                    {
                                        if (pointLeft.HasValue && pointRight.HasValue)
                                        {
                                            Image1.Source = CreateNewBitmap(capture1.Color,
                                                (int)pointLeft.Value.X, (int)pointLeft.Value.Y,
                                                (int)pointRight.Value.X, (int)pointRight.Value.Y);
                                        }
                                    }
                                    catch { }
                                }
                            });

                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private WriteableBitmap CreateNewBitmap(
            Microsoft.Azure.Kinect.Sensor.Image image,
            int xLeft, int yLeft,
            int xRight, int yRight)
        {

            WriteableBitmap wb = new WriteableBitmap(
                image.WidthPixels + 30,
                image.HeightPixels + 30,
                96,
                96,
                PixelFormats.Bgra32, null);

            var region = new Int32Rect(0, 0, image.WidthPixels, image.HeightPixels);

            unsafe
            {
                using (var pin = image.Memory.Pin())
                {
                    wb.WritePixels(region, (IntPtr)pin.Pointer, (int)image.Size, image.StrideBytes);
                }
            }
            using (wb.GetBitmapContext())
            {
                wb.FillEllipse(xLeft, yLeft, xLeft + 40, yLeft + 40, Colors.Red);
                wb.FillEllipse(xRight, yRight, xRight + 40, yRight + 40, Colors.Blue);
            }

            return wb;
        }
    }
}
