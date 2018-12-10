using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace HyperLib
{
    public class HeaderInfo
    {

        public HeaderInfo()
        {
            WaveLength = new List<float>();
            BandNames = new List<string>();
            fwhm = new List<double>();
            bbl = new List<Boolean>();
        }



        public List<string> BandNames { get; set; }

        public int Bands { get; set; }

        public List<Boolean> bbl { get; set; }

        public int ByteOrder { get; set; }

        public MapInfo currentMapInfo { get; set; }

        public int PixelType { get; set; }

        public string Description { get; set; }

        public string FileType { get; set; }

        public List<double> fwhm { get; set; }

        public int HeaderOffset { get; set; }

        public string HeaderFilePath { get; protected set; }

        public Interleave Interleave { get; set; }
               
        /**/public DataType DataType { get; set; }/**/

        public int Lines { get; set; }

        public int Samples { get; set; }

        public string SensorType { get; set; }

        public List<float> WaveLength { get; set; }

        public string WavelengthUnits { get; set; }

        public int Xstart { get; set; }

        public int Ystart { get; set; }
        /// <summary>
        ///  Reads the header file with ENVI FORMATE ( <fileName>.hdr)
        /// </summary>
        /// <param name="headerFilePath">The absolute path to the image file</param>
        /// <returns>an instance of the HeaderInfo data assigned in the headerFile</returns>
        public static HeaderInfo LoadFromFile(string pictureFilepath)
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
                            info.PixelType = int.Parse(lineParts[1]);
                            break;
                        case ("interleave"):
                            info.Interleave = (Interleave)Enum.Parse(typeof(Interleave), lineParts[1].Trim().ToUpper());
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentInfo"></param>
        /// <returns></returns>
        internal static Enumerations GetPixelDataType(HeaderInfo currentInfo)
        {
            double xi, yi, xm, ym;
            double[] x;
            double[] y;
            HyperLib.Enumerations currentReadingType = HyperLib.Enumerations.int32;
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

            switch (currentInfo.PixelType)
            {
                case (1):
                    currentReadingType = Enumerations.uint8;
                    break;
                case (2):
                    currentReadingType = Enumerations.int16;
                    break;
                case (3):
                    currentReadingType = Enumerations.int32;
                    break;
                case (4):
                    currentReadingType = Enumerations.single;
                    break;
                case (5):
                    currentReadingType = Enumerations.envidouble;
                    break;
                case (6):
                    {
                        MessageBox.Show(">> Sorry, Complex (2x32 bits)data currently not supported'");
                        MessageBox.Show(">> Importing as double-precision instead");
                        currentReadingType = Enumerations.envidouble;
                    }
                    break;
                case (9):
                    {
                        MessageBox.Show("Sorry, double-precision complex (2x64 bits) data currently not supported");
                        currentReadingType = Enumerations.envidouble;
                    }
                    break;
                case (12):
                    currentReadingType = Enumerations.uint16;
                    break;
                case (13):
                    currentReadingType = Enumerations.uint32;
                    break;
                case (14):
                case (15):
                    currentReadingType = Enumerations.int64;
                    break;
                default:
                    {
                        MessageBox.Show("File type number: :" + currentInfo.PixelType + " not supported");
                        currentReadingType = Enumerations.envidouble;
                    }
                    break;
            }
            #endregion

            return currentReadingType;
        }
        public static string GenerateHeaderFilePathFromImage(string ImageFilePath)
        {
            string headerFilePath = ImageFilePath;//.ToLower();
            if (Path.GetExtension(ImageFilePath) != "")
                headerFilePath = Path.ChangeExtension(ImageFilePath, ".hdr");
            else
                headerFilePath += ".hdr";
            return headerFilePath;
        }

    }
}

