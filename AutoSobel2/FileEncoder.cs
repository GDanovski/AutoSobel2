using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellToolDK;

namespace MultiCrop
{
    class FileEncoder
    {
        public static void SaveTif(TifFileInfo fi, string dir)
        {
            //check is the directory exist
            if (dir.IndexOf("\\") > -1)
            {
                string checkDir = dir.Substring(0, dir.LastIndexOf("\\"));
                if (!System.IO.Directory.Exists(checkDir)) System.IO.Directory.CreateDirectory(checkDir);
            }

            try {
                //return;
                //Save pixel data
                switch (fi.bitsPerPixel)
                {
                    case 8:
                        SaveTif_8bitRawData(fi, dir);
                        break;
                    case 16:
                        SaveTif_16bitRawData(fi, dir);
                        break;
                    default:
                        return;
                }
                //save metadata
                string value = calculateCTTagValue(fi);
                
                AddTag(dir, value);
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Save file error!");
            }
        }
        #region Write Directory
        private static void SaveTif_8bitRawData(TifFileInfo fi, string fileName)
        {
            int numberOfPages = fi.imageCount;

            int width = fi.sizeX;
            int height = fi.sizeY;
            int samplesPerPixel = 1;
            int bitsPerSample = fi.bitsPerPixel;

            using (Tiff output = Tiff.Open(fileName, "w"))
            {
                for (int page = 0; page < numberOfPages; page++)
                {
                    byte[][] firstPageBuffer = fi.image8bit[page];
                    output.SetField(TiffTag.IMAGELENGTH, height);
                    output.SetField(TiffTag.IMAGEWIDTH, width);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                    output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0));
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.IMAGEDESCRIPTION, ImageDetails());
                    // specify that it's a page within the multipage file
                    output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
                    // specify the page number
                    output.SetField(TiffTag.PAGENUMBER, page, numberOfPages);

                    for (int j = 0; j < height; ++j)
                        output.WriteScanline(firstPageBuffer[j], j);

