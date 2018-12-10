using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using MiscUtil;
using System.Drawing;
using System.Threading.Tasks;
using BitmapProcessing;
namespace HyperLib
{
    
    public class ImageCube:SpectralCube
    {

        public ImageCube(string imagePath) : base( imagePath)
        {
            ;
        }
        /// <summary>
        /// Loads a sub section of the Hyperspectral image into a 1-dimensional array,specified by the parameter.(rows then columns)
        /// </summary>
        /// <param name="currentWindowToLoad">The required region of the image to be loaded</param>
        /// <returns>The 1-dimentional array of the sub section of the Hyperspectral image loaded </returns>
        public float[] LoadImageCube_withSubWindow_InSingleArray(ImageSubWindow currentWindowToLoad)
        {
            return currentFormateObj.LoadImageCube_withSubWindow_InSingleArray(currentWindowToLoad);
        }
        /// <summary>
        ///  Converts the current ImageCube to a Bitmap object
        /// </summary>
        /// <param name="redIndex">The band index representing Red channel</param>
        /// <param name="greenIndex">The band index representing Green channel</param>
        /// <param name="blueIndex">The band index representing Blue channel</param>
        /// <returns>A bitmap of the current ImageCube</returns>
        public  Bitmap ToBitmap(int redIndex, int greenIndex, int blueIndex)
        {          
            int[, ,] triBandImageVector = NormalizeImageVectorForDisplay(redIndex, greenIndex, blueIndex);
            Bitmap inputImage = new Bitmap(ImageVector.GetLength(0), ImageVector.GetLength(1));
            FastBitmap currentTRIBandImage = new FastBitmap(inputImage);
            currentTRIBandImage.LockImage();
            for (int i = 0; i < inputImage.Width; i++)
            {
                for (int j = 0; j < inputImage.Height; j++)
                {
                    currentTRIBandImage.SetPixel(i, j, Color.FromArgb(triBandImageVector[i, j, 0], triBandImageVector[i, j, 1], triBandImageVector[i, j, 2]));
                }
            }
            currentTRIBandImage.UnlockImage();
            return inputImage;
        }
        /// <summary>
        /// Converts the input ImageCube vector to a Bitmap object
        /// </summary>
        /// <param name="imageVector">3- dimensional vector representing the image cube</param>
        /// <param name="redIndex">The band index representing Red channel</param>
        /// <param name="greenIndex">The band index representing Green channel</param>
        /// <param name="blueIndex">The band index representing Blue channel</param>
        /// <returns></returns>
        public static Bitmap ToBitmap(double[,,] imageVector, int redIndex, int greenIndex, int blueIndex)
        {
            ImageCube image = new ImageCube("");
            image.ImageVector = imageVector;
            return image.ToBitmap(redIndex, greenIndex, blueIndex);
        }
        /// <summary>
        /// Used internally to find the max. and min. values of the band.
        /// </summary>
        /// <param name="colorBandIndex">The band index</param>
        /// <returns>an array of 2 values , [0]: min value, [1]: Max. value</returns>
        private double[] RangeValuesFor(int colorBandIndex)
        {
            double[] maxValue = new double[2];
            Parallel.For(0, ImageVector.GetLength(0), i =>
            //for (int i = 0; i < ImageVector.GetLength(0); i++)
            {
                for (int j = 0; j < ImageVector.GetLength(1); j++)
                {
                    if (maxValue[0] < ImageVector[i, j, colorBandIndex])
                        maxValue[0] = ImageVector[i, j, colorBandIndex];
                    //...........................................................
                    if (maxValue[1] > ImageVector[i, j, colorBandIndex])
                        maxValue[1] = ImageVector[i, j, colorBandIndex];
                }
            });
            return maxValue;
        }
        /// <summary>
        /// Loads the Whole Hyperspectral Image into a 3-dimensional array , Saved in the Property : ImageVector
        /// </summary>
        /// <returns>The 3-dimentional array loaded</returns>
        public double[, ,] LoadImageCube()
        {
            ImageVector = currentFormateObj.LoadImageCube();
            return ImageVector;
        }
        /// <summary>
        /// Loads a sub section of the Hyperspectral image into a 3-dimensional array,specified by the parameter. Saved in the Property : ImageVector
        /// </summary>
        /// <param name="currentWindowToLoad">The required region of the image to be loaded</param>
        /// <returns>The 3-dimentional array of the sub section of the Hyperspectral image loaded </returns>
        public double[, ,] LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            ImageVector = currentFormateObj.LoadImageCube_withSubWindow(currentWindowToLoad);
            return ImageVector;
        }
        /// <summary>
        /// Load a sub section of the hyperspectral image with ONLY a Single band.
        /// </summary>
        /// <param name="bandIndex">A zero based index of the band required for loading</param>
        /// <param name="currentWindowToLoad">The required region of the image to be loaded</param>
        /// <returns>The 3-dimentional array of the sub section of the Hyperspectral image loaded </returns>
        public double[, ,] LoadImageSingleBand(int bandIndex, ImageSubWindow currentWindowToLoad)
        {
            if (bandIndex < 0)
            {
                throw new Exception("bandIndex is less than zero");
            }
            if (bandIndex >= Header.Bands)
            {
                throw new Exception("bandIndex exceeded the image available bands");
            }
            ImageVector = currentFormateObj.LoadImageSingleBand(bandIndex,currentWindowToLoad);
            return ImageVector;
        }
        /// <summary>
        /// Used interrnally to normalize the image vector for RGB display
        /// </summary>
        /// <param name="redIndex">The band index representing Red channel</param>
        /// <param name="greenIndex">The band index representing Green channel</param>
        /// <param name="blueIndex">The band index representing Blue channel</param>
        /// <returns>3-dimentional normalized image ready for display</returns>
        private int[, ,] NormalizeImageVectorForDisplay(int redIndex, int greenIndex, int blueIndex)
        {
            int[, ,] triBandImageVector = new int[ImageVector.GetLength(0), ImageVector.GetLength(1), 3];
            //.................................................................
            double[] redmaxValue = RangeValuesFor(redIndex);
            double[] greenmaxValue = RangeValuesFor(greenIndex);
            double[] bluemaxValue = RangeValuesFor(blueIndex);
            //.................................................................
            int redRange = (int)(redmaxValue[0] - redmaxValue[1]);
            int greenRange = (int)(greenmaxValue[0] - greenmaxValue[1]);
            int blueRange = (int)(bluemaxValue[0] - bluemaxValue[1]);
            for (int i = 0; i < ImageVector.GetLength(0); i++)
            {
                for (int j = 0; j < ImageVector.GetLength(1); j++)
                {
                    triBandImageVector[i, j, 0] = (int)(ImageVector[i, j, redIndex] * 255d / redmaxValue[0]);
                    triBandImageVector[i, j, 1] = (int)(ImageVector[i, j, greenIndex] * 255d / greenmaxValue[0]);
                    triBandImageVector[i, j, 2] = (int)(ImageVector[i, j, blueIndex] * 255d / bluemaxValue[0]);
                }
            }
            return triBandImageVector;
        }
    }
}
