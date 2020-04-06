using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellToolDK;
using System.Drawing;

namespace AutoSobel2
{
    class SobelOperator
    {
        public static void applySobel(TifFileInfo fi)
        {
            string[] kernelStrings = new string[]
            {
            "-1,-2,-1," +
                    "0,0,0," +
                    "1,2,1",
                    "-1,0,1," +
                    "-2,0,2," +
                    "-1,0,1"
            };
            MyConvolution.SobelOperation(fi.cValue, fi, kernelStrings[0], kernelStrings[1]);
        }
        class MyConvolution
        {
            #region kernels
            private static int[][] KernelMatrix(string input)
            {
                //calculates the kernel from string
                string[] kernelVals = input.Split(new string[] { "," }, StringSplitOptions.None);
                int size = (int)Math.Sqrt(kernelVals.Length);

                int[][] kernel = new int[size][];
                kernel[0] = new int[size];
                for (int position = 0, x = 0, y = 0; position < kernelVals.Length && y < size; position++, x++)
                {
                    if (x == size)
                    {
                        x = 0;
                        y++;
                        kernel[y] = new int[size];
                    }

                    kernel[y][x] = int.Parse(kernelVals[position]);
                }

                return kernel;
            }
            #endregion kernels

            #region global variables
            public static void CheckIsImagePrepared(TifFileInfo fi)
            {
                try
                {
                    switch (fi.bitsPerPixel)
                    {
                        case 8:
                            //return if the image is new
                            if (fi.image8bitFilter != null &&
                                fi.image8bitFilter != fi.image8bit)
                                return;
                            //duplicate
                            byte[][][] newImage8 = new byte[fi.imageCount][][];
                            Parallel.For(0, fi.imageCount, (ind) =>
                            {
                                byte[][] frame = new byte[fi.sizeY][];
                                for (int y = 0; y < fi.sizeY; y++)
                                {
                                    frame[y] = new byte[fi.sizeX];
                                    if (fi.image8bitFilter != null)
                                        Array.Copy(fi.image8bitFilter[ind][y], frame[y], fi.sizeX);
                                }
                                newImage8[ind] = frame;
                            });
                            fi.image8bitFilter = newImage8;

                            return;
                        case 16:
                            //return if the image is new
                            if (fi.image16bitFilter != null &&
                                fi.image16bitFilter != fi.image16bit)
                                return;
                            //duplicate
                            ushort[][][] newImage16 = new ushort[fi.imageCount][][];
                            Parallel.For(0, fi.imageCount, (ind) =>
                            {
                                ushort[][] frame = new ushort[fi.sizeY][];
                                for (int y = 0; y < fi.sizeY; y++)
                                {
                                    frame[y] = new ushort[fi.sizeX];
                                    if (fi.image16bitFilter != null)
                                        Array.Copy(fi.image16bitFilter[ind][y], frame[y], fi.sizeX);
                                }
                                newImage16[ind] = frame;
                            });
                            fi.image16bitFilter = newImage16;
                            return;
                    }
                }
                catch { }
            }

