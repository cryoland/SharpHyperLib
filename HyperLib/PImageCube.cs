using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using MiscUtil;
using MiscUtil.Conversion;
using System.Drawing;
using System.Threading.Tasks;
using BitmapProcessing;
using System.Threading;
namespace HyperLib
{
    /********************************************************************
     * BSQ (Band Sequential Format) 
        In its simplest form, the data is in BSQ format, with each line of the data 
        followed immediately by the next line in the same spectral band. This format is 
        optimal for spatial (X, Y) access of any part of a single spectral band. 
     * -----------------------------------------------------------------------------
     * BIP (Band Interleaved by Pixel Format) 
        Images stored in BIP format have the first pixel for all bands in sequential 
        order, followed by the second pixel for all bands, followed by the third pixel 
        for all bands, etc., interleaved up to the number of pixels. This format 
        provides optimum performance for spectral (Z) access of the image data.    
     * -----------------------------------------------------------------------------
     * BIL (Band Interleaved by Line Format) 
        Images stored in BIL format have the first line of the first band followed by 
        the first line of the second band, followed by the first line of the third band, 
        interleaved up to the number of bands. Subsequent lines for each band are 
        interleaved in similar fashion. This format provides a compromise in performance between 
        spatial and spectral processing and is the recommended file format for most ENVI processing tasks. 
     * LINK:http://www.biostat.wustl.edu/archives/html/s-news/2005-05/msg00107.html
     * *********************************************************************/
    delegate void DelLoadImageCube(int bandStartIndex, int BandEndIndex);
    public class PImageCube : SpectralCube
    {

        public PImageCube(string pictureFilePath)
            : base(pictureFilePath)
        {
            ;
        }
        //..........................................................................
        public Bitmap ToBitmap(int redIndex, int greenIndex, int blueIndex)
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

        public static Bitmap ToBitmap(double[, ,] imageVector, int redIndex, int greenIndex, int blueIndex)
        {
            PImageCube image = new PImageCube("");
            image.ImageVector = imageVector;
            return image.ToBitmap(redIndex, greenIndex, blueIndex);
        }


        private double AssignPixelValue(BinaryReader imageReader)
        {
            if (Header.ByteOrder != 1)
                return LoadPixelBandValue(imageReader);
            else
                return LoadPixelBandValue_BigEndian(imageReader);
        }

        private double LoadPixelBandValue(BinaryReader imageReader)
        {
            if (Header.ByteOrder != 0)
                return LoadPixelBandValue_BigEndian(imageReader);
            else

                switch (PixelType)
                {
                    case Enumerations.uint8:
                        return Convert.ToDouble(imageReader.ReadByte());
                    case Enumerations.int16:
                        return Convert.ToDouble(imageReader.ReadInt16());
                    case Enumerations.int32:
                        return Convert.ToDouble(imageReader.ReadInt32());
                    case Enumerations.single:
                        return Convert.ToDouble(imageReader.ReadSingle());
                    case Enumerations.envidouble:
                        return Convert.ToDouble(imageReader.ReadDouble());
                    case Enumerations.uint32:
                        return Convert.ToDouble(imageReader.ReadUInt32());
                    case Enumerations.int64:
                        return Convert.ToDouble(imageReader.ReadInt64());
                    case Enumerations.uint16:
                        return Convert.ToDouble(imageReader.ReadUInt16());
                    default:
                        return 0;
                }

        }

