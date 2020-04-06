using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using CellToolDK;
using System.IO;

namespace AutoSobel2
{
    class ExpressionNormalizer
    {
        public static void Process(double background,TifFileInfo fi)
        {
            //store Matrix options
            bool exportMatrix = true;
            //Load or create matrix
            List<double> Klist;

            double[][][] image = GetDoubleImage(fi);

            Klist = CalculateMatrix(background, image,fi);
           
            //calculate image
            ApplyMatrix(Klist, background, image,fi);
            //Export matrix
            if (exportMatrix)
            {
                string dir = fi.Dir.Substring(0,
                    fi.Dir.LastIndexOf(".")) + "_ExpNormMatrix_Ch" + (fi.cValue + 1) + ".txt";

                //check is the directory exist
                if (dir.IndexOf("\\") > -1)
                {
                    string checkDir = dir.Substring(0, dir.LastIndexOf("\\"));
                    if (!System.IO.Directory.Exists(checkDir)) System.IO.Directory.CreateDirectory(checkDir);
                }

                File.WriteAllText(dir,string.Join("\n", Klist));
            }
            
            SetDoubleImage(image,fi);
            image = null;
        }
       
        private static List<double> CalculateMatrix(double background, double[][][] image, TifFileInfo fi)
        {
            List<double> Klist = new List<double>();

            double value;
            int counter, i;

            for (i = fi.cValue; i < fi.imageCount; i += fi.sizeC)
            {
                value = 0;
                counter = 0;

                foreach (Point p in GetPolygonPoints(i,fi))
                {
                    value += image[i][p.Y][p.X] - background;
                    counter++;
                }

                if (counter > 0) value /= counter;

                Klist.Add(value);
            }

            double Max = Klist.Max();

            if (Max != 0)
                for (i = 0; i < Klist.Count; i++)
                    Klist[i] /= Max;

            return Klist;
        }
        private static void ApplyMatrix(List<double> Klist, double background, double[][][] image, TifFileInfo fi)
        {
            int i, x, y, position;
            for (i = fi.cValue, position = 0; i < fi.imageCount; i += fi.sizeC, position++)
            {
                for (x = 0; x < fi.sizeX; x++)
                    for (y = 0; y < fi.sizeY; y++)
                    {
                        image[i][y][x] = ((image[i][y][x] - background) / Klist[position]) + background;
                    }
            }
        }
        private static List<Point> GetPolygonPoints(int imageN, TifFileInfo fi)
        {
            Point[] points = fi.roiList[fi.cValue][0].GetLocation(imageN);
            List<Point> pList = new List<Point>();
            List<int> xList = new List<int>();
            int x, y, swap, i, j, maxY = 0, minY = fi.sizeY - 1;
            //check the size of the polygon
            foreach (Point p in points)
            {
                if (p.Y > maxY && p.Y < fi.sizeY) maxY = p.Y;
                if (p.Y < minY && p.Y >= 0) minY = p.Y;
            }
            //scan lines for Y coords
            for (y = minY; y <= maxY; y++)
            {
                //prepare list for X coords
                xList.Clear();
                j = points.Length - 1;
                //calculate X points via tgA function
                for (i = 0; i < points.Length; i++)
                {
                    if ((points[i].Y < y && points[j].Y >= y) ||
                        (points[j].Y < y && points[i].Y >= y))
                    {
                        // tgA = (y2-y1)/(x2-x1)
                        x = (int)((((y - points[i].Y) * (points[j].X - points[i].X)) /
                            (points[j].Y - points[i].Y)) + points[i].X);

                        xList.Add(x);
                    }

                    j = i;
                }
                //break if there is no points in the line
                if (xList.Count == 0) continue;
                //sort by value via bubble loop
                i = 0;
                while (i < xList.Count - 1)
                {
                    j = i + 1;
                    if (xList[i] > xList[j])
                    {
                        swap = xList[i];
                        xList[i] = xList[j];
                        xList[j] = swap;

                        if (i > 0) i--;
                    }
                    else
                    {
                        i++;
                    }
                }
                //find all points inside the bounds 2 by 2
                for (i = 0; i < xList.Count; i += 2)
                {
                    j = i + 1;
                    if (xList[i] >= fi.sizeX) break;
                    if (xList[j] > 0)
                    {
                        if (xList[i] < 0) xList[i] = 0;
                        if (xList[j] >= fi.sizeX) xList[j] = fi.sizeX - 1;

                        for (x = xList[i]; x <= xList[j]; x++)
                            pList.Add(new Point(x, y));
                    }
                }
            }

            return pList;
        }
        private static double[][][] GetDoubleImage( TifFileInfo fi)
        {
            double[][][] image = new double[fi.imageCount][][];

            for (int i = 0; i < fi.imageCount; i++)
            {
                image[i] = new double[fi.sizeY][];
                for (int y = 0; y < fi.sizeY; y++)
                {
                    image[i][y] = new double[fi.sizeX];
                    for (int x = 0; x < fi.sizeX; x++)
                        switch (fi.bitsPerPixel)
                        {
                            case 8:
                                image[i][y][x] = fi.image8bit[i][y][x];
                                break;
                            case 16:
                                image[i][y][x] = fi.image16bit[i][y][x];
                                break;
                        }
                }
            }

            return image;
        }
        private static void SetDoubleImage(double[][][] image, TifFileInfo fi)
        {
            double val;

            for (int i = 0; i < fi.imageCount; i++)
            {
                for (int y = 0; y < fi.sizeY; y++)
                {
                    for (int x = 0; x < fi.sizeX; x++)
                    {
                        val = image[i][y][x];
                        if (val < 0) val = 0;
                        switch (fi.bitsPerPixel)
                        {
                            case 8:
                                if (val > byte.MaxValue) val = byte.MaxValue;
                                fi.image8bit[i][y][x] = (byte)Math.Round(val, 0);
                                break;
                            case 16:
                                if (val > ushort.MaxValue) val = ushort.MaxValue;
                                fi.image16bit[i][y][x] = (ushort)Math.Round(val, 0);
                                break;
                        }
                    }
                    image[i][y] = null;
                }
                image[i] = null;
            }
            image = null;
        }
    }
}

