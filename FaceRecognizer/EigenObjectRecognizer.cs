using System;
using System.Diagnostics;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FaceRecognizer
{
   /// <summary>
   /// An object recognizer using PCA (Principle Components Analysis)
   /// </summary>
   [Serializable]
   public class EigenObjectRecognizer
   {
      private Image<Gray, Single>[] _eigenImages;
      private Image<Gray, Single> _avgImage;
      private Matrix<float>[] _eigenValues;
      private string[] _labels;
      private double _eigenDistanceThreshold;
      private string appPath;

      private const int DIVID = 2;

      /// <summary>
      /// Get the eigen vectors that form the eigen space
      /// </summary>
      /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
      public Image<Gray, Single>[] EigenImages
      {
         get { return _eigenImages; }
         set { _eigenImages = value; }
      }

      /// <summary>
      /// Get or set the labels for the corresponding training image
      /// </summary>
      public String[] Labels
      {
         get { return _labels; }
         set { _labels = value; }
      }

      /// <summary>
      /// Get or set the eigen distance threshold.
      /// The smaller the number, the more likely an examined image will be treated as unrecognized object. 
      /// Set it to a huge number (e.g. 5000) and the recognizer will always treated the examined image as one of the known object. 
      /// </summary>
      public double EigenDistanceThreshold
      {
         get { return _eigenDistanceThreshold; }
         set { _eigenDistanceThreshold = value; }
      }

      /// <summary>
      /// Get the average Image. 
      /// </summary>
      /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
      public Image<Gray, Single> AverageImage
      {
         get { return _avgImage; }
         set { _avgImage = value; }
      }

      /// <summary>
      /// Get the eigen values of each of the training image
      /// </summary>
      /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
      public Matrix<float>[] EigenValues
      {
         get { return _eigenValues; }
         set { _eigenValues = value; }
      }

      private EigenObjectRecognizer()
      {
      }


      /// <summary>
      /// Create an object recognizer using the specific tranning data and parameters, it will always return the most similar object
      /// </summary>
      /// <param name="images">The images used for training, each of them should be the same size. It's recommended the images are histogram normalized</param>
      /// <param name="termCrit">The criteria for recognizer training</param>
      public EigenObjectRecognizer(string path, Image<Gray, Byte>[] images, ref MCvTermCriteria termCrit)
         : this(path, images, GenerateLabels(images.Length), ref termCrit)
      {
      }

      private static String[] GenerateLabels(int size)
      {
         String[] labels = new string[size];
         for (int i = 0; i < size; i++)
            labels[i] = i.ToString();
         return labels;
      }

      /// <summary>
      /// Create an object recognizer using the specific tranning data and parameters, it will always return the most similar object
      /// </summary>
      /// <param name="images">The images used for training, each of them should be the same size. It's recommended the images are histogram normalized</param>
      /// <param name="labels">The labels corresponding to the images</param>
      /// <param name="termCrit">The criteria for recognizer training</param>
      public EigenObjectRecognizer(string path, Image<Gray, Byte>[] images, String[] labels, ref MCvTermCriteria termCrit)
         : this(path, images, labels, 0, ref termCrit)
      {
      }

      /// <summary>
      /// Create an object recognizer using the specific tranning data and parameters
      /// </summary>
      /// <param name="images">The images used for training, each of them should be the same size. It's recommended the images are histogram normalized</param>
      /// <param name="labels">The labels corresponding to the images</param>
      /// <param name="eigenDistanceThreshold">
      /// The eigen distance threshold, (0, ~1000].
      /// The smaller the number, the more likely an examined image will be treated as unrecognized object. 
      /// If the threshold is &lt; 0, the recognizer will always treated the examined image as one of the known object. 
      /// </param>
      /// <param name="termCrit">The criteria for recognizer training</param>
      public EigenObjectRecognizer(string path, Image<Gray, Byte>[] images, String[] labels, double eigenDistanceThreshold, ref MCvTermCriteria termCrit)
      {
          this.appPath = path;
          if (File.Exists(path + "/log.txt") == false)
          {
              File.Create(path + "/log.txt");
          }
          Debug.Assert(images.Length == labels.Length, "The number of images should equals the number of labels");
         Debug.Assert(eigenDistanceThreshold >= 0.0, "Eigen-distance threshold should always >= 0.0");

         CalcEigenObjects(images, ref termCrit, out _eigenImages, out _avgImage);

         /*
         _avgImage.SerializationCompressionRatio = 9;

         foreach (Image<Gray, Single> img in _eigenImages)
             //Set the compression ration to best compression. The serialized object can therefore save spaces
             img.SerializationCompressionRatio = 9;
         */

         _eigenValues = Array.ConvertAll<Image<Gray, Byte>, Matrix<float>>(images,
             delegate(Image<Gray, Byte> img)
             {
                return new Matrix<float>(EigenDecomposite(img, _eigenImages, _avgImage));
             });

         _labels = labels;

         _eigenDistanceThreshold = eigenDistanceThreshold;
      }

      #region static methods
      /// <summary>
      /// Caculate the eigen images for the specific traning image
      /// </summary>
      /// <param name="trainingImages">The images used for training </param>
      /// <param name="termCrit">The criteria for tranning</param>
      /// <param name="eigenImages">The resulting eigen images</param>
      /// <param name="avg">The resulting average image</param>
      public static void CalcEigenObjects(Image<Gray, Byte>[] trainingImages, ref MCvTermCriteria termCrit, out Image<Gray, Single>[] eigenImages, out Image<Gray, Single> avg)
      {
         int width = trainingImages[0].Width;
         int height = trainingImages[0].Height;

         IntPtr[] inObjs = Array.ConvertAll<Image<Gray, Byte>, IntPtr>(trainingImages, delegate(Image<Gray, Byte> img) { return img.Ptr; });

         if (termCrit.max_iter <= 0 || termCrit.max_iter > trainingImages.Length)
            termCrit.max_iter = trainingImages.Length;
         
         int maxEigenObjs = termCrit.max_iter;

         #region initialize eigen images
         eigenImages = new Image<Gray, float>[maxEigenObjs];
         for (int i = 0; i < eigenImages.Length; i++)
            eigenImages[i] = new Image<Gray, float>(width, height);
         IntPtr[] eigObjs = Array.ConvertAll<Image<Gray, Single>, IntPtr>(eigenImages, delegate(Image<Gray, Single> img) { return img.Ptr; });
         #endregion

         avg = new Image<Gray, Single>(width, height);

         CvInvoke.cvCalcEigenObjects(
             inObjs,
             ref termCrit,
             eigObjs,
             null,
             avg.Ptr);
      }

      /// <summary>
      /// Decompose the image as eigen values, using the specific eigen vectors
      /// </summary>
      /// <param name="src">The image to be decomposed</param>
      /// <param name="eigenImages">The eigen images</param>
      /// <param name="avg">The average images</param>
      /// <returns>Eigen values of the decomposed image</returns>
      public static float[] EigenDecomposite(Image<Gray, Byte> src, Image<Gray, Single>[] eigenImages, Image<Gray, Single> avg)
      {
         return CvInvoke.cvEigenDecomposite(
             src.Ptr,
             Array.ConvertAll<Image<Gray, Single>, IntPtr>(eigenImages, delegate(Image<Gray, Single> img) { return img.Ptr; }),
             avg.Ptr);
      }
      #endregion

      /// <summary>
      /// Given the eigen value, reconstruct the projected image
      /// </summary>
      /// <param name="eigenValue">The eigen values</param>
      /// <returns>The projected image</returns>
      public Image<Gray, Byte> EigenProjection(float[] eigenValue)
      {
         Image<Gray, Byte> res = new Image<Gray, byte>(_avgImage.Width, _avgImage.Height);
         CvInvoke.cvEigenProjection(
             Array.ConvertAll<Image<Gray, Single>, IntPtr>(_eigenImages, delegate(Image<Gray, Single> img) { return img.Ptr; }),
             eigenValue,
             _avgImage.Ptr,
             res.Ptr);
         return res;
      }

      /// <summary>
      /// Get the Euclidean eigen-distance between <paramref name="image"/> and every other image in the database
      /// </summary>
      /// <param name="image">The image to be compared from the training images</param>
      /// <returns>An array of eigen distance from every image in the training images</returns>
      public float[] GetEigenDistances(Image<Gray, Byte> image)
      {
         using (Matrix<float> eigenValue = new Matrix<float>(EigenDecomposite(image, _eigenImages, _avgImage)))
            return Array.ConvertAll<Matrix<float>, float>(_eigenValues,
                delegate(Matrix<float> eigenValueI)
                {
                   return (float)CvInvoke.cvNorm(eigenValue.Ptr, eigenValueI.Ptr, Emgu.CV.CvEnum.NORM_TYPE.CV_L2, IntPtr.Zero);
                });
      }

      /// <summary>
      /// Given the <paramref name="image"/> to be examined, find in the database the most similar object, return the index and the eigen distance
      /// </summary>
      /// <param name="image">The image to be searched from the database</param>
      /// <param name="index">The index of the most similar object</param>
      /// <param name="eigenDistance">The eigen distance of the most similar object</param>
      /// <param name="label">The label of the specific image</param>
      public void FindMostSimilarObject(Image<Gray, Byte> image, out int index, out float eigenDistance, out String label)
      {
         float[] dist = GetEigenDistances(image);

         index = 0;
         eigenDistance = dist[0];
         for (int i = 1; i < dist.Length; i++)
         {
            if (dist[i] < eigenDistance)
            {
               index = i;
               eigenDistance = dist[i];
            }
         }
         label = Labels[index];
      }


      public float GetEigenDistance(Image<Gray, Byte> image, Matrix<float> trainEigenValue)
      {
          using (Matrix<float> eigenValue = new Matrix<float>(EigenDecomposite(image, _eigenImages, _avgImage)))
              return (float)CvInvoke.cvNorm(eigenValue.Ptr, trainEigenValue.Ptr, Emgu.CV.CvEnum.NORM_TYPE.CV_L2, IntPtr.Zero);
      }

      private void log(string message)
      {
          File.AppendAllText(this.appPath + "/log.txt", message + "\n");
          Console.WriteLine(message);
      }

      private void findMost(Image<Gray, Byte> testImage, int trainStartIndex, int trainImageCount, out int index, out float eigenDistance)
      {
          int col = 0;
          this.log("========================");
          this.log("test: " + trainStartIndex);
          log("col: " + col);
          // Pick the first train image for each person.
          // key: start 0
          Dictionary<int, float> persons = new Dictionary<int, float>();
          int totalImageCount = this._eigenValues.Length;
          int personCount = totalImageCount / trainImageCount;
          for (int i = 0; i < personCount; i++)
          {
              int key = (trainStartIndex + i * trainImageCount) % totalImageCount;
              float dist = GetEigenDistance(testImage, _eigenValues[key]);
              persons[key] = dist;

              log("index, name, image (score):" + key + ", " + this._labels[key] + ", faces" + (key + 1) + ", " + dist);
          }

          col++;
          // Divide the group and find the shortest eigen distance.
          IEnumerable<KeyValuePair<int, float>> sortedDict = null;
          while (persons.Count / DIVID > 0)
          {
              log("col: " + col);
              // sort the persons based on its score and take top half.
              sortedDict = (from entry in persons orderby entry.Value ascending select entry).Take(persons.Count / DIVID);

              persons = new Dictionary<int, float>();
              foreach (KeyValuePair<int, float> iter in sortedDict)
              {
                  // use the next colum
                  int personIndex = iter.Key / 10;
                  int i = personIndex * 10 + (iter.Key + 1) % 10;
                  float dist = GetEigenDistance(testImage, _eigenValues[i]);
                  log("index, name, image (score):" + i + ", " + this._labels[i] + ", faces" + (i + 1) + ", " + dist);
                  persons[i] = dist;
              }

              col++;
          }

          index = persons.First().Key;
          eigenDistance = persons.First().Value;
      }

      /// <summary>
      /// Try to recognize the image and return its label
      /// </summary>
      /// <param name="image">The image to be recognized</param>
      /// <returns>
      /// String.Empty, if not recognized;
      /// Label of the corresponding image, otherwise
      /// </returns>
      public String Recognize(Image<Gray, Byte> image, int trainStartIndex, int trainCount)
      {
         int index;
         float eigenDistance;
         //String label;
         //FindMostSimilarObject(image, out index, out eigenDistance, out label);
         findMost(image, trainStartIndex, trainCount, out index, out eigenDistance);

         return _labels[index];
      }
   }
}
