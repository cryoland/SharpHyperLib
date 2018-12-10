using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    public abstract class DataFormat
    {
        public DataFormat(HeaderInfo header, Enumerations pixelType, string pictureFilePath)
        {
            Header = header;
            PixelType = pixelType;
            PictureFilePath = pictureFilePath;
        }
        private Enumerations _pixeltype;
        private HeaderInfo _header;
        public Enumerations PixelType
        {
            get
            {
                return _pixeltype;
            }
            private set
            {
                _pixeltype = value;
            }
        }
        public HeaderInfo Header
        {
            set { _header = value; }
            protected get
            { return _header; }
        }
        public string PictureFilePath { get; set; }
        public abstract double[, ,] LoadImageCube();
        public abstract Int16[, ,] LoadRotatedImageCube();

        public abstract double[, ,] LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad);
        public abstract float[] LoadImageCube_withSubWindow_InSingleArray(ImageSubWindow currentWindowToLoad);      
        public double[, ,] LoadImageSingleBand(int bandIndex)
        {
            return LoadImageSingleBand(bandIndex, null);
        }
        public abstract double[, ,] LoadImageSingleBand(int bandIndex, ImageSubWindow currentWindowToLoad);
        public abstract void WriteImageCube_withSubWindow(ImageSubWindow currentWindowToLoad, string outputFilePath);
        protected void WriteSubWindowHeader(ImageSubWindow currentWindowToLoad, string outputFilePath)
        {
            string newHeaderFilePath = HeaderInfo.GenerateHeaderFilePathFromImage(outputFilePath);
            using (StreamReader inforeader = new StreamReader(Header.HeaderFilePath))
            {
                using (StreamWriter infoWriter = new StreamWriter(newHeaderFilePath, false, Encoding.UTF8))
                {
                    while (!inforeader.EndOfStream)
                    {
                        string currentLine = inforeader.ReadLine();
                        if (currentLine.ToLower().Contains("samples"))
                        {
                            string[] currentLineParts = currentLine.Split('=');
                            infoWriter.Write(currentLineParts[0] + " = ");
                            infoWriter.WriteLine((currentWindowToLoad.xEnd - currentWindowToLoad.xStart));
                        }
                        else if (currentLine.ToLower().Contains("lines"))
                        {
                            string[] currentLineParts = currentLine.Split('=');
                            infoWriter.Write(currentLineParts[0] + " = ");
                            infoWriter.WriteLine((currentWindowToLoad.yEnd - currentWindowToLoad.yStart));
                        }
                        else
                        {
                            infoWriter.WriteLine(currentLine);
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// BSQ (Band Sequential Format) 
    ///In its simplest form, the data is in BSQ format, with each line of the data 
    ///followed immediately by the next line in the same spectral band. This format is 
    ///optimal for spatial (X, Y) access of any part of a single spectral band.
    /// </summary>
    public class BSQDataFormat : DataFormat
    {
        #region Constructors (1)

        public BSQDataFormat(HeaderInfo header, Enumerations pixelType, string pictureFilePath)
            : base(header, pixelType, pictureFilePath)
        { }

        #endregion Constructors

        public override double[, ,] LoadImageCube()
        {
            double[, ,] ImageVector = new double[Header.Samples, Header.Lines, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int b = 0; b < Header.Bands; b++)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            ImageVector[s, l, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                        }

                    }
                }
            }
            return ImageVector;
        }
        public override Int16[, ,] LoadRotatedImageCube()
        {
            Int16[, ,] ImageVector = new Int16[Header.Bands, Header.Lines, Header.Samples];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int b = 0; b < Header.Bands; b++)
                {
                    for (int l = 0; l < Header.Lines; l++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            ImageVector[b, l, s] = (Int16)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                        }

                    }
                }
            }
            return ImageVector;
        }
        public override double[, ,] LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            double[, ,] ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                            ImageVector[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        public override float[] LoadImageCube_withSubWindow_InSingleArray(ImageSubWindow currentWindowToLoad)
        {
            float[] ImageVector = new float[(currentWindowToLoad.xEnd - currentWindowToLoad.xStart) * (currentWindowToLoad.yEnd - currentWindowToLoad.yStart) * Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                long valueCounter = 0;
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
                            ImageVector[valueCounter++] = (float)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        public override double[, ,] LoadImageSingleBand(int bandIndex, ImageSubWindow currentWindowToLoad)
        {
            if (currentWindowToLoad == null)
            {
                currentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = Header.Samples, yStart = 0, yEnd = Header.Lines };
            }
            //..............................................................
            double[, ,] ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                imageReader.BaseStream.Position += bandIndex * Header.Lines * Header.Samples * PixelType.Size();

                imageReader.BaseStream.Position += currentWindowToLoad.yStart * Header.Samples * PixelType.Size();
                //................................................................................
                for (int l = currentWindowToLoad.yStart; l < Header.Lines && l < currentWindowToLoad.yEnd; l++)
                {
                    imageReader.BaseStream.Position += currentWindowToLoad.xStart * PixelType.Size();
                    for (int s = currentWindowToLoad.xStart; s < Header.Samples && s < currentWindowToLoad.xEnd; s++)
                    {
                        ImageVector[s, l, 0] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        public override void WriteImageCube_withSubWindow(ImageSubWindow currentWindowToLoad, string outputFilePath)
        {
            // Int16[, ,] ImageVector = new Int16[Header.Bands, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, currentWindowToLoad.xEnd - currentWindowToLoad.xStart];
            BinaryWriter imageWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create));
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                            // ImageVector[b, l - currentWindowToLoad.yStart, s - currentWindowToLoad.xStart] = (Int16)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                            BinaryPixelConverter.WritePixelValue(imageWriter, imageReader, Header.ByteOrder, PixelType);
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
            imageWriter.Close();
            WriteSubWindowHeader(currentWindowToLoad, outputFilePath);
            return;

        }
    }
    /// <summary>
    /// BIL (Band Interleaved by Line Format) 
    ///  Images stored in BIL format have the first line of the first band followed by 
    ///  the first line of the second band, followed by the first line of the third band, 
    ///  interleaved up to the number of bands. Subsequent lines for each band are 
    ///  interleaved in similar fashion. This format provides a compromise in performance between 
    ///  spatial and spectral processing and is the recommended file format for most ENVI processing tasks.       
    /// </summary>
    public class BILDataFormat : DataFormat
    {
        #region Constructors (1)

        public BILDataFormat(HeaderInfo header, Enumerations pixelType, string pictureFilePath)
            : base(header, pixelType, pictureFilePath)
        { }

        #endregion Constructors

        public override double[, ,] LoadImageCube()
        {
            double[, ,] ImageVector = new double[Header.Samples, Header.Lines, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int l = 0; l < Header.Lines; l++)
                {
                    for (int b = 0; b < Header.Bands; b++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            ImageVector[s, l, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                        }

                    }
                }
            }
            return ImageVector;
        }
        public override Int16[, ,] LoadRotatedImageCube()
        {
            Int16[, ,] ImageVector = new Int16[Header.Bands, Header.Lines, Header.Samples];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int l = 0; l < Header.Lines; l++)
                {
                    for (int b = 0; b < Header.Bands; b++)
                    {
                        for (int s = 0; s < Header.Samples; s++)
                        {
                            ImageVector[b, l, s] = (Int16)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                        }

                    }
                }
            }
            return ImageVector;
        }
        public override double[, ,] LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            double[, ,] ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                            ImageVector[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        public override float[] LoadImageCube_withSubWindow_InSingleArray(ImageSubWindow currentWindowToLoad)
        {
            float[] ImageVector = new float[(currentWindowToLoad.xEnd - currentWindowToLoad.xStart) * (currentWindowToLoad.yEnd - currentWindowToLoad.yStart) * Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                int valueCounter = 0;
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
                            //ImageVector[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                            ImageVector[valueCounter++] = (float)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        public override double[, ,] LoadImageSingleBand(int bandIndex, ImageSubWindow currentWindowToLoad)
        {
            if (currentWindowToLoad == null)
            {
                currentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = Header.Samples, yStart = 0, yEnd = Header.Lines };
            }
            //..............................................................
            double[, ,] ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                        ImageVector[s, l, 0] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        //..............................................................
        public override void WriteImageCube_withSubWindow(ImageSubWindow currentWindowToLoad, string outputFilePath)
        {
            //double[, ,] ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            BinaryWriter imageWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create));
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                            //--ImageVector[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                            BinaryPixelConverter.WritePixelValue(imageWriter, imageReader, Header.ByteOrder, PixelType);
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
            imageWriter.Close();
            WriteSubWindowHeader(currentWindowToLoad, outputFilePath);
            return;
        }
    }
    /// <summary>
    ///* BIP (Band Interleaved by Pixel Format) 
    ///   Images stored in BIP format have the first pixel for all bands in sequential 
    ///   order, followed by the second pixel for all bands, followed by the third pixel 
    ///   for all bands, etc., interleaved up to the number of pixels. This format 
    ///   provides optimum performance for spectral (Z) access of the image data.    
    /// </summary>
    public class BIPDataFormat : DataFormat
    {
        #region Constructors (1)

        public BIPDataFormat(HeaderInfo header, Enumerations pixelType, string pictureFilePath)
            : base(header, pixelType, pictureFilePath)
        { }

        #endregion Constructors

        public override double[, ,] LoadImageCube()
        {
            double[, ,] ImageVector = new double[Header.Samples, Header.Lines, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int l = 0; l < Header.Lines; l++)
                {
                    for (int s = 0; s < Header.Samples; s++)
                    {
                        for (int b = 0; b < Header.Bands; b++)
                        {
                            ImageVector[s, l, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                        }

                    }
                }
            }
            return ImageVector;
        }
        public override Int16[, ,] LoadRotatedImageCube()
        {
            Int16[, ,] ImageVector = new Int16[Header.Bands, Header.Lines, Header.Samples];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int l = 0; l < Header.Lines; l++)
                {
                    for (int s = 0; s < Header.Samples; s++)
                    {
                        for (int b = 0; b < Header.Bands; b++)
                        {
                            ImageVector[b, l, s] = (Int16)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                        }

                    }
                }
            }
            return ImageVector;
        }
        public override double[, ,] LoadImageCube_withSubWindow(ImageSubWindow currentWindowToLoad)
        {
            double[, ,] ImagePixels = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                            ImagePixels[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
            return ImagePixels;
        }
        public override float[] LoadImageCube_withSubWindow_InSingleArray(ImageSubWindow currentWindowToLoad)
        {
            float[] ImagePixels = new float[(currentWindowToLoad.xEnd - currentWindowToLoad.xStart) * (currentWindowToLoad.yEnd - currentWindowToLoad.yStart) * Header.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                int valueCounter = 0;
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
                            // ImagePixels[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                            ImagePixels[valueCounter++] = (float)BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
            return ImagePixels;
        }
        public override double[, ,] LoadImageSingleBand(int bandIndex, ImageSubWindow currentWindowToLoad)
        {
            if (currentWindowToLoad == null)
            {
                currentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = Header.Samples, yStart = 0, yEnd = Header.Lines };
            }
            //..............................................................
            double[, ,] ImageVector = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                        ImageVector[s, l, 0] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
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
        //................................................................
        public override void WriteImageCube_withSubWindow(ImageSubWindow currentWindowToLoad, string outputFilePath)
        {
            //  double[, ,] ImagePixels = new double[currentWindowToLoad.xEnd - currentWindowToLoad.xStart, currentWindowToLoad.yEnd - currentWindowToLoad.yStart, Header.Bands];
            BinaryWriter imageWriter = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create));
            using (BinaryReader imageReader = new BinaryReader(new FileStream(PictureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                            // ImagePixels[s - currentWindowToLoad.xStart, l - currentWindowToLoad.yStart, b] = BinaryPixelConverter.AssignPixelValue(imageReader, Header.ByteOrder, PixelType);
                            BinaryPixelConverter.WritePixelValue(imageWriter, imageReader, Header.ByteOrder, PixelType);
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
            imageWriter.Close();
            WriteSubWindowHeader(currentWindowToLoad, outputFilePath);
            return;
        }
    }
    internal static class BinaryPixelConverter
    {
        #region Methods (3)

        // Public Methods (3) 
        public static void WritePixelValue(BinaryWriter imageWriter, BinaryReader imageReader, int byteOrder, Enumerations pixelType)
        {
            if (byteOrder != 1)
                WritePixelBandValue(imageWriter, imageReader, pixelType, byteOrder);
            else
                WritePixelBandValue_BigEndian(imageWriter, imageReader, pixelType, byteOrder);
        }

        private static void WritePixelBandValue_BigEndian(BinaryWriter imageWriter, BinaryReader imageReader, Enumerations pixelType, int byteOrder)
        {
            if (byteOrder != 1)
                WritePixelBandValue(imageWriter, imageReader, pixelType, byteOrder);
            else
                switch (pixelType)
                {
                    case Enumerations.uint8:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToSingle(new byte[] { imageReader.ReadByte() }, 0));
                        break;
                    case Enumerations.int16:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt16(imageReader.ReadBytes(2), 0));
                        break;
                    case Enumerations.int32:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt32(imageReader.ReadBytes(4), 0));
                        break;
                    case Enumerations.single:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToSingle(imageReader.ReadBytes(4), 0));
                        break;
                    case Enumerations.envidouble:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToDouble(imageReader.ReadBytes(8), 0));
                        break;
                    case Enumerations.uint32:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToUInt32(imageReader.ReadBytes(4), 0));
                        break;
                    case Enumerations.int64:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt64(imageReader.ReadBytes(8), 0));
                        break;
                    case Enumerations.uint16:
                        imageWriter.Write(MiscUtil.Conversion.BigEndianBitConverter.Big.ToUInt16(imageReader.ReadBytes(2), 0));
                        break;
                    default:
                        return;
                }
        }

        private static void WritePixelBandValue(BinaryWriter imageWriter, BinaryReader imageReader, Enumerations pixelType, int byteOrder)
        {
            if (byteOrder != 0)
                WritePixelBandValue_BigEndian(imageWriter, imageReader, pixelType, byteOrder);
            else

                switch (pixelType)
                {
                    case Enumerations.uint8:
                        imageWriter.Write(imageReader.ReadByte());
                        break;
                    case Enumerations.int16:
                        imageWriter.Write(imageReader.ReadInt16());
                        break;
                    case Enumerations.int32:
                        imageWriter.Write(imageReader.ReadInt32());
                        break;
                    case Enumerations.single:
                        imageWriter.Write(imageReader.ReadSingle());
                        break;
                    case Enumerations.envidouble:
                        imageWriter.Write(imageReader.ReadDouble());
                        break;
                    case Enumerations.uint32:
                        imageWriter.Write(imageReader.ReadUInt32());
                        break;
                    case Enumerations.int64:
                        imageWriter.Write(imageReader.ReadInt64());
                        break;
                    case Enumerations.uint16:
                        imageWriter.Write(imageReader.ReadUInt16());
                        break;
                    default:
                        return;
                }

        }

        public static double AssignPixelValue(BinaryReader imageReader, int byteOrder, Enumerations pixelType)
        {
            if (byteOrder != 1)
                return LoadPixelBandValue(imageReader, pixelType, byteOrder);
            else
                return LoadPixelBandValue_BigEndian(imageReader, pixelType, byteOrder);
        }

        public static double LoadPixelBandValue(BinaryReader imageReader, Enumerations pixelType, int byteOrder)
        {
            if (byteOrder != 0)
                return LoadPixelBandValue_BigEndian(imageReader, pixelType, byteOrder);
            else

                switch (pixelType)
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
        public static double LoadPixelBandValue_BigEndian(BinaryReader imageReader, Enumerations PixelType, int ByteOrder)
        {
            if (ByteOrder != 1)
                return LoadPixelBandValue(imageReader, PixelType, ByteOrder);
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

        #endregion Methods
    }
}
