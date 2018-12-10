using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace HyperLib
{
    public static partial class HyperRead
    {
        delegate void DelThreadMethod(string pictureFilepath, HeaderInfo info, DataType currentReadingType, double[, ,] ImagePixels, int[] maxValues);
        public static double[, ,] PLoadImageInMemory(string pictureFilepath)
        {
            HeaderInfo info = LoadHeaderFile(pictureFilepath);
            DataType currentReadingType = GetImageBinaryFormatFromHeaderInfo(info);
            double[, ,] ImagePixels = new double[info.Samples, info.Lines, info.Bands];
            //............................................................
            int[] maxValues = MaxLoopValues(info);
            //..................................................................
            DelThreadMethod del1 = new DelThreadMethod(ThreadMethod);
            IAsyncResult res1= del1.BeginInvoke(pictureFilepath, info, currentReadingType, ImagePixels, maxValues, null, null);
            DelThreadMethod del2 = new DelThreadMethod(ThreadMethod2);
            IAsyncResult res2 = del2.BeginInvoke(pictureFilepath, info, currentReadingType, ImagePixels, maxValues, null, null);
            while (res1.IsCompleted == false)
            {
                // do some work
                Thread.Sleep(10);
            }
            while (res2.IsCompleted == false)
            {
                // do some work
                Thread.Sleep(10);
            }

            return ImagePixels;
        }

        private static void ThreadMethod(string pictureFilepath, HeaderInfo info, DataType currentReadingType, double[, ,] ImagePixels, int[] maxValues)
        {
            using (BinaryReader imageReader = new BinaryReader(new FileStream(pictureFilepath, FileMode.Open,FileAccess.Read,FileShare.Read)))
            {
                if (info.FileType.ToLower().Contains("bsq"))
                {
                    for (int b = 0; b < maxValues[0]/2; b++)
                    {
                        for (int l = 0; l < maxValues[1]; l++)
                        {
                            for (int s = 0; s < maxValues[2]; s++)
                            {
                                if (info.ByteOrder != 1)
                                    ImagePixels[s, l, b] = LoadPixelBandValue(currentReadingType, imageReader);
                                else
                                    ImagePixels[s, l, b] = LoadPixelBandValue_BigEndian(currentReadingType, imageReader);
                            }

                        }
                    }
                }
            }
        }
        private static void ThreadMethod2(string pictureFilepath, HeaderInfo info, DataType currentReadingType, double[, ,] ImagePixels, int[] maxValues)
        {
            using (BinaryReader imageReader = new BinaryReader(new FileStream(pictureFilepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                imageReader.BaseStream.Position = (maxValues[0] / 2) * maxValues[1] * maxValues[2] * sizeof(Int16);
                if (info.FileType.ToLower().Contains("bsq"))
                {
                    for (int b = maxValues[0]/2; b < maxValues[0]; b++)
                    {
                        for (int l =0; l < maxValues[1]; l++)
                        {
                            for (int s = 0; s < maxValues[2]; s++)
                            {
                                if (info.ByteOrder != 1)
                                    ImagePixels[s, l, b] = LoadPixelBandValue(currentReadingType, imageReader);
                                else
                                    ImagePixels[s, l, b] = LoadPixelBandValue_BigEndian(currentReadingType, imageReader);
                            }

                        }
                    }
                }
            }
        }
    }
}
