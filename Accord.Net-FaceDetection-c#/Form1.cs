using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO;

using Accord;
using Accord.Imaging.Filters;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Vision.Detection;
using Accord.Vision.Detection.Cascades;


namespace winformapp
{

    public partial class Form1 : Form
    {

        RectanglesMarker marker;
        ResizeNearestNeighbor res = new ResizeNearestNeighbor(200, 200);
        ResizeNearestNeighbor resize = new ResizeNearestNeighbor(320, 200);
        Rectangle[] faces = null;
        internal string aliasname = "Unknown";

        FilterInfoCollection videoDevices;
        private string ldevice;
        private IVideoSource videoSource = null;
        HaarObjectDetector detector;

        public Form1()
        {
            InitializeComponent();

            detector = new HaarObjectDetector(new FaceHaarCascade(), 32, ObjectDetectorSearchMode.Average, 1.5f, ObjectDetectorScalingMode.GreaterToSmaller);
            detector.MaxSize = new Size(320, 320);
            detector.UseParallelProcessing = true;
            detector.Suppression = 2;

            listlocalcamera();
        }

        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
            aProp.SetValue(c, true, null);
        }

        private void listlocalcamera()
        {
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0) return;
                for (int i = 0; i < videoDevices.Count; i++)
                {
                    cmbcamera.Items.Add(videoDevices[i].Name);
                }
                cmbcamera.SelectedIndex = 0;
            }
            catch
            {
            }
        }

        private void uselocalcamera(int camnum)
        {
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0) return;
                ldevice = videoDevices[camnum].MonikerString;
                selectlocalvideodevice(ldevice);
            }
            catch
            {
            }
        }

        private void selectlocalvideodevice(string dev)
        {
            VideoCaptureDevice videoSource = new VideoCaptureDevice(dev);
            videoSource.VideoResolution = selectResolution(videoSource);
            OpenVideoSource(videoSource);
        }

        private static VideoCapabilities selectResolution(VideoCaptureDevice device)
        {
            foreach (var cap in device.VideoCapabilities)
            {
                if (cap.FrameSize.Width == 1280)
                    return cap;
                if (cap.FrameSize.Height == 800)
                    return cap;
            }
            return device.VideoCapabilities.Last();
        }

        private void OpenVideoSource(IVideoSource source)
        {
            CloseVideoSource();
            videoSourcePlayer1.VideoSource = source;
            videoSource = source;
            videoSourcePlayer1.Start();
        }

        private void CloseVideoSource()
        {
            videoSourcePlayer1.SignalToStop();
            while (videoSourcePlayer1.IsRunning)
            {
                Thread.Sleep(100);
            }

                videoSourcePlayer1.Stop();
        }

        private void writetoframeimage(string aliasname, Bitmap image, int linex, int liney)
        {
            Graphics g = Graphics.FromImage(image);
            g.DrawString(aliasname, this.Font, Brushes.CornflowerBlue, new PointF(linex, liney));
            g.Flush();
        }


        private void videoSourcePlayer1_NewFrame(object sender, ref Bitmap image)
        {
            Bitmap ximage = image;
            ximage = resize.Apply(ximage);
            faces = detector.ProcessFrame(ximage);
            if (faces.Length > 0)
            {
                for (var i = 0; i < faces.Length; i++)
                {
                    faces[i].X -= 10;
                    faces[i].Y -= 10;
                    faces[i].Height += 20;
                    faces[i].Width += 20;
                    var faceimage = new Crop(faces[i]).Apply(ximage);
                    faceimage = res.Apply(faceimage);
                    pictureBox2.Image = faceimage;
                    writetoframeimage("Unknown", ximage, faces[i].X - 4, faces[i].Y - 14);
                }
            }
            marker = new RectanglesMarker(faces);
            marker.MarkerColor = Color.DeepSkyBlue;
            marker.ApplyInPlace(ximage);
            image = ximage;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (cmbcamera.Items.Count > 0)
            {
                videoSourcePlayer1.BringToFront();
                uselocalcamera(cmbcamera.SelectedIndex);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetDoubleBuffered(videoSourcePlayer1);
            SetDoubleBuffered(pictureBox2);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            videoSourcePlayer1.Dispose();
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((videoSource != null) && (videoSource is VideoCaptureDevice))
            {
                try
                {
                    ((VideoCaptureDevice)videoSource).DisplayPropertyPage(this.Handle);
                }
                catch (NotSupportedException)
                {
                    MessageBox.Show("The video source does not support configuration property page.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

    }
}