            private static int KernelCoeficient(int[][] kernel)
            {
                int sum = 0;
                //calculate coefition = sum(all values in the table)
                foreach (int[] row in kernel)
                    foreach (int val in row)
                        sum += val;

                return sum;
            }
            private static int[] KernelPxlVal(int[][] kernel)
            {
                List<int> val = new List<int>();
                //check the table for non zero values and apply them to array
                for (int y = 0; y < kernel.Length; y++)
                    for (int x = 0; x < kernel[y].Length; x++)
                        if (kernel[y][x] != 0)
                            val.Add(kernel[y][x]);

                return val.ToArray();
            }
            private static Point[] KernelPxlCord(int[][] kernel)
            {
                List<Point> pixels = new List<Point>();
                int index = -(kernel.Length - 1) / 2;
                //check the table for non zero values and calculate the coordinates
                for (int y = 0; y < kernel.Length; y++)
                    for (int x = 0; x < kernel[y].Length; x++)
                        if (kernel[y][x] != 0)
                            pixels.Add(new Point(x + index, y + index));

                return pixels.ToArray();
            }
            private static Point[] MedianPoints(int rad)
            {
                List<Point> pixels = new List<Point>();
                int index = rad + rad + 1;
                //calculate the coordinates
                for (int y = 0; y < index; y++)
                    for (int x = 0; x < index; x++)
                        pixels.Add(new Point(x - rad, y - rad));

                return pixels.ToArray();
            }
            private static Point[][][] ImagePxlMatrix(Point[] pxlCords, TifFileInfo fi, int[][] kernel)
            {
                //prepare matrix
                Point[][][] ImagePxlMatr = new Point[fi.sizeY][][];
                //fill the matrix with lists of neighbours
                Parallel.For(0, fi.sizeY, y =>
                {
                    Point[][] Row = new Point[fi.sizeX][];
                    for (int x = 0; x < fi.sizeX; x++)
                    {
                        Point[] pList = new Point[pxlCords.Length];
                        for (int i = 0; i < pxlCords.Length; i++)
                        {
                            pList[i].X = pxlCords[i].X + x;
                            pList[i].Y = pxlCords[i].Y + y;
                        }
                        Row[x] = pList;
                    }
                    ImagePxlMatr[y] = Row;
                });
                //fill the corners
                int CornerConst = ((kernel.Length - 1) / 2);
                int w = ImagePxlMatr[0].Length;
                int h = ImagePxlMatr.Length;

                for (int i = 0; i < CornerConst; i++)
                {
                    //up rows
                    foreach (Point[] pList in ImagePxlMatr[i])
                        for (int countP = 0; countP < pList.Length; countP++)
                        {
                            Point p = pList[countP];
                            if (p.X < 0) { p.X = 0; }
                            if (p.Y < 0) { p.Y = 0; }
                            if (p.X >= w) { p.X = w - 1; }
                            if (p.Y >= h) { p.Y = h - 1; }
                            pList[countP] = p;
                        }
                    //down rows
                    foreach (Point[] pList in ImagePxlMatr[(h - 1) - i])
                        for (int countP = 0; countP < pList.Length; countP++)
                        {
                            Point p = pList[countP];
                            if (p.X < 0) { p.X = 0; }
                            if (p.Y < 0) { p.Y = 0; }
                            if (p.X >= w) { p.X = w - 1; }
                            if (p.Y >= h) { p.Y = h - 1; }
                            pList[countP] = p;
                        }
                    //columns
                    for (int y = 0; y < h; y++)
                    {
                        //left column
                        Point[] pList = ImagePxlMatr[y][i];

                        for (int countP = 0; countP < pList.Length; countP++)
                        {
                            Point p = pList[countP];
                            if (p.X < 0) { p.X = 0; }
                            if (p.Y < 0) { p.Y = 0; }
                            if (p.X >= w) { p.X = w - 1; }
                            if (p.Y >= h) { p.Y = h - 1; }
                            pList[countP] = p;
                        }
                        //right column

                        pList = ImagePxlMatr[y][(w - 1) - i];
                        for (int countP = 0; countP < pList.Length; countP++)
                        {
                            Point p = pList[countP];
                            if (p.X < 0) { p.X = 0; }
                            if (p.Y < 0) { p.Y = 0; }
                            if (p.X >= w) { p.X = w - 1; }
                            if (p.Y >= h) { p.Y = h - 1; }
                            pList[countP] = p;
                        }
                    }
                }
                //End
                return ImagePxlMatr;
            }
            public static int[] GetFramesArray(int C, TifFileInfo fi)
            {
                int[] indexes = new int[fi.imageCount / fi.sizeC];

                for (int i = C, position = 0; i < fi.imageCount; i += fi.sizeC, position++)
                    indexes[position] = i;

                return indexes;
            }
            #endregion global variables

