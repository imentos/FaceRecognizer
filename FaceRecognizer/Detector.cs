using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;
using System.Timers;
using System.Linq;

namespace FaceRecognizer
{
    public class Detector
    {
        private Capture grabber;
        private HaarCascade face;
        private List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        private List<string> labels = new List<string>();
        private int trainCount = -1;
        private long frameCount = 0;
        private const int MATCH_THRESHOLD = 10;
        private const int TRAIN_THRESHOLD = 10;
        private const int NOFACE_COUNT = 30;
        private Dictionary<string, int> matchPersons = new Dictionary<string, int>();
        private string bestMatchPerson;

        private string appPath;
        private string personToTrain;
        private EigenObjectRecognizer recognizer = null;

        public event MatchHandler match;
        public delegate void MatchHandler(string bestMatchPerson, int count);

        public event LogHandler logger;
        public delegate void LogHandler(string message);

        public event TrainCompleteHandler trainComplete;
        public delegate void TrainCompleteHandler();

        private void log(string msg)
        {
            DateTime now = DateTime.Now;
            Console.WriteLine(now.ToString() + ": " + msg);
        }

        public Detector(string path)
        {
            this.appPath = path;

            this.face = new HaarCascade("haarcascade_frontalface_default.xml");
            try
            {
                //Load of previus trainned faces and labels for each image
                string labelsinfo = File.ReadAllText(path + "/TrainedFaces/TrainedLabels.txt");
                string[] labels = labelsinfo.Split('%');
                int numLabels = Convert.ToInt16(labels[0]);

                for (int tf = 1; tf < numLabels + 1; tf++)
                {
                    string loadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(path + "/TrainedFaces/" + loadFaces));
                    this.labels.Add(labels[tf]);
                }

                MCvTermCriteria termCrit = new MCvTermCriteria(10, 0.001);
                recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), this.labels.ToArray(), 5000, ref termCrit);
            }
            catch (Exception e)
            {
                Console.WriteLine("Init fails:" + e.Message);
            }

            try
            {
                grabber = new Capture();
                grabber.QueryFrame();
            }
            catch (Exception e)
            {
                throw;                
            }

        }

        private bool train(Image<Gray, byte> gray, string name)
        {
            Image<Gray, byte> trainedFace = null;
            try
            {
                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
                foreach (MCvAvgComp f in facesDetected[0])
                {
                    trainedFace = gray.Copy(f.rect).Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    break;
                }
                trainedFace._EqualizeHist();
                trainingImages.Add(trainedFace);
                labels.Add(name);

                //Write the number of triained faces in a file text for further load
                File.WriteAllText(this.appPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                //Write the labels of triained faces in a file text for further load
                int totalImages = trainingImages.ToArray().Length + 1;
                for (int i = 1; i < totalImages; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(this.appPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(this.appPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }
                //Console.WriteLine(textBox1.Text + "´s face detected and added");
                //this.logger(name + "´s face detected and added");
                log(name + "´s face detected and added");

                MCvTermCriteria termCrit = new MCvTermCriteria(10, 0.001);
                recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), this.labels.ToArray(), 5000, ref termCrit);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Training Fail:" + e.Message);
                return false;
            }
        }

        public void startTraining(string name)
        {
            this.trainCount = 0;
            this.personToTrain = name;
        }

        public void capture()
        {
            this.log("capturing...");
            Image<Bgr, Byte> currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            Image<Gray, byte> gray = currentFrame.Convert<Gray, Byte>();
            this.log("capture done");

            // check train done
            if (this.frameCount % 10 == 0 && this.trainCount >= 0 && this.trainCount < TRAIN_THRESHOLD)
            {
                if (this.train(gray, this.personToTrain))
                {
                    this.trainCount++;
                    this.log("training..." + trainCount);
                }
                this.match("", -2);
                return;
            }
            else if (this.trainCount == TRAIN_THRESHOLD)
            {
                this.trainComplete();
                this.trainCount = -1;
            }


            // Face detect
            this.log("face detecting...");
            Image<Gray, byte> result;
            MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            this.log("face detecting done");

            // find the best matched again.
            if (facesDetected[0].Length != 1)
            {
                this.log("no faces detected");
                this.match(this.bestMatchPerson, 0);

                if (this.frameCount % NOFACE_COUNT == 0)
                {
                    this.matchPersons.Clear();
                    this.bestMatchPerson = "";
                    this.log("reset");
                    this.match("", 0);
                }
            }
            else
            {
                this.log("faces detected");

                foreach (MCvAvgComp f in facesDetected[0])
                {
                    result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    // Find the best match person
                    int testCount = 0;
                    foreach (KeyValuePair<string, int> iter in this.matchPersons)
                    {
                        testCount += iter.Value;
                    }

                    if (trainingImages.ToArray().Length != 0)
                    {
                        string name = recognizer.Recognize(result, TRAIN_THRESHOLD);
                        this.log("found:" + name + ", maxCount:" + testCount);

                        if (String.IsNullOrEmpty(name) == false)
                        {
                            // Find the best match until threshold
                            if (testCount < MATCH_THRESHOLD)
                            {
                                if (this.matchPersons.ContainsKey(name))
                                {
                                    this.matchPersons[name]++;
                                }
                                else
                                {
                                    this.matchPersons[name] = 1;
                                }

                                this.match("", testCount);
                            }
                            else
                            {
                                IEnumerable<KeyValuePair<string, int>> sortedDict = from entry in matchPersons orderby entry.Value descending select entry;
                                string matchPersonsOut = "";
                                string bestMatch = "";
                                int i = 0;
                                foreach (KeyValuePair<string, int> iter in sortedDict)
                                {
                                    matchPersonsOut += iter.Key + ": " + iter.Value + "\n";
                                    if (i++ == 0)
                                    {
                                        if (iter.Value >= 7)
                                        {
                                            bestMatch = iter.Key;
                                            this.bestMatchPerson = bestMatch;
                                        }
                                        else
                                        {
                                            this.matchPersons.Clear();
                                            this.bestMatchPerson = "";
                                            this.log("cannot find the person over 7");
                                        }
                                    }
                                }
                                this.match(bestMatch, testCount);

                                this.logger(matchPersonsOut);
                            }
                        }
                    }

                }
            }
            this.frameCount++;
        }
    }
}
