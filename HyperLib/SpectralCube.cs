using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace HyperLib
{
    public class SpectralCube
    {
        protected HeaderInfo _headerInfo;
        protected int[] _maxValues;
        protected Enumerations _pixelType;
        public DataFormat currentFormateObj { get; private set; }

        public SpectralCube(string pictureFilePath)
        {
            if (File.Exists(pictureFilePath))
            {
                _PictureFilePath = pictureFilePath;
                Header = HeaderInfo.LoadFromFile(_PictureFilePath);
            }

        }

        [DefaultValue("")]
        protected string _PictureFilePath { get; set; }

        public string PictureFilePath { get { return _PictureFilePath; } }

        public HeaderInfo Header
        {
            get
            {
                if (_headerInfo == null)
                {
                    _headerInfo = HeaderInfo.LoadFromFile(_PictureFilePath);
                    UpdateHeaderDependencies();
                }
                return _headerInfo;
            }
            private set
            {
                _headerInfo = value;
                UpdateHeaderDependencies();
            }
        }

        private void UpdateHeaderDependencies()
        {
            if (_headerInfo != null)
            {
                PixelType = HeaderInfo.GetPixelDataType(_headerInfo);
                switch (Header.Interleave)
                {
                    case Interleave.BSQ:
                        currentFormateObj = new BSQDataFormat(_headerInfo, PixelType, PictureFilePath);
                        break;
                    case Interleave.BIL:
                        currentFormateObj = new BILDataFormat(_headerInfo, PixelType, PictureFilePath);
                        break;
                    case Interleave.BIP:
                        currentFormateObj = new BIPDataFormat(_headerInfo, PixelType, PictureFilePath);
                        break;
                    default:
                        break;
                }
            }
        }

        public Enumerations PixelType
        {
            get
            {
                if (Header == null)
                    Header = HeaderInfo.LoadFromFile(_PictureFilePath);
                return _pixelType;
            }
            private set
            {
                _pixelType = value;
            }
        }
        public double[, ,] ImageVector
        {
            get;
            protected set;
        }
        protected int[] MaxLoopValues()
        {
            int[] maxValues = new int[3];
            switch (Header.Interleave)
            {
                case Interleave.BSQ:
                    maxValues[0] = Header.Bands;
                    maxValues[1] = Header.Lines;
                    maxValues[2] = Header.Samples;
                    break;
                case Interleave.BIL:
                    maxValues[0] = Header.Lines;
                    maxValues[1] = Header.Bands;
                    maxValues[2] = Header.Samples;
                    break;
                case Interleave.BIP:
                    maxValues[0] = Header.Lines;
                    maxValues[1] = Header.Samples;
                    maxValues[2] = Header.Bands;
                    break;
                default:
                    break;
            }
            return maxValues;
        }
    }
}
