
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

namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const string APP_FILE = "apps.txt";
        
        private Detector detector = null;
        private DateTime startTime;
        private Thread thread = null;
        private Dictionary<string, string> apps = new Dictionary<string, string>();
        private string currentApp = null;
        private int currentProcessId = -1;
        private string currentMatch = null;

        public FrmPrincipal()
        {
            try
            {
                InitializeComponent();

                enableInputs(false);
                readApps();

                detector = new Detector(Application.StartupPath);
                detector.match += new Detector.MatchHandler(handleMatched);
                detector.logger += new Detector.LogHandler(detector_logger);
                detector.trainComplete += new Detector.TrainCompleteHandler(detector_trainComplete);
                this.FormClosed += new FormClosedEventHandler(FrmPrincipal_FormClosed);

                thread = new Thread(new ThreadStart(run));
                thread.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void readApps()
        {
            if (File.Exists(Application.StartupPath + "\\" + APP_FILE) == false)
            {
                File.Create(Application.StartupPath + "\\" + APP_FILE);
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
                    /*
                    if (match != this.currentMatch)
                    {
                        this.currentMatch = match;

                        if (this.apps.ContainsKey(match))
                        {
                            if (this.appExist(currentProcessId) == false)// && String.IsNullOrEmpty(this.currentApp) == false)
                            {
                                string a = Win32API.FindExecutable(this.apps[match]);
                                Process.Start(a);
                                Process app = Process.Start(this.apps[match]);
                                if (app != null)
                                {
                                    this.currentProcessId = app.Id;
                                }
                            }

                        }
                    }
                    else
                    {
                        if (this.currentProcessId != -1)
                        {
                            Process process = Process.GetProcessById(this.currentProcessId);
                            SetForegroundWindow(process.MainWindowHandle);
                        }
                    }
            */

                        
                    // check if different app                    
                    if (this.apps.ContainsKey(match) && this.currentApp != this.apps[match])
                    {
                        this.currentApp = this.apps[match];
                        this.currentProcessId = -1;
                    }

                    // check if the process exist
                    if (this.appExist(currentProcessId) == false && String.IsNullOrEmpty(this.currentApp) == false)
                    {                        
                        string a = Win32API.FindExecutable(this.apps[match]);
                        string b = Path.GetFileNameWithoutExtension(a);
                        if (processIsRunning(b))
                            
                        {

                            //if (app != null) this.currentProcessId = app.Id;
                        }
                        else
                        {
                            Process app = Process.Start(this.currentApp);
                            /*
                            string a = Win32API.FindExecutable(this.apps[match]);
                            string b = Path.GetFileNameWithoutExtension(a);
                            if (processIsRunning(b))
                            {
                                //int bg = 0;
                                //bg = 10;
                                //MessageBox.Show("VF");
                            }*/
                        }
                    }
                    else if (this.currentProcessId != -1)
                    {
                        Process process = Process.GetProcessById(this.currentProcessId);
                        SetForegroundWindow(process.MainWindowHandle);
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

        private bool appExist(int id)
        {
            try
            {

                return Process.GetProcessById(currentProcessId).HasExited == false;
            } 
            catch (Exception e) {
                return false;
            }
        }

        private void personToTrain_TextChanged(object sender, EventArgs e)
        {
            enableInputs(String.IsNullOrEmpty(this.personToTrain.Text) == false);
        }

        private void enableInputs(bool enable)
        {
            this.trainButton.Enabled = enable;
            this.appButton.Enabled = enable;
        }
    }
}