            #region Smooth the image
            //source https://en.wikipedia.org/wiki/Kernel_(image_processing)
            public static void SmoothImage(int C, TifFileInfo fi, string inKernel)
            {
                CheckIsImagePrepared(fi);
                //choose kernel table
                int[][] kernel = KernelMatrix(inKernel);
                //find the deviding coeficient
                int coeficient = KernelCoeficient(kernel);
                //prepare array with exact coordinates for the kernel table mumbers with val
                Point[] pxlCords = KernelPxlCord(kernel);
                //prepare array with the values != 0 in the kernel table
                int[] pxlVals = KernelPxlVal(kernel);
                //prepare shablon image matrix with all exact coordinates for sum
                Point[][][] ImagePxlMatr = ImagePxlMatrix(pxlCords, fi, kernel);

                switch (fi.bitsPerPixel)
                {
                    case 8:
                        byte[][][] image8bit = SmoothAllStack8bit(C, ImagePxlMatr, fi, pxlVals, coeficient);
                        fi.image8bitFilter = image8bit;
                        break;
                    case 16:
                        ushort[][][] image16bit = SmoothAllStack16bit(C, ImagePxlMatr, fi, pxlVals, coeficient);
                        fi.image16bitFilter = image16bit;
                        break;
                }
            }
            private static ushort[][][] SmoothAllStack16bit(int C, Point[][][] ImagePxlMatr, TifFileInfo fi, int[] pxlVals, double coeficient)
            {
                ushort[][][] image = fi.image16bitFilter;//source image
                ushort maxVal = ushort.MaxValue; //16 bit image max intensity
                double MinValue = ushort.MinValue; //0

                int[] imageIndexes = GetFramesArray(C, fi);
                Parallel.ForEach(imageIndexes, frame =>
                {
                    ushort[][] selectedImage = new ushort[fi.sizeY][];
                    ushort[][] origSelectedImage = image[frame];

                    for (int y = 0; y < fi.sizeY; y++)
                    {
                        ushort[] row = new ushort[fi.sizeX];
                        for (int x = 0; x < fi.sizeX; x++)
                        {
                            double val = 0;
                            Point[] plist = ImagePxlMatr[y][x];
                        //calculate value of the neighbors
                        for (int pInd = 0; pInd < plist.Length; pInd++)
                            {
                                Point p = plist[pInd];
                                val += ((double)origSelectedImage[p.Y][p.X] * (double)pxlVals[pInd]);
                            }

                        //normalize
                        if (coeficient != 1)
                                if (coeficient == 0)
                                    val = Math.Abs(val);
                                else
                                    val /= coeficient;

                        //check the range of the value and apply
                        if (val > maxVal)
                                row[x] = maxVal;
                            else if (val < MinValue)
                                row[x] = ushort.MinValue;
                            else
                                row[x] = (ushort)val;
                        }
                    //apply the new row
                    selectedImage[y] = row;
                    }
                //apply the new frame
                image[frame] = selectedImage;
                });
                //return result image
                return image;
            }
            private static byte[][][] SmoothAllStack8bit(int C, Point[][][] ImagePxlMatr, TifFileInfo fi, int[] pxlVals, double coeficient)
            {
                byte[][][] image = fi.image8bitFilter;//source image
                double maxVal = byte.MaxValue - 2;//255
                double MinValue = byte.MinValue;//0

                int[] imageIndexes = GetFramesArray(C, fi);
                Parallel.ForEach(imageIndexes, frame =>
                {
                    byte[][] selectedImage = new byte[fi.sizeY][];
                    byte[][] origSelectedImage = image[frame];
                    for (int y = 0; y < fi.sizeY; y++)
                    {
                        byte[] row = new byte[fi.sizeX];
                        for (int x = 0; x < fi.sizeX; x++)
                        {
                            double val = 0;
                            Point[] plist = ImagePxlMatr[y][x];

                        //calculate value of the neighbors
                        for (int pInd = 0; pInd < plist.Length; pInd++)
                            {
                                Point p = plist[pInd];
                                val += ((double)origSelectedImage[p.Y][p.X] * (double)pxlVals[pInd]);
                            }
                        //normalize
                        if (coeficient != 1)
                                if (coeficient == 0)
                                    val = Math.Abs(val);
                                else
                                    val /= coeficient;
                        //check the range of the value and apply
                        if (val >= maxVal)
                                row[x] = byte.MaxValue - 1;
                            else if (val < MinValue)
                                row[x] = byte.MinValue;
                            else
                                row[x] = (byte)val;
                        }
                    //apply the new row
                    selectedImage[y] = row;
                    }
                //apply the new frame
                image[frame] = selectedImage;
                });
                //return result image
                return image;
            }

