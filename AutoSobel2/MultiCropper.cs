using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellToolDK;
using System.Drawing;

namespace AutoSobel2
{
    class MultiCropper
    {
        public static TifFileInfo ProcessROI(string dir, ROI roi, TifFileInfo fi)
        {
            if (!roi.Checked || roi.Shape < 2) return null;
                    
           return CropToolStripMenuItem_click(roi, dir, fi);
            
        }
        private static TifFileInfo TrackCropToolStripMenuItem_click(ROI roi, string dir, TifFileInfo fi)
        {
            ROI oldROI = roi;
            roi = ROITransformer.ROIEditor.TransformToRect(fi, roi);

            if (roi == null) return null;

            fi.available = false;
            TifFileInfo newFI = null;

            //crop the rectangle
            newFI = DuplicateFI(fi);
            newFI.Dir = newFI.Dir.Substring(0, newFI.Dir.LastIndexOf(".")) + "_ROI"
                + (fi.roiList[fi.cValue].IndexOf(roi) + 1).ToString() + ".tif";
            Size size = new Size(roi.Width, roi.Height);
            newFI.sizeX = roi.Width;
            newFI.sizeY = roi.Height;
            newFI.xCompensation = 0;
            newFI.yCompensation = 0;

            newFI.imageCount = fi.imageCount;
            newFI.openedImages = newFI.imageCount;
            AddEmptyArraysToFI(newFI);

            Point[] locs = roi.GetLocationAll()[0];

            switch (fi.bitsPerPixel)
            {
                case 8:

                    byte[][][] image8bit = new byte[fi.imageCount][][];
                    Parallel.For(0, fi.imageCount, frame =>
                    {
                        Point location = locs[frame];
                        Rectangle rect = new Rectangle(location, size);

                        image8bit[frame] = new byte[rect.Height][];
                        for (int y = rect.Y, yNew = 0; y < rect.Y + rect.Height; y++, yNew++)
                        {
                            image8bit[frame][yNew] = new byte[rect.Width];

                            for (int x = rect.X, xNew = 0; x < rect.X + rect.Width; x++, xNew++)
                                if (x >= 0 && y >= 0 && x < fi.sizeX && y < fi.sizeY)
                                    image8bit[frame][yNew][xNew] = fi.image8bit[frame][y][x];
                        }
                    });
                    newFI.image8bit = image8bit;
                    newFI.image8bitFilter = newFI.image8bit;
                    break;
                case 16:
                    ushort[][][] image16bit = new ushort[fi.imageCount][][];
                    Parallel.For(0, fi.imageCount, frame =>
                    {
                        Point location = locs[frame];

                        Rectangle rect = new Rectangle(location, size);
                        image16bit[frame] = new ushort[rect.Height][];
                        for (int y = rect.Y, yNew = 0; y < rect.Y + rect.Height; y++, yNew++)
                        {
                            image16bit[frame][yNew] = new ushort[rect.Width];

                            for (int x = rect.X, xNew = 0; x < rect.X + rect.Width; x++, xNew++)
                                if (x >= 0 && y >= 0 && x < fi.sizeX && y < fi.sizeY)
                                    image16bit[frame][yNew][xNew] = fi.image16bit[frame][y][x];
                        }
                    });
                    newFI.image16bit = image16bit;
                    newFI.image16bitFilter = newFI.image16bit;
                    break;
            }

            newFI.loaded = true;
            newFI.original = false;
            //FileEncoder.SaveTif(newFI, dir);
            fi.available = true;

            RecalculateOriginalROI(oldROI, roi, newFI);

            return newFI;
        }
        private static TifFileInfo CropToolStripMenuItem_click(ROI roi, string dir, TifFileInfo fi)
        {
            if (roi.Type == 1)
            {
                return TrackCropToolStripMenuItem_click(roi, dir,fi);
            }
            return null;
            fi.available = false;
            TifFileInfo newFI = null;

            Rectangle rect = Rectangle.Empty;
            //find rectangles

            switch (roi.Type)
            {
                case 0:
                    if (roi.Shape == 1 | roi.Shape == 0)
                    {
                        Point p = roi.GetLocation(fi.cValue)[0];
                        Size size = new Size(roi.Width, roi.Height);

                        rect = new Rectangle(p, size);

                    }
                    else if (roi.Shape == 2 || roi.Shape == 3 || roi.Shape == 4 || roi.Shape == 5)
                    {
                        Point[] pList = roi.GetLocation(fi.cValue);

                        int X = int.MaxValue;
                        int Y = int.MaxValue;
                        int W = int.MinValue;
                        int H = int.MinValue;

                        foreach (Point p1 in pList)
                        {
                            if (p1.X < X) X = p1.X;
                            if (p1.Y < Y) Y = p1.Y;
                            if (p1.X > W) W = p1.X;
                            if (p1.Y > H) H = p1.Y;
                        }

                        Point p = new Point(X, Y);
                        Size size = new Size(W - X, H - Y);

                        rect = new Rectangle(p, size);

                    }
                    break;
                case 1:
                    if (roi.Shape == 1 | roi.Shape == 0)
                    {
                        Point[] pList = roi.GetLocationAll()[0];

                        int X = int.MaxValue;
                        int Y = int.MaxValue;
                        int W = int.MinValue;
                        int H = int.MinValue;

                        for (int i = fi.cValue; i < fi.imageCount; i += fi.sizeC)
                        {
                            Point p1 = pList[i];
                            if (p1 != null)
                            {
                                if (p1.X < X) X = p1.X;
                                if (p1.Y < Y) Y = p1.Y;
                                if (p1.X > W) W = p1.X;
                                if (p1.Y > H) H = p1.Y;
                            }
                        }

                        Point p = new Point(X, Y);
                        Size size = new Size(W - X + roi.Width, H - Y + roi.Height);

                        rect = new Rectangle(p, size);

                    }
                    else if (roi.Shape == 2 || roi.Shape == 3 || roi.Shape == 4 || roi.Shape == 5)
                    {
                        int X = int.MaxValue;
                        int Y = int.MaxValue;
                        int W = int.MinValue;
                        int H = int.MinValue;
                        Point[] pList;

                        for (int i = fi.cValue; i < fi.imageCount; i += fi.sizeC)
                        {
                            pList = roi.GetLocation(i);

                            foreach (Point p1 in pList)
                            {
                                if (p1.X < X) X = p1.X;
                                if (p1.Y < Y) Y = p1.Y;
                                if (p1.X > W) W = p1.X;
                                if (p1.Y > H) H = p1.Y;
                            }
                        }

                        Point p = new Point(X, Y);
                        Size size = new Size(W - X, H - Y);

                        rect = new Rectangle(p, size);
                    }
                    break;
            }

            //crop the rectangle
            newFI = DuplicateFI(fi);
            newFI.Dir = newFI.Dir.Substring(0, newFI.Dir.LastIndexOf(".")) + "_ROI"
                + (fi.roiList[fi.cValue].IndexOf(roi) + 1).ToString() + ".tif";

            newFI.sizeX = rect.Width;
            newFI.sizeY = rect.Height;
            newFI.xCompensation = rect.X;
            newFI.yCompensation = rect.Y;

            newFI.imageCount = fi.imageCount;
            newFI.openedImages = newFI.imageCount;
            AddEmptyArraysToFI(newFI);


            switch (fi.bitsPerPixel)
            {
                case 8:
                    byte[][][] image8bit = new byte[fi.imageCount][][];
                    Parallel.For(0, fi.imageCount, frame =>
                    {
                        image8bit[frame] = new byte[rect.Height][];
                        for (int y = rect.Y, yNew = 0; y < rect.Y + rect.Height; y++, yNew++)
                        {
                            image8bit[frame][yNew] = new byte[rect.Width];

                            for (int x = rect.X, xNew = 0; x < rect.X + rect.Width; x++, xNew++)
                                if (x >= 0 && y >= 0 && x < fi.sizeX && y < fi.sizeY)
                                    image8bit[frame][yNew][xNew] = fi.image8bit[frame][y][x];
                        }
                    });
                    newFI.image8bit = image8bit;
                    newFI.image8bitFilter = newFI.image8bit;
                    break;
                case 16:
                    ushort[][][] image16bit = new ushort[fi.imageCount][][];
                    Parallel.For(0, fi.imageCount, frame =>
                    {
                        image16bit[frame] = new ushort[rect.Height][];
                        for (int y = rect.Y, yNew = 0; y < rect.Y + rect.Height; y++, yNew++)
                        {
                            image16bit[frame][yNew] = new ushort[rect.Width];

                            for (int x = rect.X, xNew = 0; x < rect.X + rect.Width; x++, xNew++)
                                if (x >= 0 && y >= 0 && x < fi.sizeX && y < fi.sizeY)
                                    image16bit[frame][yNew][xNew] = fi.image16bit[frame][y][x];
                        }
                    });
                    newFI.image16bit = image16bit;
                    newFI.image16bitFilter = newFI.image16bit;
                    break;
            }

            newFI.loaded = true;
            newFI.original = false;
            //FileEncoder.SaveTif(newFI, dir);
            fi.available = true;
            return newFI;
        }
        private static TifFileInfo DuplicateFI(TifFileInfo fi)
        {
            TifFileInfo newFi = new TifFileInfo();

            newFi.LutList = new List<Color>();
            foreach (Color col in fi.LutList)
                newFi.LutList.Add(col);

            newFi.Dir = fi.Dir;
            newFi.seriesCount = fi.seriesCount;
            newFi.imageCount = fi.imageCount;
            newFi.sizeX = fi.sizeX;
            newFi.sizeY = fi.sizeY;
            newFi.sizeZ = fi.sizeZ;
            newFi.umZ = fi.umZ;
            newFi.umXY = fi.umXY;
            newFi.sizeC = fi.sizeC;
            newFi.sizeT = fi.sizeT;
            newFi.bitsPerPixel = fi.bitsPerPixel;
            newFi.dimensionOrder = fi.dimensionOrder;
            newFi.pixelType = fi.pixelType;
            newFi.FalseColored = fi.FalseColored;
            newFi.isIndexed = fi.isIndexed;
            newFi.MetadataComplete = fi.MetadataComplete;
            newFi.DatasetStructureDescription = fi.DatasetStructureDescription;

            newFi.TimeSteps = fi.TimeSteps;
            newFi.Micropoint = fi.Micropoint;
            //Metadata protocol info
            newFi.FileDescription = fi.FileDescription;
            newFi.xCompensation = fi.xCompensation;
            newFi.yCompensation = fi.yCompensation;
            return newFi;
        }
        private static void AddEmptyArraysToFI(TifFileInfo fi)
        {
            
            #region Segmentation variables
            fi.SegmentationCBoxIndex = new int[fi.sizeC];
            fi.SegmentationProtocol = new int[fi.sizeC];
            fi.thresholdsCBoxIndex = new int[fi.sizeC];
            fi.sumHistogramChecked = new bool[fi.sizeC];
            fi.thresholdValues = new int[fi.sizeC][];
            fi.thresholdColors = new Color[fi.sizeC][];
            fi.RefThresholdColors = new Color[fi.sizeC][];
            fi.thresholds = new int[fi.sizeC];
            fi.SpotColor = new Color[fi.sizeC];
            fi.RefSpotColor = new Color[fi.sizeC];
            fi.SelectedSpotThresh = new int[fi.sizeC];
            fi.SpotThresh = new int[fi.sizeC];
            fi.typeSpotThresh = new int[fi.sizeC];
            fi.SpotTailType = new string[fi.sizeC];
            fi.spotSensitivity = new int[fi.sizeC];
            fi.roiList = new List<ROI>[fi.sizeC];
            fi.tracking_MaxSize = new int[fi.sizeC];
            fi.tracking_MinSize = new int[fi.sizeC];
            fi.tracking_Speed = new int[fi.sizeC];
            for (int i = 0; i < fi.sizeC; i++)
            {
                fi.sumHistogramChecked[i] = false;
                fi.thresholdValues[i] = new int[5];
                fi.thresholdColors[i] = new Color[]
                { Color.Transparent,Color.Transparent,Color.Transparent,Color.Transparent,Color.Transparent };
                fi.RefThresholdColors[i] = new Color[]
                {Color.Black,Color.Orange,Color.Green,Color.Blue,Color.Magenta};
                fi.SpotColor[i] = Color.Red;
                fi.RefSpotColor[i] = Color.Red;
                fi.SpotTailType[i] = "<";
                fi.spotSensitivity[i] = 100;
                fi.tracking_MaxSize[i] = 10000;
                fi.tracking_MinSize[i] = 5;
                fi.tracking_Speed[i] = 5;
            }
            #endregion Segmentation variables
            
        }
       public static void RecalculateOriginalROI(ROI original, ROI modified, TifFileInfo newFI)
        {
            if (modified != null)
                for (int i = 0; i < newFI.imageCount; i += newFI.sizeC)
                {
                    var points = original.GetLocation(i);
                    Point newP = modified.GetLocation(i)[0];
                    Point p;

                    for (int ind = 0; ind < points.Length; ind++)
                    {
                        p = points[ind];
                        p.X = p.X - newP.X;
                        p.Y = p.Y - newP.Y;
                        points[ind] = p;
                    }

                    original.SetLocation(i, points);
                }

            newFI.roiList = new List<ROI>[newFI.sizeC];
            for (int c = 0; c < newFI.sizeC; c++)
            {
                newFI.roiList[c] = new List<ROI>();
            }

            newFI.roiList[0].Add(original);

            if (newFI.sizeC > 1)
                for (int c = 1; c < newFI.sizeC; c++)
                {
                    ROI newROI = original.Duplicate();

                    for (int i = 0; i < newFI.imageCount; i += newFI.sizeC)
                    {
                        var points = original.GetLocation(i);

                        newROI.SetLocation(i + c, points);
                    }

                    newFI.roiList[c].Add(newROI);
                }
        }
    }
}