                    output.WriteDirectory();
                }
            }
        }
        private static void SaveTif_16bitRawData(TifFileInfo fi, string fileName)
        {
            int numberOfPages = fi.imageCount;

            int width = fi.sizeX;
            int height = fi.sizeY;
            int samplesPerPixel = 1;
            int bitsPerSample = fi.bitsPerPixel;

            using (Tiff output = Tiff.Open(fileName, "w"))
            {
                for (int page = 0; page < numberOfPages; page++)
                {
                    ushort[][] image = fi.image16bit[page];
                    output.SetField(TiffTag.IMAGELENGTH, height);
                    output.SetField(TiffTag.IMAGEWIDTH, width);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                    output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0));
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.IMAGEDESCRIPTION, ImageDetails());
                    // specify that it's a page within the multipage file
                    output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
                    // specify the page number
                    output.SetField(TiffTag.PAGENUMBER, page, numberOfPages);

                    for (int i = 0; i < height; i++)
                    {
                        ushort[] samples = image[i];

                        byte[] buffer = new byte[samples.Length * sizeof(ushort)];
                        Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);

                        output.WriteScanline(buffer, i);
                    }

                    output.WriteDirectory();
                }
            }
        }
        private static string ImageDetails()
        {
            string val = "CellTool tif file\nProgram name: CellTool\nProgram version: 1.0.0.0\n";
            return val;
        }
        #endregion Write Directory

        #region Custom Tag
        private const TiffTag TIFFTAG_CellTool_METADATA = (TiffTag)40005;

        private static Tiff.TiffExtendProc m_parentExtender;

        private static void TagExtender(Tiff tif)
        {
            TiffFieldInfo[] tiffFieldInfo =
            {
                new TiffFieldInfo(TIFFTAG_CellTool_METADATA, -1, -1, TiffType.ASCII,
                    FieldBit.Custom, true, false, "CellTool_Metadata"),
            };
            
            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);

            if (m_parentExtender != null)
                m_parentExtender(tif);
        }
        private static string calculateCTTagValue(TifFileInfo fi)
        {

            List<string> vals = new List<string>();
            try
            {
                vals.Add("seriesCount->" + fi.seriesCount.ToString());
                vals.Add("imageCount->" + fi.imageCount.ToString());
                vals.Add("sizeX->" + fi.sizeX.ToString());
                vals.Add("sizeY->" + fi.sizeY.ToString());
                vals.Add("sizeC->" + fi.sizeC.ToString());
                vals.Add("sizeZ->" + fi.sizeZ.ToString());
                vals.Add("sizeT->" + fi.sizeT.ToString());
                vals.Add("umXY->" + fi.umXY.ToString());
                vals.Add("umZ->" + fi.umZ.ToString());
                vals.Add("bitsPerPixel->" + fi.bitsPerPixel.ToString());
                vals.Add("dimensionOrder->" + fi.dimensionOrder);
                vals.Add("pixelType->" + fi.pixelType.ToString());
                vals.Add("FalseColored->" + fi.FalseColored.ToString());
                vals.Add("isIndexed->" + fi.isIndexed.ToString());
                vals.Add("MetadataComplete->" + fi.MetadataComplete.ToString());
                vals.Add("DatasetStructureDescription->" + fi.DatasetStructureDescription);
                vals.Add("Micropoint->" + fi.Micropoint.ToString());
                vals.Add("autoDetectBandC->" + fi.autoDetectBandC.ToString());
                vals.Add("applyToAllBandC->" + fi.applyToAllBandC.ToString());
                vals.Add("xCompensation->" + fi.xCompensation.ToString());
                vals.Add("yCompensation->" + fi.yCompensation.ToString());
                vals.Add("DataSourceInd->" + fi.DataSourceInd.ToString());
                vals.Add("LutList->" + TagValueToString(fi.LutList));
                vals.Add("TimeSteps->" + TagValueToString(fi.TimeSteps));

                if (fi.MinBrightness != null)
                    vals.Add("MinBrightness->" + TagValueToString(fi.MinBrightness));
                else
                {
                    int[] a = new int[fi.sizeC];
                    vals.Add("MinBrightness->" + TagValueToString(a));
                }

                if (fi.MaxBrightness != null)
                    vals.Add("MaxBrightness->" + TagValueToString(fi.MaxBrightness));
                else
                {
                    int[] a = new int[fi.sizeC];
                    int c1 = 250;
                    if (fi.bitsPerPixel != 8) c1 = 16000;
                    for (int i = 0; i < a.Length; i++)
                        a[i] = c1;
                    vals.Add("MaxBrightness->" + TagValueToString(a));
                }
                
                vals.Add("tracking_MaxSize->" + TagValueToString(fi.tracking_MaxSize));
                vals.Add("tracking_MinSize->" + TagValueToString(fi.tracking_MinSize));
                vals.Add("tracking_Speed->" + TagValueToString(fi.tracking_Speed));
                vals.Add("SegmentationProtocol->" + TagValueToString(fi.SegmentationProtocol));
                vals.Add("SegmentationCBoxIndex->" + TagValueToString(fi.SegmentationCBoxIndex));
                vals.Add("thresholdsCBoxIndex->" + TagValueToString(fi.thresholdsCBoxIndex));
                vals.Add("SelectedSpotThresh->" + TagValueToString(fi.SelectedSpotThresh));
                vals.Add("typeSpotThresh->" + TagValueToString(fi.typeSpotThresh));
                vals.Add("SpotThresh->" + TagValueToString(fi.SpotThresh));
                vals.Add("spotSensitivity->" + TagValueToString(fi.spotSensitivity));
                vals.Add("thresholds->" + TagValueToString(fi.thresholds));
                vals.Add("SpotColor->" + TagValueToString(fi.SpotColor));
                vals.Add("RefSpotColor->" + TagValueToString(fi.RefSpotColor));
                vals.Add("sumHistogramChecked->" + TagValueToString(fi.sumHistogramChecked));
                vals.Add("SpotTailType->" + string.Join("\t", fi.SpotTailType));
                vals.Add("thresholdColors->" + TagValueToString(fi.thresholdColors));
                vals.Add("RefThresholdColors->" + TagValueToString(fi.RefThresholdColors));
                vals.Add("thresholdValues->" + TagValueToString(fi.thresholdValues));
                vals.Add("FileDescription->" + fi.FileDescription);
                vals.Add("xAxisTB->" + fi.xAxisTB.ToString());
                vals.Add("yAxisTB->" + fi.yAxisTB.ToString());

                //Roi part
                int c = 0;
                foreach (List<ROI> roiList in fi.roiList)
                {
                    if (roiList != null)
                        foreach (ROI roi in roiList)
                            vals.Add("roi.new->" + roi_new(c, roi));
                    c++;
                }
                //it is important FilterHistory to be the last
                vals.Add("FilterHistory->" + TagValueToString(fi.FilterHistory.ToArray()));
            }
            catch { System.Windows.Forms.MessageBox.Show(vals[vals.Count - 1]); }
            return string.Join(";\n", vals);
        }
        private static string roi_new(int C, ROI roi)
        {
            return "roi.new(" + C.ToString() + "," +
                roi.getID.ToString() + "," + roi.roi_getAllInfo() + ")";
        }
        private static string TagValueToString(int[][] intList)
        {
            string val = "";
            foreach (int[] line in intList)
            {
                foreach (int i in line)
                    val += i.ToString() + "\t";
                val += "\n";
            }
            return val;
        }
        private static string TagValueToString(List<double> dList)
        {
            string val = "";
            foreach (double d in dList)
                val += d.ToString() + "\t";
            return val;
        }
        private static string TagValueToString(int[] intArr)
        {
            string val = "";
            foreach (int i in intArr)
                val += i.ToString() + "\t";
            return val;
        }
        private static string TagValueToString(bool[] bArr)
        {
            string val = "";
            foreach (bool b in bArr)
                val += b.ToString() + "\t";
            return val;
        }
        private static string TagValueToString(Color[][] cBigList)
        {
            string val = "";

            foreach (Color[] cList in cBigList)
            {
                foreach (Color c in cList)
                    val += ColorTranslator.ToHtml(c) + "\t";
                val += "\n";
            }

            return val;
        }
        private static string TagValueToString(Color[] cList)
        {
            string val = "";
            foreach (Color c in cList)
                val += ColorTranslator.ToHtml(c) + "\t";
            return val;
        }
        private static string TagValueToString(List<Color> cList)
        {
            string val = "";
            foreach (Color c in cList)
                val += ColorTranslator.ToHtml(c) + "\t";
            return val;
        }
        private static void AddTag(string dir, string value)
        {
            // Register the extender callback 
            m_parentExtender = Tiff.SetTagExtender(TagExtender);
            
            using (Tiff image = Tiff.Open(dir, "a"))
            {
                image.SetDirectory((short)(image.NumberOfDirectories() - 1));
                // we should rewind to first directory (first image) because of append mode
                
                // set the custom tag  
                image.SetField(TIFFTAG_CellTool_METADATA, value);

                // rewrites directory saving new tag
                image.CheckpointDirectory();
            }

            // restore previous tag extender
            Tiff.SetTagExtender(m_parentExtender);
        }
        #endregion Custom Tag
    }
}
