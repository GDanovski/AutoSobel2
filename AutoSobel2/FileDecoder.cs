/*
 CellTool - software for bio-image analysis
 Copyright (C) 2018  Georgi Danovski

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
//LibTif
using BitMiracle.LibTiff.Classic;
using CellToolDK;


namespace Cell_Tool_3
{
    class FileDecoder
    {
        private static void Image8bit_readFrame(int i, Tiff image, TifFileInfo fi, int[] dimOrder = null)
        {
            
                if (fi.image8bit == null) { return; }

                if (dimOrder != null)
                    image.SetDirectory((short)dimOrder[i]);
                else
                    image.SetDirectory((short)i);

                int scanlineSize = image.ScanlineSize();

                byte[][] buffer8 = new byte[fi.sizeY][];

                for (int j = 0; j < fi.sizeY; j++)
                {
                    buffer8[j] = new byte[scanlineSize];
                    image.ReadScanline(buffer8[j], j);
                }
                if (fi.image8bit == null) { return; }
                try
                {
                    fi.image8bit[i] = buffer8;
                }
                catch
                {

                }
            
        }
        private static void Image16bit_readFrame(int i, Tiff image, TifFileInfo fi, int[] dimOrder = null)
        {
                if (fi.image16bit == null) { return; }

                if (dimOrder != null)
                    image.SetDirectory((short)dimOrder[i]);
                else
                    image.SetDirectory((short)i);

                int scanlineSize = image.ScanlineSize();

                ushort[][] buffer16 = new ushort[fi.sizeY][];

                for (int j = 0; j < fi.sizeY; j++)
                {
                    byte[] line = new byte[scanlineSize];
                    buffer16[j] = new ushort[scanlineSize / 2];
                    image.ReadScanline(line, j);
                    Buffer.BlockCopy(line, 0, buffer16[j], 0, line.Length);
                }
                if (fi.image16bit == null) { return; }
                try
                {
                    fi.image16bit[i] = buffer16;
                }
                catch
                {
                }
            
        }
        private static void ImageReader_BGW( Tiff image1,TifFileInfo fi, string path, int[] dimOrder = null)
        {
            
            //Tiff.ByteArrayToShorts
            int height = fi.sizeY;
            int BitsPerPixel = fi.bitsPerPixel;
            int midFrame = fi.sizeC * fi.sizeZ;
            switch (BitsPerPixel)
            {
                    case 8:
                    Parallel.For(midFrame, (int)image1.NumberOfDirectories(), i =>
                    {
                        using (Tiff image = Tiff.Open(path, "r"))
                        {
                            Image8bit_readFrame(i, image, fi, dimOrder);
                            image.Close();
                        }
                    });
                    break;
                    case 16:
                    Parallel.For(midFrame, (int)image1.NumberOfDirectories(), i =>
                    {
                        
                        using (Tiff image = Tiff.Open(path, "r"))
                        {
                            Image16bit_readFrame(i, image, fi, dimOrder);
                            image.Close();
                        }
                    });
                    break;
                }
        }

        #region Read CellTool3 metadata
        
        public static TifFileInfo ReadImage(string path)
        {
            string[] vals = null;
            //Check for file

            Tiff image = Tiff.Open(path, "r");
            {
                try
                {
                    image.SetDirectory((short)(image.NumberOfDirectories() - 1));
                }
                catch
                {
                    return null;
                }
                
                //image.SetDirectory((short)(image.NumberOfDirectories() - 1));
                // read auto-registered tag 50341
                FieldValue[] value = image.GetField((TiffTag)40005);//CellTool3 tif tag
                if(value != null)
                {
                    //File.WriteAllText("D:\\Work\\Metadata\\CTMeta.txt", value[1].ToString());
                    vals = value[1].ToString().Split(new string[] { ";\n" }, StringSplitOptions.None);
                }
                else
                {
                    image.Close();
                    return null;
                }
                image.SetDirectory(0);
            }
            //reading part
            TifFileInfo fi = new TifFileInfo();
            fi.Dir = path;
            fi.original = false;
            //fi.available = false;
            //read tags
            int[] FilterHistory = ApplyCT3Tags(vals, fi);
            
            vals = null;
           
            bool loaded = false;

            fi.available = false;
            //Add handlers to the backgroundworker
            {
                //prepare array and read file
                int midFrame = fi.sizeC * fi.sizeZ;
                switch (fi.bitsPerPixel)
                {
                    case 8:
                        fi.image8bit = new byte[fi.imageCount/*image.NumberOfDirectories()*/][][];
                        for (int i = 0; i < midFrame; i++)
                        {
                            if (i >= fi.imageCount) { break; }
                            Image8bit_readFrame(i, image, fi);
                        }
                        break;
                    case 16:
                        fi.image16bit = new ushort[fi.imageCount/*image.NumberOfDirectories()*/][][];
                        for (int i = 0; i < midFrame; i++)
                        {
                            if (i >= fi.imageCount) { break; }
                            Image16bit_readFrame(i, image, fi);
                        }
                        break;
                }
                loaded = true;
                //parallel readers
                ImageReader_BGW(image, fi,  path);

                switch (fi.bitsPerPixel)
                {
                    case 8:
                        fi.image8bitFilter = fi.image8bit;
                        break;
                    case 16:
                        fi.image16bitFilter = fi.image16bit;
                        break;
                }

                image.Close();
            }

            {

                fi.openedImages = fi.imageCount;
                fi.available = true;
                fi.loaded = true;

                //CalculateAllRois(fi);
            }
            //out put
            return fi;
        }
        
        private static int[] ApplyCT3Tags(string[] BigVals, TifFileInfo fi)
        {
            int[] FilterHistory = null;
            string[] vals = null;

            foreach (string val in BigVals)
            {
                try
                {
                    vals = val.Split(new string[] { "->" }, StringSplitOptions.None);
                    switch (vals[0])
                    {
                        case ("seriesCount"):
                            fi.seriesCount = StringToTagValue(fi.seriesCount, vals[1]);
                            break;
                        case ("imageCount"):
                            fi.imageCount = StringToTagValue(fi.imageCount, vals[1]);
                            break;
                        case ("sizeX"):
                            fi.sizeX = StringToTagValue(fi.sizeX, vals[1]);
                            break;
                        case ("sizeY"):
                            fi.sizeY = StringToTagValue(fi.sizeY, vals[1]);
                            break;
                        case ("sizeC"):
                            fi.sizeC = StringToTagValue(fi.sizeC, vals[1]);
                            fi.roiList = new List<ROI>[fi.sizeC];
                            break;
                        case ("sizeZ"):
                            fi.sizeZ = StringToTagValue(fi.sizeZ, vals[1]);
                            break;
                        case ("sizeT"):
                            fi.sizeT = StringToTagValue(fi.sizeT, vals[1]);
                            break;
                        case ("umXY"):
                            fi.umXY = StringToTagValue(fi.umXY, vals[1]);
                            break;
                        case ("umZ"):
                            fi.umZ = StringToTagValue(fi.umZ, vals[1]);
                            break;
                        case ("bitsPerPixel"):
                            fi.bitsPerPixel = StringToTagValue(fi.bitsPerPixel, vals[1]);
                            break;
                        case ("dimensionOrder"):
                            fi.dimensionOrder = StringToTagValue(fi.dimensionOrder, vals[1]);
                            break;
                        case ("pixelType"):
                            fi.pixelType = StringToTagValue(fi.pixelType, vals[1]);
                            break;
                        case ("FalseColored"):
                            fi.FalseColored = StringToTagValue(fi.FalseColored, vals[1]);
                            break;
                        case ("isIndexed"):
                            fi.isIndexed = StringToTagValue(fi.isIndexed, vals[1]);
                            break;
                        case ("MetadataComplete"):
                            fi.MetadataComplete = StringToTagValue(fi.MetadataComplete, vals[1]);
                            break;
                        case ("DatasetStructureDescription"):
                            fi.DatasetStructureDescription = StringToTagValue(fi.DatasetStructureDescription, vals[1]);
                            break;
                        case ("Micropoint"):
                            fi.Micropoint = StringToTagValue(fi.Micropoint, vals[1]);
                            break;
                        case ("autoDetectBandC"):
                            fi.autoDetectBandC = StringToTagValue(fi.autoDetectBandC, vals[1]);
                            break;
                        case ("applyToAllBandC"):
                            fi.applyToAllBandC = StringToTagValue(fi.applyToAllBandC, vals[1]);
                            break;
                        case ("xCompensation"):
                            fi.xCompensation = StringToTagValue(fi.xCompensation, vals[1]);
                            break;
                        case ("yCompensation"):
                            fi.yCompensation = StringToTagValue(fi.yCompensation, vals[1]);
                            break;
                        case ("DataSourceInd"):
                            fi.DataSourceInd = StringToTagValue(fi.DataSourceInd, vals[1]);
                            break;
                        case ("LutList"):
                            fi.LutList = StringToTagValue(fi.LutList, vals[1]);
                            break;
                        case ("TimeSteps"):
                            fi.TimeSteps = StringToTagValue(fi.TimeSteps, vals[1]);
                            break;
                        case ("MinBrightness"):
                            fi.MinBrightness = StringToTagValue(fi.MinBrightness, vals[1]);
                            break;
                        case ("MaxBrightness"):
                            fi.MaxBrightness = StringToTagValue(fi.MaxBrightness, vals[1]);
                            break;
                        case ("tracking_MaxSize"):
                            fi.tracking_MaxSize = StringToTagValue(fi.tracking_MaxSize, vals[1]);
                            break;
                        case ("tracking_MinSize"):
                            fi.tracking_MinSize = StringToTagValue(fi.tracking_MinSize, vals[1]);
                            break;
                        case ("tracking_Speed"):
                            fi.tracking_Speed = StringToTagValue(fi.tracking_Speed, vals[1]);
                            break;
                        case ("SegmentationProtocol"):
                            fi.SegmentationProtocol = StringToTagValue(fi.SegmentationProtocol, vals[1]);
                            break;
                        case ("SegmentationCBoxIndex"):
                            fi.SegmentationCBoxIndex = StringToTagValue(fi.SegmentationCBoxIndex, vals[1]);
                            break;
                        case ("thresholdsCBoxIndex"):
                            fi.thresholdsCBoxIndex = StringToTagValue(fi.thresholdsCBoxIndex, vals[1]);
                            break;
                        case ("SelectedSpotThresh"):
                            fi.SelectedSpotThresh = StringToTagValue(fi.SelectedSpotThresh, vals[1]);
                            break;
                        case ("typeSpotThresh"):
                            fi.typeSpotThresh = StringToTagValue(fi.typeSpotThresh, vals[1]);
                            break;
                        case ("SpotThresh"):
                            fi.SpotThresh = StringToTagValue(fi.SpotThresh, vals[1]);
                            break;
                        case ("spotSensitivity"):
                            fi.spotSensitivity = StringToTagValue(fi.spotSensitivity, vals[1]);
                            break;
                        case ("thresholds"):
                            fi.thresholds = StringToTagValue(fi.thresholds, vals[1]);
                            break;
                        case ("SpotColor"):
                            fi.SpotColor = StringToTagValue(fi.SpotColor, vals[1]);
                            break;
                        case ("RefSpotColor"):
                            fi.RefSpotColor = StringToTagValue(fi.RefSpotColor, vals[1]);
                            break;
                        case ("sumHistogramChecked"):
                            fi.sumHistogramChecked = StringToTagValue(fi.sumHistogramChecked, vals[1]);
                            break;
                        case ("SpotTailType"):
                            fi.SpotTailType = StringToTagValue(fi.SpotTailType, vals[1]);
                            break;
                        case ("thresholdColors"):
                            fi.thresholdColors = StringToTagValue(fi.thresholdColors, vals[1]);
                            break;
                        case ("RefThresholdColors"):
                            fi.RefThresholdColors = StringToTagValue(fi.RefThresholdColors, vals[1]);
                            break;
                        case ("thresholdValues"):
                            fi.thresholdValues = StringToTagValue(fi.thresholdValues, vals[1]);
                            break;
                        case ("FileDescription"):
                            fi.FileDescription = StringToTagValue(fi.FileDescription, vals[1]);
                            break;
                        case ("roi.new"):
                            roi_new(vals[1], fi);
                            break;
                        case ("FilterHistory"):
                            FilterHistory = StringToTagValue(FilterHistory, vals[1]);
                            break;
                        case ("xAxisTB"):
                            fi.xAxisTB = int.Parse(vals[1]);
                            break;
                        case ("yAxisTB"):
                            fi.yAxisTB = int.Parse(vals[1]);
                            break;
                        case ("yFormula"): 
                                fi.yAxisTB = int.Parse(vals[1]);
                            break;
                        case ("watershed"):
                            break;
                        case ("newFilters"):
                            break;
                        default:
                            Console.WriteLine("Error: " + vals[0]);
                            break;
                    }
                }
                catch
                {
                    Console.WriteLine("Error: \n" + vals[0] + ":\t" + vals[1]);
                }
            }
            return FilterHistory;
        }
        public static void LoadRoiSet(string fileName, TifFileInfo fi)
        {
            if (!File.Exists(fileName)) return;

            fi.roiList = new List<ROI>[fi.sizeC];

            for (int c = 0; c < fi.sizeC; c++)
                fi.roiList[c] = new List<ROI>();

            using (StreamReader sr = new StreamReader(fileName))
            {
                string str = sr.ReadToEnd();
                foreach (string val in str.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                {
                    if (val != "")
                        roi_new(val, fi);
                }
            }

        }
        private static void roi_new(string val, TifFileInfo fi)
        {
            
            string[] vals = val.Substring(8,val.Length - 9).Split(new string[] { "," }, StringSplitOptions.None);
            int chanel = Convert.ToInt32(vals[0]);
            int RoiID = Convert.ToInt32(vals[1]);
            
            string RoiInfo = vals[2];

            ROI current =  CreateFromHistory(RoiID, RoiInfo, fi);

            if (fi.roiList[chanel] == null) fi.roiList[chanel] = new List<ROI>();
            fi.roiList[chanel].Add(current);

            if (fi.ROICounter <= RoiID) fi.ROICounter = RoiID + 1;
        }
        private static ROI CreateFromHistory(int RoiID, string val, TifFileInfo fi)
        {
            val = val.Remove(val.Length - 1, 1).Remove(0, 1);
            string[] vals = val.Split(new string[] { "\n" }, StringSplitOptions.None);


            int shape = int.Parse(vals[0]);
            int type = int.Parse(vals[1]);
            int W = int.Parse(vals[2]);
            int H = int.Parse(vals[3]);
            int stack = int.Parse(vals[4]);
            int d = int.Parse(vals[5]);
            int FromT = int.Parse(vals[6]);
            int ToT = int.Parse(vals[7]);
            int FromZ = int.Parse(vals[8]);
            int ToZ = int.Parse(vals[9]);
            int BiggestW = int.Parse(vals[10]);
            int BiggestH = int.Parse(vals[11]);
            bool ReturnBiggest = bool.Parse(vals[12]);
            bool turnOnStackRoi = bool.Parse(vals[13]);
            int i = 14;

            ROI current = new ROI(RoiID, fi.imageCount, shape, type, turnOnStackRoi);
            current.Width = W;
            current.Height = H;
            current.Stack = stack;
            current.D = d;
            current.FromT = FromT;
            current.ToT = ToT;
            current.FromZ = FromZ;
            current.ToZ = ToZ;
            current.biggestH = BiggestH;
            current.biggestW = BiggestW;
            current.returnBiggest = ReturnBiggest;

            if (vals.Length > 15)
                try
                {
                    i++;
                    i++;
                }
                catch { }

            if (vals.Length > 16)
            {
                if (vals[i].StartsWith("Comment="))
                {
                    current.Comment = vals[i].Replace("Comment=", "");
                    i++;
                }
            }

            setLocationFromHistory(current, vals, i);
            return current;
        }
        private static void setLocationFromHistory(ROI roi,string[] vals, int n)
        {
            Point[][] points = new Point[vals.Length - n][];
            for (int i = n, frame = 0; i < vals.Length; i++, frame++)
            {
                string[] row = vals[i].Split(new string[] { "\t" }, StringSplitOptions.None);
                List<Point> rowFinal = new List<Point>();
                if (row.Length > 1)
                    for (int x = 0, y = 1; y < row.Length; x += 2, y += 2)
                        rowFinal.Add(new Point(int.Parse(row[x]), int.Parse(row[y])));

                points[frame] = rowFinal.ToArray();
            }
            roi.SetLocationAll(points);
        }

        private static string StringToTagValue(string d, string val)
        {
            return val;
        }
        private static int StringToTagValue(int d, string val)
        {
            return int.Parse(val);
        }
        private static double StringToTagValue(double d, string val)
        {
            try
            {
                return double.Parse(val);
            }
            catch
            {
                if (val.Contains("."))
                    return double.Parse(val.Replace(".", ","));
                else if (val.Contains(","))
                    return double.Parse(val.Replace(",", "."));
                else
                    return 0;
            }
        }
        private static bool StringToTagValue(bool d, string val)
        {
            return bool.Parse(val);
        }
        private static int[][] StringToTagValue(int[][] d,string val)
        {
            List<int[]> res = new List<int[]>();
            foreach (string line in val.Split(new string[] { "\n" }, StringSplitOptions.None))
            {
                List<int> smallRes = new List<int>();
                foreach (string i in line.Split(new string[] { "\t" }, StringSplitOptions.None))
                    if (i != "")
                        smallRes.Add(int.Parse(i));
                res.Add(smallRes.ToArray());
            }

            return res.ToArray();
        }
        private static List<string>[] StringToTagValue(List<string>[] d, string val, int c)
        {
            string[] vals = val.Split(new string[] { "\n" }, StringSplitOptions.None);

            List<string>[] res = new List<string>[c];

            for (int i = 0; i < c; i++)
                if (vals[i] != "")
                    res[i] = vals[i].Split(new string[] { "{}" }, StringSplitOptions.None).ToList();
           
            return res;
        }
        private static string[] StringToTagValue(string[] d, string val)
        {
            List<string> res = new List<string>();
            foreach (string i in val.Split(new string[] { "\t" }, StringSplitOptions.None))
                if (i != "")
                    res.Add(i);

            return res.ToArray();
        }
        private static List<double> StringToTagValue(List<double> d, string val)
        {
            List<double> res = new List<double>();
            foreach (string i in val.Split(new string[] { "\t" }, StringSplitOptions.None))
                if (i != "")
                    try
                    {
                        res.Add(double.Parse(i));
                    }
                    catch
                    {
                        if (i.Contains("."))
                            res.Add(double.Parse(i.Replace(".", ",")));
                        else if (i.Contains(","))
                            res.Add(double.Parse(i.Replace(",", ".")));
                    }

            return res;
        }
        private static int[] StringToTagValue(int[] intArr, string val)
        {
            List<int> res = new List<int>();
            foreach (string i in val.Split(new string[] { "\t" }, StringSplitOptions.None))
                if (i != "")
                    try
                    {
                        res.Add(int.Parse(i));
                    }
                    catch { }

            return res.ToArray();
        }
        private static bool[] StringToTagValue(bool[] bArr, string val)
        {
            List<bool> res = new List<bool>();
            foreach (string i in val.Split(new string[] { "\t" }, StringSplitOptions.None))
                if (i != "")
                    res.Add(bool.Parse(i));

            return res.ToArray();
        }
        private static Color[][] StringToTagValue(Color[][] cBigList, string val)
        {
            List<Color[]> res = new List<Color[]>();
            foreach (string line in val.Split(new string[] { "\n" }, StringSplitOptions.None))
            {
                List<Color> smallRes = new List<Color>();
                foreach (string i in line.Split(new string[] { "\t" }, StringSplitOptions.None))
                    if (i != "")
                        smallRes.Add(ColorTranslator.FromHtml(i));
                res.Add(smallRes.ToArray());
            }

            return res.ToArray();
        }
        private static Color[] StringToTagValue(Color[] cList, string val)
        {
            List<Color> smallRes = new List<Color>();
            foreach (string i in val.Split(new string[] { "\t" }, StringSplitOptions.None))
                if (i != "")
                    smallRes.Add(ColorTranslator.FromHtml(i));

            return smallRes.ToArray();
        }
        private static List<Color> StringToTagValue(List<Color> cList, string val)
        {
            List<Color> smallRes = new List<Color>();
            foreach (string i in val.Split(new string[] { "\t" }, StringSplitOptions.None))
                if (i != "")
                    smallRes.Add(ColorTranslator.FromHtml(i));

            return smallRes;
        }
        #endregion Read CellTool3 metadata
    }
}
