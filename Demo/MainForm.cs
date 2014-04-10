
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
using System.Linq;
using FaceRecognizer;
using System.Runtime.InteropServices;
using System.Configuration;

namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const string APP_FILE = "apps.txt";
        
        private Detector detector = null;
        private DateTime startTime;
        private Thread thread = null;
        private Dictionary<string, string> apps = new Dictionary<string, string>();

        public FrmPrincipal()
        {
            try
            {
                InitializeComponent();

                enableInputs(false);
                readApps();

                int maxIter = Convert.ToInt16(ConfigurationSettings.AppSettings["MAX_ITER"]);

                detector = new Detector(Application.StartupPath, maxIter);
                detector.match += new Detector.MatchHandler(handleMatched);
                detector.logger += new Detector.LogHandler(detector_logger);
                detector.trainComplete += new Detector.TrainCompleteHandler(detector_trainComplete);
                detector.cleanup += new Detector.CleaupHandler(detector_cleanup);

                this.FormClosed += new FormClosedEventHandler(FrmPrincipal_FormClosed);
                this.MouseEnter += new EventHandler(FrmPrincipal_MouseEnter);

                thread = new Thread(new ThreadStart(run));
                thread.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        void detector_cleanup()
        {
            foreach (KeyValuePair<string, string> iter in this.apps)
            {
                string currentApp = this.apps[iter.Key];

                // check if the process exist
                string appPath = Win32API.FindExecutable(this.apps[iter.Key]);
                string appName = Path.GetFileNameWithoutExtension(appPath);
                foreach (Process process in Process.GetProcessesByName(appName))
                {
                    process.Kill();
                }
            }
        }

        void FrmPrincipal_MouseEnter(object sender, EventArgs e)
        {
            this.BringToFront();
        }

        private void readApps()
        {
            if (File.Exists(Application.StartupPath + "/" + APP_FILE) == false)
            {
                File.Create(Application.StartupPath + "/" + APP_FILE);                
            }

            if (String.IsNullOrEmpty(File.ReadAllText(Application.StartupPath + "/" + APP_FILE)))
            {
                return;
            }

            string[] lines = File.ReadAllLines(Application.StartupPath + "\\" + APP_FILE);
            foreach (var row in lines)
            {
                this.apps.Add(row.Split('=')[0], row.Split('=')[1]);
            }
        }

        private void updateApps()
        {
            File.Delete(APP_FILE);
            File.AppendAllText(APP_FILE, string.Join("\n", this.apps.Select(x => x.Key + "=" + x.Value).ToArray()));
        }

        void run()
        {
            while (true)
            {
                this.detector.capture();
            }
        }

        private void log(string msg)
        {
            DateTime now = DateTime.Now;
            Console.WriteLine(now.ToString() + ": " + msg);
        }

        void FrmPrincipal_FormClosed(object sender, FormClosedEventArgs e)
        {
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
                if (count == Detector.TRAINING)
                {
                    this.indicatorLabel.BackColor = Color.Gray;
                    this.loggerLabel.Text = "";
                }
                else if (count == 0)
                {
                    this.indicatorLabel.BackColor = Color.Red;
                }
                else if (count > 0 && count < Detector.MATCH_THRESHOLD)
                {
                    if (count == 1)
                    {
                        this.startTime = DateTime.Now;
                    }
                    else if (count == Detector.MATCH_THRESHOLD - 1)
                    {
                        this.timeLabel.Text = DateTime.Now.Subtract(this.startTime).TotalSeconds.ToString();
                    }
                    this.indicatorLabel.BackColor = Color.Yellow;
                }
                else
                {
                    this.indicatorLabel.BackColor = Color.Green;
                }

                // Update the final result
                if (String.IsNullOrEmpty(match) == false)
                {
                    if (this.apps.ContainsKey(match))
                    {
                        string currentApp = this.apps[match];

                        // check if the process exist
                        string appPath = Win32API.FindExecutable(this.apps[match]);
                        string appName = Path.GetFileNameWithoutExtension(appPath);
                        if (processIsRunning(appName) == false)
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo(currentApp);
                            startInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            Process app = Process.Start(startInfo);
                        }
                        else
                        {
                            Process process = Process.GetProcessesByName(appName)[0];
                            if (GetForegroundWindow() != this.Handle)
                            {
                                SetForegroundWindow(process.MainWindowHandle);
                            }
                            //process.WaitForInputIdle();

                            SetForegroundWindow(this.Handle);
                        }
                    }

                    match = String.Format("Hi, {0}", match);
                }
                this.nameLabel.Text = match;
            });
        }

        private void trainButton_Click(object sender, System.EventArgs e)
        {
            if (this.detector != null)
            {
                this.detector.startTraining(this.personToTrain.Text);
            }
            this.trainButton.Enabled = false;
            this.trainButton.Text = "Training...";
            this.loggerLabel.Text = "";
        }

        void detector_trainComplete()
        {
            this.Invoke((MethodInvoker)delegate
            {
                this.personToTrain.Text = "";
                this.trainButton.Enabled = true;
                this.trainButton.Text = "Train";

                this.updateApps();
            });
        }

        private void pickApp_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (String.IsNullOrEmpty(this.personToTrain.Text) == false)
                {
                    this.apps[this.personToTrain.Text] = openFileDialog.FileName;

                    this.updateApps();
                }
            }
        }

        bool processIsRunning(string process)
        {
            return (System.Diagnostics.Process.GetProcessesByName(process).Length != 0);
        }

        private void personToTrain_TextChanged(object sender, EventArgs e)
        {
            enableInputs(String.IsNullOrEmpty(this.personToTrain.Text) == false);
        }

        private void enableInputs(bool enable)
        {
            this.trainButton.Enabled = enable;
            this.appButton.Enabled = enable;
            this.removeButton.Enabled = enable;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.personToTrain.Text) == false)
            {
                if (this.detector.removePerson(this.personToTrain.Text) == false)
                {
                    MessageBox.Show("Failed to remove person", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            }

        }
    }
}