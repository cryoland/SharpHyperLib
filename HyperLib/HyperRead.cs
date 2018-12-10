using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using MiscUtil;
namespace HyperLib
{
    public static partial class HyperRead
    {

        /// <summary>
        ///  Reads the header file with ENVI FORMATE ( <fileName>.hdr)
        /// </summary>
        /// <param name="headerFilePath">The absolute path to the header file</param>
        /// <returns>an instance of the HeaderInfo data assigned in the headerFile</returns>
        public static HeaderInfo LoadHeaderFile(string pictureFilepath)
        {
            string headerFilePath = pictureFilepath.ToLower();
            if (Path.GetExtension(pictureFilepath) != "")
                headerFilePath = Path.ChangeExtension(pictureFilepath, ".hdr");
            else
                headerFilePath += ".hdr";

            HeaderInfo info = new HeaderInfo();
            if (!File.Exists(headerFilePath))
            {
                global::System.Windows.Forms.MessageBox.Show("Header File doesn't exists Please check the path");
                return null;
            }
            using (StreamReader inforeader = new StreamReader(headerFilePath))
            {
                while (!inforeader.EndOfStream)
                {
                    string line = inforeader.ReadLine();
                    string[] lineParts = line.Split('=');

                    if (lineParts.Length == 1)
                        continue;
                    switch (lineParts[0].Trim().ToLower())
                    {
                        case ("description"):
                            {
                                StringBuilder descr = new StringBuilder(lineParts[1]);
                                string descrline = inforeader.ReadLine();
                                while (!descrline.EndsWith("}"))
                                {
                                    descr.Append(descrline);
                                    descrline = inforeader.ReadLine();
                                }
                                descr.Append(descrline.TrimEnd('}'));
                                info.Description = descr.ToString();
                            }
                            break;
                        case ("map info"):
                            {
                                StringBuilder mapInfo = new StringBuilder(lineParts[1]);
                                if (!line.EndsWith("}"))
                                {
                                    string descrline = inforeader.ReadLine();
                                    while (!descrline.EndsWith("}"))
                                    {
                                        mapInfo.Append(descrline);
                                        descrline = inforeader.ReadLine();
                                    }
                                    mapInfo.Append(descrline.TrimEnd('}'));
                                }
                                mapInfo = new StringBuilder(line.Split(new char[] { '=' }, 2)[1]);
                                info.currentMapInfo = new MapInfo(mapInfo.ToString());
                            }
                            break;
                        case ("band names"):
                            {
                                string descrline = inforeader.ReadLine();
                                while (!descrline.EndsWith("}"))
                                {
                                    info.BandNames.Add(descrline.TrimEnd(','));
                                    descrline = inforeader.ReadLine();
                                }
                                info.BandNames.Add(descrline.TrimEnd('}'));
                            }
                            break;
                        case ("wavelength"):
                            {
                                string currentLine = inforeader.ReadLine();
                                string[] waveLengths = currentLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                while (!currentLine.EndsWith("}"))
                                {
                                    for (int i = 0; i < waveLengths.Length; i++)
                                    {
                                        info.WaveLength.Add(float.Parse(waveLengths[i]));
                                    }
                                    currentLine = inforeader.ReadLine();
                                    waveLengths = currentLine.TrimEnd('}').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                }
                                for (int i = 0; i < waveLengths.Length; i++)
                                {
                                    info.WaveLength.Add(float.Parse(waveLengths[i]));
                                }
                            }
                            break;
                        case ("fwhm"):
                            {
                                string currentLine = inforeader.ReadLine();
                                string[] waveLengths = currentLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                while (!currentLine.EndsWith("}"))
                                {
                                    for (int i = 0; i < waveLengths.Length; i++)
                                    {
                                        info.fwhm.Add(double.Parse(waveLengths[i]));
                                    }
                                    currentLine = inforeader.ReadLine();
                                    waveLengths = currentLine.TrimEnd('}').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                }
                                for (int i = 0; i < waveLengths.Length; i++)
                                {
                                    info.fwhm.Add(double.Parse(waveLengths[i]));
                                }
                            }
                            break;
                        case ("bbl"):
                            {
                                string currentLine = inforeader.ReadLine();
                                string[] waveLengths = currentLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                while (!currentLine.EndsWith("}"))
                                {
                                    for (int i = 0; i < waveLengths.Length; i++)
                                    {
                                        if (waveLengths[i].Trim() == "1")
                                            info.bbl.Add(true);
                                        else
                                            info.bbl.Add(false);
                                    }
                                    currentLine = inforeader.ReadLine();
                                    waveLengths = currentLine.TrimEnd('}').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                }
                                for (int i = 0; i < waveLengths.Length; i++)
                                {
                                    if (waveLengths[i].Trim() == "1")
                                        info.bbl.Add(true);
                                    else
                                        info.bbl.Add(false);
                                }
                            }
                            break;
                        case ("samples"):
                            info.Samples = int.Parse(lineParts[1]);
                            break;
                        case ("lines"):
                            info.Lines = int.Parse(lineParts[1]);
                            break;
                        case ("bands"):
                            info.Bands = int.Parse(lineParts[1]);
                            break;
                        case ("header offset"):
                            info.HeaderOffset = int.Parse(lineParts[1]);
                            break;
                        case ("file type"):
                            info.FileType = lineParts[1];
                            break;
                        case ("data type"):
                            info.DataType = /**/(DataType)/**/int.Parse(lineParts[1]);
                            break;
                        case ("interleave"):
                            info.FileType = lineParts[1];
                            break;
                        case ("sensor type"):
                            info.SensorType = lineParts[1];
                            break;
                        case ("byte order"):
                            info.ByteOrder = int.Parse(lineParts[1]);
                            break;
                        case ("x start"):
                            info.Xstart = int.Parse(lineParts[1]);
                            break;
                        case ("y start"):
                            info.Ystart = int.Parse(lineParts[1]);
                            break;
                        case ("wavelength units"):
                            info.WavelengthUnits = lineParts[1];
                            break;

                        default:
                            break;
                    }
                }
            }
            
            return info;
        }
        //.............................................................................
        private static DataType GetImageBinaryFormatFromHeaderInfo(HeaderInfo currentInfo)
        {
            double xi, yi, xm, ym;
            double[] x;
            double[] y;
            DataType currentReadingType = DataType.int32;
            #region Make geo-location vectors
            //.Make geo-location vectors................Not needed but already Implemented...............
            if (currentInfo.currentMapInfo != null)
            {
                if (currentInfo.currentMapInfo.MapX.HasValue && currentInfo.currentMapInfo.MapY.HasValue)
                {
                    xi = currentInfo.currentMapInfo.ImageCoordinats[0];
                    yi = currentInfo.currentMapInfo.ImageCoordinats[1];
                    xm = currentInfo.currentMapInfo.MapX.Value;
                    ym = currentInfo.currentMapInfo.MapY.Value;
                    //adjust points to corner (1.5,1.5)
                    if (yi > 1.5)
                        ym = ym + ((yi * currentInfo.currentMapInfo.DY) - currentInfo.currentMapInfo.DY);
                    if (xi > 1.5)
                        xm = xm - ((xi * currentInfo.currentMapInfo.DY) - currentInfo.currentMapInfo.DX);
                    //.............................................................
                    x = new double[currentInfo.Samples];
                    y = new double[currentInfo.Lines];

                    for (int i = 0; i < currentInfo.Samples; i++)
                    {
                        x[i] = xm + (i * currentInfo.currentMapInfo.DX);
                    }
                    for (int i = 0; i < currentInfo.Lines; i++)
                    {
                        y[i] = ym + (i * currentInfo.currentMapInfo.DY);
                    }

                }
            }
            //...................................................................
            #endregion
            #region Set binary format parameters

            switch (/**/(int)/**/currentInfo.DataType)
            {
                case (1):
                    currentReadingType = DataType.uint8;
                    break;
                case (2):
                    currentReadingType = DataType.int16;
                    break;
                case (3):
                    currentReadingType = DataType.int32;
                    break;
                case (4):
                    currentReadingType = DataType.single;
                    break;
                case (5):
                    currentReadingType = DataType.envidouble;
                    break;
                case (6):
                    {
                        MessageBox.Show(">> Sorry, Complex (2x32 bits)data currently not supported'");
                        MessageBox.Show(">> Importing as double-precision instead");
                        currentReadingType = DataType.envidouble;
                    }
                    break;
                case (9):
                    {
                        MessageBox.Show("Sorry, double-precision complex (2x64 bits) data currently not supported");
                        currentReadingType = DataType.envidouble;
                    }
                    break;
                case (12):
                    currentReadingType = DataType.uint16;
                    break;
                case (13):
                    currentReadingType = DataType.uint32;
                    break;
                case (14):
                case (15):
                    currentReadingType = DataType.int64;
                    break;
                default:
                    {
                        MessageBox.Show("File type number: :" + currentInfo.DataType + " not supported");
                        currentReadingType = DataType.envidouble;
                    }
                    break;
            }
            #endregion
            return currentReadingType;
        }
        //.............................................................................
        /// <summary>
        /// Assign the value of the pixel band to ImagePixelBandValue according to its dataType.
        /// </summary>
        /// <param name="currentReadingType"> the data type of the image defined in the headerInfo</param>
        /// <param name="imageReader">the binaryreader of the image</param>
        private static double LoadPixelBandValue_BigEndian(DataType currentReadingType, BinaryReader imageReader)
        {
            switch (currentReadingType)
            {
                case DataType.uint8:
                    return Convert.ToDouble(  MiscUtil.Conversion.BigEndianBitConverter.Big.ToSingle(new byte[] {imageReader.ReadByte()},0));
                    // Convert.ToDouble(imageReader.ReadByte());
                    break;
                case DataType.int16:

                    return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt16(imageReader.ReadBytes(2), 0));
                        // Convert.ToDouble(imageReader.ReadInt16());
                    break;
                case DataType.int32:
                    return Convert.ToDouble(  MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt32(imageReader.ReadBytes(4), 0));
                        //Convert.ToDouble(imageReader.ReadInt32());
                    break;
                case DataType.single:
                     return Convert.ToDouble( MiscUtil.Conversion.BigEndianBitConverter.Big.ToSingle(imageReader.ReadBytes(4), 0));
                        //Convert.ToDouble(imageReader.ReadSingle());
                    break;
                case DataType.envidouble:
                    return MiscUtil.Conversion.BigEndianBitConverter.Big.ToDouble(imageReader.ReadBytes(8), 0);   
                // return Convert.ToDouble(imageReader.ReadDouble());
                    break;
                case DataType.uint32:
                   return Convert.ToDouble( MiscUtil.Conversion.BigEndianBitConverter.Big.ToUInt32(imageReader.ReadBytes(4), 0));    
                //return Convert.ToDouble(imageReader.ReadUInt32());
                    break;
                case DataType.int64:
                    return Convert.ToDouble( MiscUtil.Conversion.BigEndianBitConverter.Big.ToInt64(imageReader.ReadBytes(8), 0));   
                // return Convert.ToDouble(imageReader.ReadInt64());
                    break;
                case DataType.uint16:
                    return Convert.ToDouble(MiscUtil.Conversion.BigEndianBitConverter.Big.ToUInt16(imageReader.ReadBytes(2), 0));   
                    break;
                default:
                    return 0;
                    break;
            }
        }
        private static double LoadPixelBandValue(DataType currentReadingType, BinaryReader imageReader)
        {
            switch (currentReadingType)
            {
                case DataType.uint8:
                    return Convert.ToDouble(imageReader.ReadByte());
                    break;
                case DataType.int16:

                    return Convert.ToDouble(imageReader.ReadInt16());
                    break;
                case DataType.int32:
                    return Convert.ToDouble(imageReader.ReadInt32());
                    break;
                case DataType.single:
                    return Convert.ToDouble(imageReader.ReadSingle());
                    break;
                case DataType.envidouble:
                    return Convert.ToDouble(imageReader.ReadDouble());
                    break;
                case DataType.uint32:
                    return Convert.ToDouble(imageReader.ReadUInt32());
                    break;
                case DataType.int64:
                    return Convert.ToDouble(imageReader.ReadInt64());
                    break;
                case DataType.uint16:
                    return Convert.ToDouble(imageReader.ReadUInt16());
                    break;
                default:
                    return 0;
                    break;
            }
        }
        //..................................................................................
        /// <summary>
        /// Loads the Whole Image as a 3-Dimensional Array [X,Y,Z]
        /// X: is the number od Samples (width)
        /// Y: is the number of Lines (height)
        /// Z: is the number of Bands (Z-axis)
        /// </summary>
        /// <param name="pictureFilepath">the absolute path of the (.dat) file</param>
        /// <returns>3-Dimensional Array [X,Y,Z] containing the image data cube</returns>
        public static double[, ,] LoadImageInMemory(string pictureFilepath)
        {
            HeaderInfo info = LoadHeaderFile(pictureFilepath);
            DataType currentReadingType = GetImageBinaryFormatFromHeaderInfo(info);
            double[, ,] ImagePixels = new double[info.Samples, info.Lines, info.Bands];
            //............................................................
            int[] maxValues = MaxLoopValues(info);
            //..................................................................
            using (BinaryReader imageReader = new BinaryReader(new FileStream(pictureFilepath, FileMode.Open)))
            {
                if (info.FileType.ToLower().Contains("bsq"))
                {
                    for (int b = 0; b < maxValues[0]; b++)
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
                else if (info.FileType.ToLower().Contains("bil"))
                {
                    for (int l = 0; l < info.Lines; l++)
                    {
                        for (int b = 0; b < info.Bands; b++)
                        {
                            for (int s = 0; s < info.Samples; s++)
                            {
                                if (info.ByteOrder != 1)
                                    ImagePixels[s, l, b] = LoadPixelBandValue(currentReadingType, imageReader);
                                else
                                    ImagePixels[s, l, b] = LoadPixelBandValue_BigEndian(currentReadingType, imageReader);
                            }

                        }
                    }
                }
                else if (info.FileType.ToLower().Contains("bip"))
                {
                    for (int l = 0; l < info.Lines; l++)
                    {
                        for (int s = 0; s < info.Samples; s++)
                        {
                            for (int b = 0; b < info.Bands; b++)
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
            return ImagePixels;
        }

        private static int[] MaxLoopValues(HeaderInfo info)
        {
            int[] maxValues = new int[3];
            if (info.FileType.ToLower().Contains("bsq"))
            {
                maxValues[0] = info.Bands;
                maxValues[1] = info.Lines;
                maxValues[2] = info.Samples;
            }
            else if (info.FileType.ToLower().Contains("bil"))
            {
                maxValues[0] = info.Lines;
                maxValues[1] = info.Bands;
                maxValues[2] = info.Samples;
            }
            else if (info.FileType.ToLower().Contains("bip"))
            {
                maxValues[0] = info.Lines;
                maxValues[1] = info.Samples;
                maxValues[2] = info.Bands;
            }
            return maxValues;
        }
        /// <summary>
        /// Loads a sub Image window  as a 3-Dimensional Array [X,Y,Z]
        /// X: is the number od Samples (width)
        /// Y: is the number of Lines (height)
        /// Z: is the number of Bands (Z-axis)
        /// </summary>
        /// <param name="pictureFilepath">the absolute path of the (.dat) file</param>
        /// <param name="CurrentWindowToLoad">a window object identifing the part of the image cube required</param>
        /// <returns>3-Dimensional Array [X,Y,Z] containing the image window data cube</returns>
        public static double[, ,] LoadImageInMemory(string pictureFilepath,ImageSubWindow CurrentWindowToLoad)
        {

            HeaderInfo info = LoadHeaderFile(pictureFilepath);
            DataType currentReadingType = GetImageBinaryFormatFromHeaderInfo(info);
            //..............................................................
            GC.Collect();

            double[, ,] ImagePixels = new double[CurrentWindowToLoad.xEnd - CurrentWindowToLoad.xStart, CurrentWindowToLoad.yEnd - CurrentWindowToLoad.yStart, info.Bands];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(pictureFilepath, FileMode.Open)))
            {
                // imageReader.BaseStream.Position= info.Samples
                for (int b = 0; b < info.Bands; b++)
                {
                    long currentBandStartPosition = imageReader.BaseStream.Position;
                    #region Set Seek Position -- CurrentWindowToLoad.yStart * info.Samples * sizeof(byte)
                    switch (currentReadingType)
                    {
                        case DataType.uint8:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(byte);
                            break;
                        case DataType.int16:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Int16);
                            break;
                        case DataType.int32:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Int32);
                            break;
                        case DataType.single:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Single);
                            break;
                        case DataType.envidouble:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(double);
                            break;
                        case DataType.uint32:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(UInt32);
                            break;
                        case DataType.int64:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Int64);
                            break;
                        case DataType.uint16:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(UInt16);
                            break;
                        default:
                            break;
                    }


                    #endregion
                    //.................................................................
                    #region Read Pixel Lines
                    for (int l = CurrentWindowToLoad.yStart; l < info.Lines && l < CurrentWindowToLoad.yEnd; l++)
                    {
                        #region Set Seek Position -- CurrentWindowToLoad.xStart * sizeof(byte)
                        switch (currentReadingType)
                        {
                            case DataType.uint8:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(byte);
                                break;
                            case DataType.int16:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Int16);
                                break;
                            case DataType.int32:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Int32);
                                break;
                            case DataType.single:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Single);
                                break;
                            case DataType.envidouble:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(double);
                                break;
                            case DataType.uint32:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(UInt32);
                                break;
                            case DataType.int64:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Int64);
                                break;
                            case DataType.uint16:
                                imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(UInt16);
                                break;
                            default:
                                break;
                        }


                        #endregion
                        for (int s = CurrentWindowToLoad.xStart; s < info.Samples && s < CurrentWindowToLoad.xEnd; s++)
                        {
                            ImagePixels[s - CurrentWindowToLoad.xStart, l - CurrentWindowToLoad.yStart, b] = LoadPixelBandValue(currentReadingType, imageReader);
                        }
                        #region Set Seek Position -- (CurrentWindowToLoad.xEnd - info.Samples) * sizeof(byte)
                        if (CurrentWindowToLoad.xEnd < info.Samples)
                        {
                            int remainderSamples = info.Samples - CurrentWindowToLoad.xEnd;
                            switch (currentReadingType)
                            {
                                case DataType.uint8:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(byte);
                                    break;
                                case DataType.int16:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(Int16);
                                    break;
                                case DataType.int32:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(Int32);
                                    break;
                                case DataType.single:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(Single);
                                    break;
                                case DataType.envidouble:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(double);
                                    break;
                                case DataType.uint32:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(UInt32);
                                    break;
                                case DataType.int64:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(Int64);
                                    break;
                                case DataType.uint16:
                                    imageReader.BaseStream.Position += remainderSamples * sizeof(UInt16);
                                    break;
                                default:
                                    break;
                            }

                        }
                        #endregion
                    }
                    #endregion
                    //.................................................................
                    #region Set Seek Position -- (CurrentWindowToLoad.yEnd - info.Lines) * info.Samples * sizeof(byte)
                    if (CurrentWindowToLoad.yEnd < info.Lines)
                    {
                        int remainderlines = info.Lines - CurrentWindowToLoad.yEnd;
                        switch (currentReadingType)
                        {
                            case DataType.uint8:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(byte);
                                break;
                            case DataType.int16:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Int16);
                                break;
                            case DataType.int32:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Int32);
                                break;
                            case DataType.single:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Single);
                                break;
                            case DataType.envidouble:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(double);
                                break;
                            case DataType.uint32:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(UInt32);
                                break;
                            case DataType.int64:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Int64);
                                break;
                            case DataType.uint16:
                                imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(UInt16);
                                break;
                            default:
                                break;
                        }
                    }

                    #endregion
                }
            }
            return ImagePixels;
        }
        /// <summary>
        /// Loads the Whole Image as a 3-Dimensional Array [X,Y,Z], inwhich the Z axis is only one value
        /// X: is the number od Samples (width)
        /// Y: is the Number of Lines (height)
        /// Z: is the number of Bands (Z-axis) ONLY ONE
        /// </summary>
        /// <param name="pictureFilepath">the absolute path of the (.dat) file</param>
        /// <param name="bandIndex"> the index of the band required</param>
        /// <returns>3-Dimensional Array [X,Y,Z] containing the whole image data cube,inwhich the Z axis is only one value</returns>
        public static double[, ,] LoadImageBandInMemory(string pictureFilepath,int bandIndex)
        {
            return LoadImageBandInMemory(pictureFilepath, null, bandIndex);
        }
        /// <summary>
        /// Loads the Whole Image as a 3-Dimensional Array [X,Y,Z], inwhich the Z axis is only one value
        /// X: is the number od Samples (width)
        /// Y: is the Number of Lines (height)
        /// Z: is the number of Bands (Z-axis) ONLY ONE
        /// </summary>
        /// <param name="pictureFilepath">the absolute path of the (.dat) file</param>
        /// /// <param name="CurrentWindowToLoad">a window object identifing the part of the image cube required</param>
        /// <param name="bandIndex"> the index of the band required</param>
        /// <returns>3-Dimensional Array [X,Y,Z] containing the image window data cube,inwhich the Z axis is only one value</returns>
        public static double[, ,] LoadImageBandInMemory(string pictureFilepath,ImageSubWindow CurrentWindowToLoad, int bandIndex)
        {

            HeaderInfo info = LoadHeaderFile(pictureFilepath);
            DataType currentReadingType = GetImageBinaryFormatFromHeaderInfo(info);
            if (CurrentWindowToLoad == null)
            {
                CurrentWindowToLoad = new ImageSubWindow() { xStart = 0, xEnd = info.Samples, yStart = 0, yEnd = info.Lines };
            }
            //..............................................................
            double[, ,] ImagePixels = new double[CurrentWindowToLoad.xEnd - CurrentWindowToLoad.xStart, CurrentWindowToLoad.yEnd - CurrentWindowToLoad.yStart, 1];
            using (BinaryReader imageReader = new BinaryReader(new FileStream(pictureFilepath, FileMode.Open)))
            {
                #region Set Seek BAND Position -- CurrentWindowToLoad.yStart * info.Samples * sizeof(byte)
                switch (currentReadingType)
                {
                    case DataType.uint8:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(byte);
                        break;
                    case DataType.int16:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(Int16);
                        break;
                    case DataType.int32:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(Int32);
                        break;
                    case DataType.single:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(Single);
                        break;
                    case DataType.envidouble:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(double);
                        break;
                    case DataType.uint32:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(UInt32);
                        break;
                    case DataType.int64:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(Int64);
                        break;
                    case DataType.uint16:
                        imageReader.BaseStream.Position += bandIndex * info.Lines * info.Samples * sizeof(UInt16);
                        break;
                    default:
                        break;
                }


                #endregion
                int b = 0;
                long currentBandStartPosition = imageReader.BaseStream.Position;
                #region Set Seek Position -- CurrentWindowToLoad.yStart * info.Samples * sizeof(byte)
                switch (currentReadingType)
                {
                    case DataType.uint8:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(byte);
                        break;
                    case DataType.int16:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Int16);
                        break;
                    case DataType.int32:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Int32);
                        break;
                    case DataType.single:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Single);
                        break;
                    case DataType.envidouble:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(double);
                        break;
                    case DataType.uint32:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(UInt32);
                        break;
                    case DataType.int64:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(Int64);
                        break;
                    case DataType.uint16:
                        imageReader.BaseStream.Position += CurrentWindowToLoad.yStart * info.Samples * sizeof(UInt16);
                        break;
                    default:
                        break;
                }


                #endregion
                //.................................................................
                #region Read Pixel Lines
                for (int l = CurrentWindowToLoad.yStart; l < info.Lines && l < CurrentWindowToLoad.yEnd; l++)
                {
                    #region Set Seek Position -- CurrentWindowToLoad.xStart * sizeof(byte)
                    switch (currentReadingType)
                    {
                        case DataType.uint8:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(byte);
                            break;
                        case DataType.int16:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Int16);
                            break;
                        case DataType.int32:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Int32);
                            break;
                        case DataType.single:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Single);
                            break;
                        case DataType.envidouble:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(double);
                            break;
                        case DataType.uint32:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(UInt32);
                            break;
                        case DataType.int64:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(Int64);
                            break;
                        case DataType.uint16:
                            imageReader.BaseStream.Position += CurrentWindowToLoad.xStart * sizeof(UInt16);
                            break;
                        default:
                            break;
                    }


                    #endregion
                    for (int s = CurrentWindowToLoad.xStart; s < info.Samples && s < CurrentWindowToLoad.xEnd; s++)
                    {
                        ImagePixels[s, l, b] = LoadPixelBandValue(currentReadingType, imageReader);
                    }
                    #region Set Seek Position -- (CurrentWindowToLoad.xEnd - info.Samples) * sizeof(byte)
                    if (CurrentWindowToLoad.xEnd < info.Samples)
                    {
                        int remainderSamples = info.Samples - CurrentWindowToLoad.xEnd;
                        switch (currentReadingType)
                        {
                            case DataType.uint8:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(byte);
                                break;
                            case DataType.int16:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(Int16);
                                break;
                            case DataType.int32:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(Int32);
                                break;
                            case DataType.single:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(Single);
                                break;
                            case DataType.envidouble:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(double);
                                break;
                            case DataType.uint32:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(UInt32);
                                break;
                            case DataType.int64:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(Int64);
                                break;
                            case DataType.uint16:
                                imageReader.BaseStream.Position += remainderSamples * sizeof(UInt16);
                                break;
                            default:
                                break;
                        }

                    }
                    #endregion
                }
                #endregion
                //.................................................................
                #region Set Seek Position -- (CurrentWindowToLoad.yEnd - info.Lines) * info.Samples * sizeof(byte)
                if (CurrentWindowToLoad.yEnd < info.Lines)
                {
                    int remainderlines = info.Lines - CurrentWindowToLoad.yEnd;
                    switch (currentReadingType)
                    {
                        case DataType.uint8:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(byte);
                            break;
                        case DataType.int16:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Int16);
                            break;
                        case DataType.int32:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Int32);
                            break;
                        case DataType.single:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Single);
                            break;
                        case DataType.envidouble:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(double);
                            break;
                        case DataType.uint32:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(UInt32);
                            break;
                        case DataType.int64:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(Int64);
                            break;
                        case DataType.uint16:
                            imageReader.BaseStream.Position += remainderlines * info.Samples * sizeof(UInt16);
                            break;
                        default:
                            break;
                    }
                }

                #endregion
            }
            return ImagePixels;
        }
        //........................................................................................
       
    }
}