        //...................................................
        private double LoadPixelBandValue_BigEndian(BinaryReader imageReader)
        {
            if (Header.ByteOrder != 1)
                return LoadPixelBandValue(imageReader);
            else
                switch (PixelType)
                {
                    case Enumerations.uint8:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToSingle(new byte[] { imageReader.ReadByte() }, 0));
                    case Enumerations.int16:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt16(imageReader.ReadBytes(2), 0));
                    case Enumerations.int32:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt32(imageReader.ReadBytes(4), 0));
                    case Enumerations.single:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToSingle(imageReader.ReadBytes(4), 0));
                    case Enumerations.envidouble:
                        return MiscUtil.Conversion.BigEndianBitConverter.Big.ToDouble(imageReader.ReadBytes(8), 0);
                    case Enumerations.uint32:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToUInt32(imageReader.ReadBytes(4), 0));
                    case Enumerations.int64:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt64(imageReader.ReadBytes(8), 0));
                    case Enumerations.uint16:
                        return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToUInt16(imageReader.ReadBytes(2), 0));
                    default:
                        return 0;
                }
        }


        private bool PrepareImageVector()
        {
            ImageVector = new double[Header.Samples, Header.Lines, Header.Bands];
            //............................................................
            _maxValues = MaxLoopValues();
            return true;
        }

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



        public double[, ,] ImageVector
        {
            get;
            private set;
        }
        public double[, ,] LoadImageCube()
        {
            PrepareImageVector();
            if (string.IsNullOrEmpty(_PictureFilePath) || Header == null)
            {
                return null;
            }
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                if (Header.Interleave == Interleave.BSQ)
                {
                    for (int b = 0; b < Header.Bands; b++)
                    {
                        for (int l = 0; l < Header.Lines; l++)
                        {
                            for (int s = 0; s < Header.Samples; s++)
                            {
                                ImageVector[s, l, b] = AssignPixelValue(imageReader);
                            }

                        }
                    }
                }
                else if (Header.Interleave == Interleave.BIL)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int b = 0; b < Header.Bands; b++)
                        {
                            for (int s = 0; s < Header.Samples; s++)
                            {
                                ImageVector[s, l, b] = AssignPixelValue(imageReader);
                            }

                        }
                    }
                }
                else if (Header.Interleave == Interleave.BIP)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            for (int b = 0; b < Header.Bands; b++)
                            {
                                ImageVector[s, l, b] = AssignPixelValue(imageReader);
                            }

                        }
                    }
                }
            }
            return ImageVector;
        }
        public double[, ,] PLoadImageCube()
        {
            PrepareImageVector();
            if (string.IsNullOrEmpty(_PictureFilePath) || Header == null)
            {
                return null;
            }
            int coreCount = Environment.ProcessorCount;
            //.............................................................
            if (coreCount > 1)
            {
                int bandrange = Header.Bands / coreCount;
                List<IAsyncResult> loadingdels = new List<IAsyncResult>(coreCount + 1);
                for (int i = 0; i < (coreCount - 1); i++)
                {
                    DelLoadImageCube del = new DelLoadImageCube(BSQ_PloadImageCube);
                    IAsyncResult result = del.BeginInvoke(i * bandrange, (i + 1) * bandrange, null, new object());
                    loadingdels.Add(result);
                }
                DelLoadImageCube delend = new DelLoadImageCube(BSQ_PloadImageCube);
                IAsyncResult resultEnd = delend.BeginInvoke((coreCount - 1) * bandrange, Header.Bands, null, new object());
                loadingdels.Add(resultEnd);
                //.................................................
                foreach (var item in loadingdels)
                {
                    while (item.IsCompleted == false)
                    {
                        // do some work
                        Thread.Sleep(10);
                    }
                }
                //..............................................
                return ImageVector;
            }
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                if (Header.Interleave == Interleave.BSQ)
                {
                    for (int b = 0; b < Header.Bands; b++)
                    {
                        for (int l = 0; l < Header.Lines; l++)
                        {
                            for (int s = 0; s < Header.Samples; s++)
                            {
                                ImageVector[s, l, b] = AssignPixelValue(imageReader);
                            }

                        }
                    }
                }
                else if (Header.Interleave == Interleave.BIL)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int b = 0; b < Header.Bands; b++)
                        {
                            for (int s = 0; s < Header.Samples; s++)
                            {
                                ImageVector[s, l, b] = AssignPixelValue(imageReader);
                            }

                        }
                    }
                }
                else if (Header.Interleave == Interleave.BIP)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            for (int b = 0; b < Header.Bands; b++)
                            {
                                ImageVector[s, l, b] = AssignPixelValue(imageReader);
                            }

                        }
                    }
                }
            }
            return ImageVector;
        }
        public double[, ,] LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            if (Header.Interleave == Interleave.BSQ)
            {
                return BSQ_LoadImageCube_withSubWindow(currentWindowToLoad);
            }
            else if (Header.Interleave == Interleave.BIL)
            {
                return BIL_LoadImageCube_withSubWindow(currentWindowToLoad);
            }
            else if (Header.Interleave == Interleave.BIP)
            {
                return BIP_LoadImageCube_withSubWindow(currentWindowToLoad);
            }
            else
            {
                return null;
            }

        }
        //.....................................................................................
        private double[, ,] BSQ_LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int b = 0; b < Header.Bands; b++)
                {
                    long currentBandStartPosition = imageReader.BaseStream.Position;
                    //Set Seek Position -- CurrentWindowToLoad.yStart * Header.Samples * sizeof(byte)
                    imageReader.BaseStream.Position += currentWindowToLoad.yStart * Header.Samples * PixelType.Size();
                    //.................................................................
                    for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                    {
                        //Set Seek Position -- CurrentWindowToLoad.xStart * sizeof(byte)
                        imageReader.BaseStream.Position += currentWindowToLoad.xStart * PixelType.Size();

                        for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                        {
                            ImageVector[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = LoadPixelBandValue(imageReader);
                        }
                        //Set Seek Position -- (CurrentWindowToLoad.xEnd - Header.Samples) * sizeof(byte)
                        if (currentWindowToLoad.xEnd < Header.Samples)
                        {
                            int remainderSamples = Header.Samples - currentWindowToLoad.xEnd;
                            imageReader.BaseStream.Position += remainderSamples * PixelType.Size();
                        }
                    }
                    //Set Seek Position -- (CurrentWindowToLoad.yEnd - Header.Lines) * Header.Samples * sizeof(byte)
                    if (currentWindowToLoad.yEnd < Header.Lines)
                    {
                        int remainderlines = Header.Lines - currentWindowToLoad.yEnd;
                        imageReader.BaseStream.Position += remainderlines * Header.Samples * PixelType.Size();
                    }
                }
            }
            return ImageVector;
        }
        private double[, ,] BIL_LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                //  Set Seek Position -- Skip NON required Lines                
                imageReader.BaseStream.Position += currentWindowToLoad.yStart * Header.Bands * Header.Samples * PixelType.Size();
                //........................................................
                for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                {
                    for (int b = 0; b < Header.Bands; b++)
                    {
                        //Set Seek Position -- SKIP NON Needed Samples @ Start
                        imageReader.BaseStream.Position += currentWindowToLoad.xStart * PixelType.Size();
                        //.................................................................                        
                        for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                        {
                            ImageVector[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = LoadPixelBandValue(imageReader);
                        }
                        //Set Seek Position -- SKIP NON Needed Samples @ END
                        if (currentWindowToLoad.xEnd < Header.Samples)
                        {
                            int remainderSamples = Header.Samples - currentWindowToLoad.xEnd;
                            imageReader.BaseStream.Position += remainderSamples * PixelType.Size();
                        }
                    }
                    //Set Seek Position -- SKIP NON Needed Lines @ END
                    if (currentWindowToLoad.yEnd < Header.Lines)
                    {
                        int remainderlines = Header.Lines - currentWindowToLoad.yEnd;
                        imageReader.BaseStream.Position += remainderlines * Header.Samples * PixelType.Size();
                    }
                }
            }
            return ImageVector;
        }
        private double[, ,] BIP_LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            double[, ,] ImagePixels = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                // Set Seek Position -- Pixels NOT needed @ Start
                imageReader.BaseStream.Position += (currentWindowToLoad.yStart * Header.Samples * Header.Bands * PixelType.Size()); // Skip Not needed LINES to reach the start line                   
                //...............................................................................................
                for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                {
                    if (currentWindowToLoad.xStart != 0)
                    {
                        imageReader.BaseStream.Position += (currentWindowToLoad.xStart * Header.Bands * PixelType.Size());// Skip Not needed SAMPLES to reach the start
                    }
                    for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                    {
                        for (int b = 0; b < Header.Bands; b++)
                        {
                            ImagePixels[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = LoadPixelBandValue(imageReader);
                        }
                        // Set Seek Position -- (CurrentWindowToLoad.xEnd - Header.Samples) * sizeof(byte)
                        if (currentWindowToLoad.xEnd < Header.Samples)
                        {
                            int remainderSamples = Header.Samples - currentWindowToLoad.xEnd;
                            imageReader.BaseStream.Position += remainderSamples * Header.Bands * PixelType.Size();
                        }
                    }
                }
            }
            ImageVector = ImagePixels;
            return ImageVector;
        }
        //.......................................................................................
        public double[, ,] BSQ_LoadImageSingleBand(int bandIndex)
        {
            if (Header.Interleave == Interleave.BSQ)
            {
                return BSQ_LoadImageSingleBand(null, bandIndex);
            }
            else if (Header.Interleave == Interleave.BIL)
            {
                return BIL_LoadImageSingleBand(null, bandIndex);
            }
            else if (Header.Interleave == Interleave.BIP)
            {
                return BIP_LoadImageSingleBand(null, bandIndex);
            }
            return null;
        }
        public double[, ,] LoadImageSingleBand(ImageSubWindow currentWindowToLoad, int bandIndex)
        {
            if (Header.Interleave == Interleave.BSQ)
            {
                return BSQ_LoadImageSingleBand(currentWindowToLoad, bandIndex);
            }
            else if (Header.Interleave == Interleave.BIL)
            {
                return BIL_LoadImageSingleBand(currentWindowToLoad, bandIndex);
            }
            else if (Header.Interleave == Interleave.BIP)
            {
                return BIP_LoadImageSingleBand(currentWindowToLoad, bandIndex);
            }
            return null;
        }
        private double[, ,] BSQ_LoadImageSingleBand(ImageSubWindow currentWindowToLoad, int bandIndex)
        {
            if (currentWindowToLoad == null)
            {
                currentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = Header.Samples, yStart = 0, yEnd = Header.Lines };
            }
            //..............................................................
            ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                imageReader.BaseStream.Position += bandIndex * Header.Lines * Header.Samples * PixelType.Size();

                imageReader.BaseStream.Position += currentWindowToLoad.yStart * Header.Samples * PixelType.Size();
                //................................................................................
                for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                {
                    imageReader.BaseStream.Position += currentWindowToLoad.xStart * PixelType.Size();
                    for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                    {
                        ImageVector[s, l, 0] = LoadPixelBandValue(imageReader);
                    }
                    if (currentWindowToLoad.xEnd < Header.Samples)
                    {
                        int remainderSamples = Header.Samples - currentWindowToLoad.xEnd;
                        imageReader.BaseStream.Position += remainderSamples * PixelType.Size();

                    }
                }
                //.................................................................
            }
            return ImageVector;
        }
        //..........................................................................
        private double[, ,] BIL_LoadImageSingleBand(ImageSubWindow currentWindowToLoad, int bandIndex)
        {
            if (currentWindowToLoad == null)
            {
                currentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = Header.Samples, yStart = 0, yEnd = Header.Lines };
            }
            //..............................................................
            ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                imageReader.BaseStream.Position += currentWindowToLoad.yStart * Header.Samples * Header.Bands * PixelType.Size();
                //....................................................................................
                for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                {
                    imageReader.BaseStream.Position += bandIndex * Header.Samples * PixelType.Size();
                    //.................................................................................
                    imageReader.BaseStream.Position += currentWindowToLoad.xStart * PixelType.Size();
                    for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                    {
                        ImageVector[s, l, 0] = LoadPixelBandValue(imageReader);
                    }
                    if (currentWindowToLoad.xEnd < Header.Samples)
                    {
                        int remainderSamples = Header.Samples - currentWindowToLoad.xEnd;
                        imageReader.BaseStream.Position += remainderSamples * PixelType.Size();
                    }
                    if (bandIndex < (Header.Bands - 1))
                    {
                        int remainderBands = Header.Bands - (bandIndex + 1);
                        imageReader.BaseStream.Position += remainderBands * Header.Samples * PixelType.Size();
                    }
                }
                //.................................................................
            }
            return ImageVector;
        }
        private double[, ,] BIP_LoadImageSingleBand(ImageSubWindow currentWindowToLoad, int bandIndex)
        {
            if (currentWindowToLoad == null)
            {
                currentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = Header.Samples, yStart = 0, yEnd = Header.Lines };
            }
            //..............................................................
            ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                imageReader.BaseStream.Position += currentWindowToLoad.yStart * Header.Samples * Header.Bands * PixelType.Size();
                //....................................................................................
                for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                {
                    //.................................................................................
                    imageReader.BaseStream.Position += currentWindowToLoad.xStart * Header.Bands * PixelType.Size();
                    for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                    {
                        imageReader.BaseStream.Position += bandIndex * PixelType.Size();
                        ImageVector[s, l, 0] = LoadPixelBandValue(imageReader);
                        if (bandIndex < (Header.Bands - 1))
                        {
                            int remainderBands = Header.Bands - (bandIndex + 1);
                            imageReader.BaseStream.Position += remainderBands * PixelType.Size();
                        }
                    }
                    if (currentWindowToLoad.xEnd < Header.Samples)
                    {
                        int remainderSamples = Header.Samples - currentWindowToLoad.xEnd;
                        imageReader.BaseStream.Position += remainderSamples * Header.Bands * PixelType.Size();
                    }

                }
                //.................................................................
            }
            return ImageVector;
        }
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

        private void BSQ_PloadImageCube(int bandIndexStart, int bandIndexEnd)
        {
            using (BinaryReader imageReader = new BinaryReader(new FileStream(_PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                if (Header.Interleave == Interleave.BSQ)
                {
                    imageReader.BaseStream.Position += bandIndexStart * Header.Lines * Header.Samples * PixelType.Size();
                    //...................................................................................
                    for (int b = bandIndexStart; b < bandIndexEnd; b++)
                    {
                        for (int l = 0; l < Header.Lines; l++)
                        {
                            BSQ_PLoadPixelBandValue(imageReader, b, l);
                        }
                    }
                }
                else if (Header.Interleave == Interleave.BIL)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int b = 0; b < Header.Bands; b++)
                        {
                            BSQ_PLoadPixelBandValue(imageReader, b, l);

                        }
                    }
                }
                else if (Header.Interleave == Interleave.BIP)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            BIP_PLoadPixelBandValue(imageReader, s, l);
                        }
                    }
                }
            }
        }
        private void BSQ_PLoadPixelBandValue(BinaryReader imageReader, int b, int l)
        {
            byte[] buffer = new byte[Header.Samples * PixelType.Size()];
            buffer = imageReader.ReadBytes(Header.Samples * PixelType.Size());
            int inc = PixelType.Size();
            switch (PixelType)
            {
                case Enumerations.uint8:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        ImageVector[i, l, b] = (Byte)(buffer[i]);
                    });
                    break;
                case Enumerations.int16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToInt16(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.single:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToSingle(temp, 0);
                        }
                    });
                    break;
                case Enumerations.envidouble:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToDouble(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToUInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int64:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToInt64(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BitConverter.ToUInt16(temp, 0);
                        }
                    });
                    break;
                default:
                    break;
            }
        }
        private void BSQ_LoadPixelBandValue_BigEndian(BinaryReader imageReader, int b, int l)
        {
            byte[] buffer = new byte[Header.Samples * PixelType.Size()];
            buffer = imageReader.ReadBytes(Header.Samples * PixelType.Size());
            int inc = PixelType.Size();
            switch (PixelType)
            {
                case Enumerations.uint8:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        ImageVector[i, l, b] = BigEndianBitConverter.Big.ToSingle(new byte[] { buffer[i] }, 0);
                    });
                    break;
                case Enumerations.int16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToInt16(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.single:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToSingle(temp, 0);
                        }
                    });
                    break;
                case Enumerations.envidouble:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToDouble(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToUInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int64:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToInt64(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[(i / inc), l, b] = BigEndianBitConverter.Big.ToUInt16(temp, 0);
                        }
                    });
                    break;
                default:
                    break;
            }
        }
        private void AssignPixelValue(BinaryReader reader, int b, int l)
        {
            if (Header.ByteOrder == 0)
                BSQ_PLoadPixelBandValue(reader, b, 1);
            else
                BSQ_LoadPixelBandValue_BigEndian(reader, b, l);
        }
        //........................................................................................
        private void BIP_PLoadPixelBandValue(BinaryReader imageReader, int s, int l)
        {
            byte[] buffer = new byte[Header.Bands * PixelType.Size()];
            buffer = imageReader.ReadBytes(Header.Bands * PixelType.Size());
            int inc = PixelType.Size();
            switch (PixelType)
            {
                case Enumerations.uint8:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        ImageVector[s, l, i] = (Byte)(buffer[i]);
                    });
                    break;
                case Enumerations.int16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToInt16(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.single:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToSingle(temp, 0);
                        }
                    });
                    break;
                case Enumerations.envidouble:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToDouble(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToUInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int64:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToInt64(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BitConverter.ToUInt16(temp, 0);
                        }
                    });
                    break;
                default:
                    break;
            }
        }
        private void BIP_LoadPixelBandValue_BigEndian(BinaryReader imageReader, int s, int l)
        {
            byte[] buffer = new byte[Header.Bands * PixelType.Size()];
            buffer = imageReader.ReadBytes(Header.Bands * PixelType.Size());
            int inc = PixelType.Size();
            switch (PixelType)
            {
                case Enumerations.uint8:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        ImageVector[s, l, i] = BigEndianBitConverter.Big.ToSingle(new byte[] { buffer[i] }, 0);
                    });
                    break;
                case Enumerations.int16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToInt16(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.single:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToSingle(temp, 0);
                        }
                    });
                    break;
                case Enumerations.envidouble:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToDouble(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint32:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToUInt32(temp, 0);
                        }
                    });
                    break;
                case Enumerations.int64:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToInt64(temp, 0);
                        }
                    });
                    break;
                case Enumerations.uint16:
                    Parallel.For(0, buffer.Length, i =>
                    {
                        if (i % inc == 0 && i != 0)
                        {
                            byte[] temp = new byte[inc];
                            Array.Copy(buffer, (i - inc), temp, 0, inc);
                            ImageVector[s, l, (i / inc)] = BigEndianBitConverter.Big.ToUInt16(temp, 0);
                        }
                    });
                    break;
                default:
                    break;
            }
        }
    }
}
