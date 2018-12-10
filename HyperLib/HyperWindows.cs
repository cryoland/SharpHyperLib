using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
namespace HyperLib
{
    public static class HyperWindows
    {
        public static Bitmap LoadEnviBinaryImage(string _pictureFilepath, int redIndex, int greenIndex, int blueIndex)
        {          
            double[, ,] ImagePixels =HyperRead.LoadImageInMemory(_pictureFilepath);//, new ImageWindow() { xStart = 600, xEnd = 900, yStart = 300, yEnd = 800 });
            int[, ,] triBandImageVector = NormalizeImageVectorForDisplay(ImagePixels, redIndex, greenIndex, blueIndex);
            Bitmap currentTRIBandImage = new Bitmap(ImagePixels.GetLength(0), ImagePixels.GetLength(1));
            for (int i = 0; i < currentTRIBandImage.Width; i++)
            {
                for (int j = 0; j < currentTRIBandImage.Height; j++)
                {
                    currentTRIBandImage.SetPixel(i, j, Color.FromArgb(triBandImageVector[i, j, 0], triBandImageVector[i, j, 1], triBandImageVector[i, j, 2]));
                }
            }
            return currentTRIBandImage;
        }
        public static Bitmap PLoadEnviBinaryImage(string _pictureFilepath, int redIndex, int greenIndex, int blueIndex)
        {
            double[, ,] ImagePixels = HyperRead.PLoadImageInMemory(_pictureFilepath);//, new ImageWindow() { xStart = 600, xEnd = 900, yStart = 300, yEnd = 800 });
            int[, ,] triBandImageVector = PNormalizeImageVectorForDisplay(ImagePixels, redIndex, greenIndex, blueIndex);
            Bitmap currentTRIBandImage = new Bitmap(ImagePixels.GetLength(0), ImagePixels.GetLength(1));
            for (int i = 0; i < currentTRIBandImage.Width; i++)
            {
                for (int j = 0; j < currentTRIBandImage.Height; j++)
                {
                    currentTRIBandImage.SetPixel(i, j, Color.FromArgb(triBandImageVector[i, j, 0], triBandImageVector[i, j, 1], triBandImageVector[i, j, 2]));
                }
            }
            return currentTRIBandImage;
        }