            public static void Median(int C, int rad, TifFileInfo fi)
            {
                CheckIsImagePrepared(fi);

                Point[] points = MedianPoints(rad);

                int[] imageIndexes = GetFramesArray(C, fi);
                switch (fi.bitsPerPixel)
                {
                    case 8:
                        Parallel.ForEach(imageIndexes, frame =>
                        {
                            fi.image8bitFilter[frame] = MedianAlgorithm(fi.image8bitFilter[frame], points, fi);
                        });
                        break;
                    case 16:
                        Parallel.ForEach(imageIndexes, frame =>
                        {
                            fi.image16bitFilter[frame] = MedianAlgorithm(fi.image16bitFilter[frame], points, fi);
                        });
                        break;
                }
            }
            private static byte[][] MedianAlgorithm(byte[][] image, Point[] points, TifFileInfo fi)
            {
                int x, y;
                byte[][] newImage = new byte[fi.sizeY][];
                List<byte> l = new List<byte>();
                for (y = 0; y < fi.sizeY; y++)
                {
                    newImage[y] = new byte[fi.sizeX];
                    Array.Copy(image[y], newImage[y], fi.sizeX);
                    for (x = 0; x < fi.sizeX; x++)
                    {
                        l.Clear();
                        foreach (Point p in points)
                            if (x + p.X >= 0 && x + p.X < fi.sizeX &&
                                y + p.Y >= 0 && y + p.Y < fi.sizeY)
                                l.Add(image[y + p.Y][x + p.X]);

                        newImage[y][x] = findMedian(l);
                    }
                }
                return newImage;
            }
            private static byte findMedian(List<byte> myList)
            {
                byte val = 0;

                myList.Sort();

                int med = (int)(myList.Count / 2 - 0.2);

                //nechetno
                if (med + med != myList.Count)
                    val = myList[med];
                else
                    val = (byte)((myList[med] + myList[med - 1]) / 2);

                return val;
            }
            private static ushort findMedian(List<ushort> myList)
            {
                ushort val = 0;
                myList.Sort();

                int med = (int)(myList.Count / 2 - 0.2);

                //nechetno
                if (med + med != myList.Count)
                    val = myList[med];
                else
                    val = (ushort)((myList[med] + myList[med - 1]) / 2);

                return val;
            }
            private static ushort[][] MedianAlgorithm(ushort[][] image, Point[] points, TifFileInfo fi)
            {
                int x, y;
                ushort[][] newImage = new ushort[fi.sizeY][];
                List<ushort> l = new List<ushort>();

                for (y = 0; y < fi.sizeY; y++)
                {
                    newImage[y] = new ushort[fi.sizeX];
                    Array.Copy(image[y], newImage[y], fi.sizeX);
                    for (x = 0; x < fi.sizeX; x++)
                    {
                        l.Clear();

                        foreach (Point p in points)
                            if (x + p.X >= 0 && x + p.X < fi.sizeX &&
                                y + p.Y >= 0 && y + p.Y < fi.sizeY)
                                l.Add(image[y + p.Y][x + p.X]);

                        newImage[y][x] = findMedian(l);
                    }
                }
                return newImage;
            }
            #endregion Smooth the image
            #region Sobel Operation
            public static void SobelOperation(int C, TifFileInfo fi, string inKernelTopDown, string inKernelLeftRight)
            {
                CheckIsImagePrepared(fi);
                //choose kernel table
                int[][] kernelTopDown = KernelMatrix(inKernelTopDown);
                int[][] kernelLeftRight = KernelMatrix(inKernelLeftRight);

                //prepare array with exact coordinates for the kernel table mumbers with val
                Point[] pxlCordsTopDown = KernelPxlCord(kernelTopDown);
                Point[] pxlCordsLeftRight = KernelPxlCord(kernelLeftRight);
                //prepare array with the values != 0 in the kernel table
                int[] pxlValsTopDown = KernelPxlVal(kernelTopDown);
                int[] pxlValsLeftRight = KernelPxlVal(kernelLeftRight);
                //prepare shablon image matrix with all exact coordinates for sum
                Point[][][] ImagePxlMatrTopDown = ImagePxlMatrix(pxlCordsTopDown, fi, kernelTopDown);
                Point[][][] ImagePxlMatrLeftRight = ImagePxlMatrix(pxlCordsLeftRight, fi, kernelLeftRight);

                switch (fi.bitsPerPixel)
                {
                    case 8:
                        byte[][][] image8bit = detectEdgesAllStack8bit(
                            C, ImagePxlMatrTopDown, ImagePxlMatrLeftRight, fi,
                            pxlValsTopDown, pxlValsLeftRight);
                        fi.image8bitFilter = image8bit;
                        break;
                    case 16:
                        ushort[][][] image16bit = detectEdgesAllStack16bit(
                            C, ImagePxlMatrTopDown, ImagePxlMatrLeftRight, fi,
                            pxlValsTopDown, pxlValsLeftRight);
                        fi.image16bitFilter = image16bit;
                        break;
                }

            }
            private static byte[][][] detectEdgesAllStack8bit(int C, Point[][][] ImagePxlMatrTopDown, Point[][][] ImagePxlMatrLeftRight,
                TifFileInfo fi, int[] pxlValsTopDown, int[] pxlValsLeftRight)
            {
                byte[][][] image = fi.image8bitFilter;
                double maxVal = byte.MaxValue - 2;
                double MinValue = byte.MinValue;

                int[] imageIndexes = GetFramesArray(C, fi);
                Parallel.ForEach(imageIndexes, frame =>
                {
                    byte[][] selectedImage = new byte[fi.sizeY][];
                    byte[][] origSelectedImage = image[frame];
                    for (int y = 0; y < fi.sizeY; y++)
                    {
                        byte[] row = new byte[fi.sizeX];
                        for (int x = 0; x < fi.sizeX; x++)
                        {
                        //Top-Down
                        double valTopDown = 0;
                            Point[] plistTopDown = ImagePxlMatrTopDown[y][x];
                            for (int pInd = 0; pInd < plistTopDown.Length; pInd++)
                            {
                                Point p = plistTopDown[pInd];
                                valTopDown += ((double)origSelectedImage[p.Y][p.X] * (double)pxlValsTopDown[pInd]);
                            }
                            valTopDown = Math.Abs(valTopDown);
                        //Left-Right
                        double valLeftRight = 0;
                            Point[] plistLeftRight = ImagePxlMatrLeftRight[y][x];
                            for (int pInd = 0; pInd < plistLeftRight.Length; pInd++)
                            {
                                Point p = plistLeftRight[pInd];
                                valLeftRight += ((double)origSelectedImage[p.Y][p.X] * (double)pxlValsLeftRight[pInd]);
                            }
                            valLeftRight = Math.Abs(valLeftRight);
                        //sum
                        double val = (valLeftRight + valTopDown) / 2;
                        //check the value range
                        if (val >= maxVal)
                                row[x] = byte.MaxValue - 1;
                            else if (val < MinValue)
                                row[x] = byte.MinValue;
                            else
                                row[x] = (byte)val;
                        }
                        selectedImage[y] = row;
                    }
                    image[frame] = selectedImage;
                });

                return image;
            }
            private static ushort[][][] detectEdgesAllStack16bit(int C, Point[][][] ImagePxlMatrTopDown, Point[][][] ImagePxlMatrLeftRight,
                TifFileInfo fi, int[] pxlValsTopDown, int[] pxlValsLeftRight)
            {
                ushort[][][] image = fi.image16bitFilter;
                ushort maxVal = ushort.MaxValue;
                double MinValue = ushort.MinValue;

                int[] imageIndexes = GetFramesArray(C, fi);
                Parallel.ForEach(imageIndexes, frame =>
                {
                    ushort[][] selectedImage = new ushort[fi.sizeY][];
                    ushort[][] origSelectedImage = image[frame];
                    for (int y = 0; y < fi.sizeY; y++)
                    {
                        ushort[] row = new ushort[fi.sizeX];
                        for (int x = 0; x < fi.sizeX; x++)
                        {
                        //Top-Down
                        double valTopDown = 0;
                            Point[] plistTopDown = ImagePxlMatrTopDown[y][x];
                            for (int pInd = 0; pInd < plistTopDown.Length; pInd++)
                            {
                                Point p = plistTopDown[pInd];
                                valTopDown += ((double)origSelectedImage[p.Y][p.X] * (double)pxlValsTopDown[pInd]);
                            }
                            valTopDown = Math.Abs(valTopDown);
                        //Right-Left
                        double valLeftRight = 0;
                            Point[] plistLeftRight = ImagePxlMatrLeftRight[y][x];
                            for (int pInd = 0; pInd < plistLeftRight.Length; pInd++)
                            {
                                Point p = plistLeftRight[pInd];
                                valLeftRight += ((double)origSelectedImage[p.Y][p.X] * (double)pxlValsLeftRight[pInd]);
                            }
                            valLeftRight = Math.Abs(valLeftRight);
                        //sum
                        double val = (valLeftRight + valTopDown) / 2;
                            if (val > maxVal)
                                row[x] = maxVal;
                            else if (val < MinValue)
                                row[x] = ushort.MinValue;
                            else
                                row[x] = (ushort)val;
                        }
                        selectedImage[y] = row;
                    }
                    image[frame] = selectedImage;
                });
                return image;
            }
            #endregion Sobel Operation detection
        }
    }
}


