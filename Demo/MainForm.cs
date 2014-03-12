
//Multiple face detection and recognition in real time
//Using EmguCV cross platform .Net wrapper to the Intel OpenCV image processing library for C#.Net
//Writed by Sergio Andrés Guitérrez Rojas
//"Serg3ant" for the delveloper comunity
// Sergiogut1805@hotmail.com
//Regards from Bucaramanga-Colombia ;)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;
using System.Timers;
using System.Threading;

using FaceRecognizer;

namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        private Detector detector = null;
        private DateTime startTime;
        private Thread thread = null;

        public FrmPrincipal()
        {
            InitializeComponent();

            detector = new Detector(Application.StartupPath);
            detector.match += new Detector.MatchHandler(handleMatched);
            detector.logger += new Detector.LogHandler(detector_logger);
            detector.trainComplete += new Detector.TrainCompleteHandler(detector_trainComplete);
            this.FormClosed += new FormClosedEventHandler(FrmPrincipal_FormClosed);            

            //Application.Idle += new EventHandler(idle);
            thread = new Thread(new ThreadStart(run));
            thread.Start();
        }

        //public void capture()//(object sender, EventArgs e)
        void idle(object sender, EventArgs e)
        {
            this.detector.capture();
        }

        void run()
        {
            while (true)
            {
                this.detector.capture();
            }
        }

        void FrmPrincipal_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Application.Idle -= new EventHandler(detector.capture);
            this.thread.Abort();
        }

        void detector_logger(string message)
        {
            this.Invoke((MethodInvoker)delegate
            {
                this.loggerLabel.Text = message;
            });
        }

        private void handleMatched(string match, int count)
        {
            this.Invoke((MethodInvoker)delegate
            {
                if (count == -2)
                {
                    this.indicatorLabel.BackColor = Color.Gray;
                    this.loggerLabel.Text = "";
                }
                else if (count == 0)
                {
                    this.indicatorLabel.BackColor = Color.Red;
                }
                else if (count > 0 && count < 10)
                {
                    if (count == 1)
                    {
                        this.startTime = DateTime.Now;
                    }
                    else if (count == 9)
                    {
                        this.timeLabel.Text = DateTime.Now.Subtract(this.startTime).TotalSeconds.ToString();
                    }
                    this.indicatorLabel.BackColor = Color.Yellow;
                }
                else
                {
                    this.indicatorLabel.BackColor = Color.Green;

                }

                if (String.IsNullOrEmpty(match) == false)
                {
                    match = String.Format("Hi, {0}", match);
                }
                this.nameLabel.Text = match;
            });
        }

        private void trainButton_Click(object sender, System.EventArgs e)
        {
            this.detector.startTraining(this.personToTrainText.Text);
            this.trainButton.Enabled = false;
            this.trainButton.Text = "Training...";
            this.loggerLabel.Text = "";
        }

        void detector_trainComplete()
        {
            this.Invoke((MethodInvoker)delegate
            {
                this.personToTrainText.Text = "";
                this.trainButton.Enabled = true;
                this.trainButton.Text = "Train";
            });
        }
    }
}