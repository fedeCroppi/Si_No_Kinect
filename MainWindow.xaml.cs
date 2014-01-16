// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace FaceTrackingBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.FaceTracking;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        KinectSensor newSensor;
        FaceTracker faceTracker;
        private byte[] colorPixelData;
        private short[] depthPixelData;
        private Skeleton[] skeletonData, skeletonDataPle;

        //Gestos
        private bool derecha, izquierda, arriba, abajo;
        private DateTime ultimoGesto, si, no;
        private float periodoEntreGestos = 0;
        private int cont;

        public MainWindow()
        {
            InitializeComponent();
            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            sensorChooser.Start();
            derecha = izquierda = arriba = abajo = true;

            FileStream fs = File.OpenRead(@"C:\Users\Federico\Desktop\dades_Kinect\skeletonData");
            BinaryFormatter bf = new BinaryFormatter();
            this.skeletonDataPle = (Skeleton[])bf.Deserialize(fs);
            fs.Close();
        }

        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs kinectChangedEventArgs)
        {
            KinectSensor oldSensor = kinectChangedEventArgs.OldSensor;
            newSensor = kinectChangedEventArgs.NewSensor;

            if (oldSensor != null)
            {
                oldSensor.AllFramesReady -= KinectSensorOnAllFramesReady;
                oldSensor.ColorStream.Disable();
                oldSensor.DepthStream.Disable();
                oldSensor.DepthStream.Range = DepthRange.Default;
                oldSensor.SkeletonStream.Disable();
                oldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                oldSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }

            if (newSensor != null)
            {
                try
                {
                    newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    try
                    {
                        // This will throw on non Kinect For Windows devices.
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    newSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    newSensor.SkeletonStream.Enable();

                    newSensor.AllFramesReady += KinectSensorOnAllFramesReady;

                    // Initialize data arrays
                    colorPixelData = new byte[newSensor.ColorStream.FramePixelDataLength];
                    depthPixelData = new short[newSensor.DepthStream.FramePixelDataLength];
                    skeletonData = new Skeleton[6];

                    // Initialize a new FaceTracker with the KinectSensor
                    faceTracker = new FaceTracker(newSensor);
                }
                catch (InvalidOperationException)
                {
                    // This exception can be thrown when we are trying to
                    // enable streams on a device that has gone away.  This
                    // can occur, say, in app shutdown scenarios when the sensor
                    // goes away between the time it changed status and the
                    // time we get the sensor changed notification.
                    //
                    // Behavior here is to just eat the exception and assume
                    // another notification will come along if a sensor
                    // comes back.
                }
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            sensorChooser.Stop();
        }

        private void KinectSensorOnAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            // Retrieve each single frame and copy the data
            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                    return;
                colorImageFrame.CopyPixelDataTo(colorPixelData);
            }

            using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame == null)
                    return;
                depthImageFrame.CopyPixelDataTo(depthPixelData);
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                skeletonFrame.CopySkeletonDataTo(skeletonData);
                if (this.skeletonData[0].TrackingState != SkeletonTrackingState.Tracked &&
                    this.skeletonData[1].TrackingState != SkeletonTrackingState.Tracked &&
                    this.skeletonData[2].TrackingState != SkeletonTrackingState.Tracked &&
                    this.skeletonData[3].TrackingState != SkeletonTrackingState.Tracked &&
                    this.skeletonData[4].TrackingState != SkeletonTrackingState.Tracked &&
                    this.skeletonData[5].TrackingState != SkeletonTrackingState.Tracked)
                {
                    this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    this.skeletonDataPle.CopyTo(this.skeletonData, 0);
                    this.skeletonData[3].TrackingState = SkeletonTrackingState.Tracked;
                }
            }

            var skeleton = skeletonData.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
            // Make the faceTracker processing the data.
            FaceTrackFrame faceFrame = faceTracker.Track(newSensor.ColorStream.Format, colorPixelData,
                                              newSensor.DepthStream.Format, depthPixelData,
                                              skeleton);

            if (skeleton != null)
            {
                var cap = skeleton.Joints[JointType.Head];

                ullDret.X = ullEsq.X = pupDre.X = pupEsq.X = cejaDer.X = cejaIzq.X = nariz.X = boca.X = faceFrame.Rotation.Y * -1;
                ullDret.Y = ullEsq.Y = pupDre.Y = pupEsq.Y = cejaDer.Y = cejaIzq.Y = nariz.Y = boca.Y = faceFrame.Rotation.X * -1;

                bocaScale.ScaleX = faceFrame.Rotation.Y / 50;

                // movimiento de cabeza
                CanvasTranslate.X = cap.Position.X * 150;
                CanvasTranslate.Y = cap.Position.Y * -150;
            }
            // If a face is tracked, then we can use it.
            if (faceFrame.TrackSuccessful)
            {
                // Retrieve only the Animation Units coeffs.
                var AUCoeff = faceFrame.GetAnimationUnitCoefficients();
                
                var manAbajo = AUCoeff[AnimationUnit.JawLower];
                manAbajo = manAbajo < 0 ? 0 : manAbajo;

                bocaTransf.ScaleY = manAbajo * 5 + 0.1;
                bocaTransf.ScaleX = (AUCoeff[AnimationUnit.LipStretcher] + 1);

                //cejaDer.Y = cejaIzq.Y = (AUCoeff[AnimationUnit.BrowLower]) * 50;

                RightBrowRotate.Angle = (AUCoeff[AnimationUnit.BrowRaiser] * 50);
                LeftBrowRotate.Angle = -RightBrowRotate.Angle;

                CanvasRotate.Angle = faceFrame.Rotation.Z * -1;
            }

            if (nariz.X < -4 && derecha)
            {
                izquierda = true;

                if (derecha && DateTime.Now.TimeOfDay.TotalMilliseconds - no.TimeOfDay.TotalMilliseconds < 1000)
                {
                    cont++;
                }
                else
                {
                    cont = 0;
                }

                no = DateTime.Now;

                derecha = false;

                if (cont > 4)
                {
                    label.Content = "NO";
                    cont = 0;
                }
            }
            if (nariz.X > 4 && izquierda)
            {
                derecha = true;

                if (izquierda && DateTime.Now.TimeOfDay.TotalMilliseconds - no.TimeOfDay.TotalMilliseconds < 1000)
                {
                    cont++;
                }
                else
                {
                    cont = 0;
                }

                no = DateTime.Now;

                izquierda = false;

                if (cont > 4)
                {
                    label.Content = "NO";
                    cont = 0;
                }
            }
            if (nariz.Y < -2 && abajo)
            {
                arriba = true;
                
                if (abajo && DateTime.Now.TimeOfDay.TotalMilliseconds - si.TimeOfDay.TotalMilliseconds < 1000)
                {
                    cont++;
                }
                else
                {
                    cont = 0;
                }

                si = DateTime.Now;

                abajo = false;

                if (cont > 4)
                {
                    label.Content = "SI";
                    cont = 0;
                }
            }
            if (nariz.Y > 2 && arriba)
            {
                abajo = true;

                if (arriba && DateTime.Now.TimeOfDay.TotalMilliseconds - si.TimeOfDay.TotalMilliseconds < 1000)
                {
                    cont++;
                }
                else
                {
                    cont = 0;
                }

                si = DateTime.Now;

                arriba = false;

                if (cont > 3)
                {
                    label.Content = "SI";
                    cont = 0;
                }
            }
        }
    }
}