        private static void ConvertImageCubeToBitmap(object currentParameterObj)
        {
            ThreadParameter currentParameter = (ThreadParameter)currentParameterObj;
            // int j = currentParameter.heightMinValue;

            for (int i = currentParameter.widthMinValue; i < currentParameter.widthMaxValue; i++)
            {
                //for (j = 0; j < currentParameter.heightMaxValue - 1; j++)
                //{
                Parallel.For(0, currentParameter.heightMaxValue, j =>
            {
                //imagebyte[i,j,0]= (byte)currentParameter.triBandImageVector[i, j, 0];
                //imagebyte[i , j ,1] = (byte)currentParameter.triBandImageVector[i, j, 1];
                //imagebyte[i , j ,2] = (byte)currentParameter.triBandImageVector[i, j, 2];

                lock (currentParameter.currentTRIBandImage)
                {
                    //if (j >= currentParameter.heightMaxValue)
                    //    continue;
                    currentParameter.currentTRIBandImage.SetPixel(i, j, Color.FromArgb(
                        currentParameter.triBandImageVector[i, j, 0],
                        currentParameter.triBandImageVector[i, j, 1],
                        currentParameter.triBandImageVector[i, j, 2]));
                }
            });
            }
        }
        private static int[, ,] NormalizeImageVectorForDisplay(double[, ,] ImageVector, int RedBandIndex, int GreenBandIndex, int BlueBandIndex)
        {
            int[, ,] triBandImageVector = new int[ImageVector.GetLength(0), ImageVector.GetLength(1), 3];
            //.................................................................
            double[] redmaxValue = RangeValuesFor(ImageVector, RedBandIndex);
            double[] greenmaxValue = RangeValuesFor(ImageVector, GreenBandIndex);
            double[] bluemaxValue = RangeValuesFor(ImageVector, BlueBandIndex);
            //.................................................................
            int redRange = (int)(redmaxValue[0] - redmaxValue[1]);
            int greenRange = (int)(greenmaxValue[0] - greenmaxValue[1]);
            int blueRange = (int)(bluemaxValue[0] - bluemaxValue[1]);
            for (int i = 0; i < ImageVector.GetLength(0); i++)
            {
                for (int j = 0; j < ImageVector.GetLength(1); j++)
                {
                    triBandImageVector[i, j, 0] = (int)(ImageVector[i, j, RedBandIndex] * 255d / redmaxValue[0]);
                    triBandImageVector[i, j, 1] = (int)(ImageVector[i, j, GreenBandIndex] * 255d / greenmaxValue[0]);
                    triBandImageVector[i, j, 2] = (int)(ImageVector[i, j, BlueBandIndex] * 255d / bluemaxValue[0]);
                }
            }
            return triBandImageVector;
        }
        private static int[, ,] PNormalizeImageVectorForDisplay(double[, ,] ImageVector, int RedBandIndex, int GreenBandIndex, int BlueBandIndex)
        {
            int[, ,] triBandImageVector = new int[ImageVector.GetLength(0), ImageVector.GetLength(1), 3];
            //.................................................................
            double[] redmaxValue = RangeValuesFor(ImageVector, RedBandIndex);
            double[] greenmaxValue = RangeValuesFor(ImageVector, GreenBandIndex);
            double[] bluemaxValue = RangeValuesFor(ImageVector, BlueBandIndex);
            //.................................................................
            int redRange = (int)(redmaxValue[0] - redmaxValue[1]);
            int greenRange = (int)(greenmaxValue[0] - greenmaxValue[1]);
            int blueRange = (int)(bluemaxValue[0] - bluemaxValue[1]);
            Parallel.For(0, ImageVector.GetLength(0), i =>
            // for (int i = 0; i < ImageVector.GetLength(0); i++)
            {
                for (int j = 0; j < ImageVector.GetLength(1); j++)
                {
                    triBandImageVector[i, j, 0] = (int)(ImageVector[i, j, RedBandIndex] * 255d / redmaxValue[0]);
                    triBandImageVector[i, j, 1] = (int)(ImageVector[i, j, GreenBandIndex] * 255d / greenmaxValue[0]);
                    triBandImageVector[i, j, 2] = (int)(ImageVector[i, j, BlueBandIndex] * 255d / bluemaxValue[0]);
                }
            });
            return triBandImageVector;
        }
        private static double[] RangeValuesFor(double[, ,] ImageVector, int ColorBandIndex)
        {
            double[] maxValue = new double[2];
            Parallel.For(0, ImageVector.GetLength(0), i =>
            //for (int i = 0; i < ImageVector.GetLength(0); i++)
            {
                for (int j = 0; j < ImageVector.GetLength(1); j++)
                {
                    if (maxValue[0] < ImageVector[i, j, ColorBandIndex])
                        maxValue[0] = ImageVector[i, j, ColorBandIndex];
                    //...........................................................
                    if (maxValue[1] > ImageVector[i, j, ColorBandIndex])
                        maxValue[1] = ImageVector[i, j, ColorBandIndex];
                }
            });
            return maxValue;
        }

        delegate void DelConvertImageCubeToBitmap(ThreadParameter currentParameter);
        class ThreadParameter
        {
            public int[, ,] triBandImageVector { get; set; }
            public Bitmap currentTRIBandImage { get; set; }
            public int widthMinValue { get; set; }
            public int widthMaxValue { get; set; }
            public int heightMinValue { get; set; }
            public int heightMaxValue { get; set; }
            public MemoryStream bufferstream { get; set; }
            public int Index { get; set; }
        }
    }
}
