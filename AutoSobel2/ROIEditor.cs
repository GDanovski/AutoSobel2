using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellToolDK;
using System.Windows.Forms;
using System.Drawing;

namespace ROITransformer
{
    class ROIEditor
    {
        public static ROI TransformToRect(TifFileInfo fi, ROI source)
        {
            if (source.Checked == true && source.Type == 1 && source.Shape > 1)
            {
                ROI dest = NewROI(fi);
                //dest.SetLocationAll(GetRoiNewLocations(source));
                Point[][] points = GetRoiNewLocations(source);

                for (int c = 0; c < fi.sizeC; c++)
                    for (int i = fi.cValue, realC = c; i < fi.imageCount; i += fi.sizeC, realC += fi.sizeC)
                        dest.SetLocation(realC, points[i]);

                dest.Width = source.Width;
                dest.Height = source.Height;

                return dest;
            }
            else if (source.Checked == true && source.Type == 1 && source.Shape <= 1)
            {

                ROI dest = NewROI(fi);
                //dest.SetLocationAll(GetRoiNewLocations(source));
                Point[][] points = source.GetLocationAll();

                for (int c = 0; c < fi.sizeC; c++)
                    for (int i = fi.cValue, realC = c; i < fi.imageCount; i += fi.sizeC, realC += fi.sizeC)
                        dest.SetLocation(realC, points[i]);

                dest.Width = source.Width;
                dest.Height = source.Height;

                return dest;
            }

            return null;
        }
        private static ROI NewROI(TifFileInfo fi)
        {
            ROI roi = new ROI(fi.ROICounter, fi.imageCount, 0, 1, false);

            fi.ROICounter++;

            roi.FromT = 1;
            roi.FromZ = 1;
            roi.ToT = fi.sizeT;
            roi.ToZ = fi.sizeZ;
            
            return roi;
        }
        private static Point[][] GetRoiNewLocations(ROI roi)
        {
           Point[][] source = roi.GetLocationAll();
           Point[][] target = new Point[source.Length][];

           int W = 0;
           int H = 0;
           Rectangle rect;

            for (int i = 0; i < source.Length; i++)
                if (source[i] != null && source[i].Length != 0)
                {
                    rect = CalculateRectangle(source[i]);
                    target[i] = new Point[] { rect.Location};
                    if (W < rect.Width) W = rect.Width;
                    if (H < rect.Height) H = rect.Height;
                }

            W += 40;
            H += 40;

            roi.Width = W;
            roi.Height = H;

            W /= 2;
            H /= 2;
            
            for (int i = 0; i < target.Length; i++)
                if (target[i] != null && target[i].Length != 0)
                {
                    target[i][0].X -= W;
                    target[i][0].Y -= H;
                }

            return target;
        }
        private static Rectangle CalculateRectangle(Point[] source)
        {
            Rectangle rect = new Rectangle(int.MaxValue, int.MaxValue, 0, 0);

            foreach(Point p in source)
            {
                if (p.X < rect.X) rect.X = p.X;
                if (p.Y < rect.Y) rect.Y = p.Y;
                if (p.X > rect.Width) rect.Width = p.X;
                if (p.Y > rect.Height) rect.Height = p.Y;
            }

            rect.Width -= rect.X;
            rect.Height -= rect.Y;
            rect.X += (int)(rect.Width / 2);
            rect.Y += (int)(rect.Height / 2);

            return rect;
        }
    }
}
