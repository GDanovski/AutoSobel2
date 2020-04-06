using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellToolDK;
using System.IO;

namespace AutoSobel2
{
    class ImageAnalyser
    {
        /// <summary>
        /// Analyse image file - crop with tracking, copy Polygonal ROI, apply Sobel operator and save the results
        /// </summary>
        /// <param name="DirData">Output and input directories </param>
        /// <returns>True if file successfully processed</returns>
        public static bool Analyse(FileDirData DirData, double[] background)
        {
            TifFileInfo fi = new TifFileInfo();
           // try
            {
                //read celltool 3 image 
                var console = Console.Out;
                Console.SetOut(TextWriter.Null);
                Console.SetError(TextWriter.Null);

                fi = Cell_Tool_3.FileDecoder.ReadImage(DirData.InputDir);//read the image
                Cell_Tool_3.FileDecoder.LoadRoiSet(DirData.InputRoi, fi);//read roi file

                Console.SetOut(console);
                Console.SetError(console);
                int curInd = 1;
                if (fi.roiList != null && fi.roiList[0] != null)
                    foreach (ROI roi in fi.roiList[0])
                    {
                        if (roi.Checked && roi.Type == 1 && roi.Shape > 1)
                        {
                            FileDirData curDirData = DirData.Duplicate();

                            if (!curDirData.InputDir.EndsWith("_CompositeRegistred.tif") &&
                                !curDirData.InputDir.EndsWith("_Q2.tif") &&
                                !curDirData.InputDir.EndsWith("_1.tif"))
                            {
                                curDirData.ExtractClipNumber();
                                curDirData.AddCellIndex(curInd.ToString());
                            }


                            ProcessSingleRoi(fi, roi, curDirData, background);
                        }


                        curInd++;
                    }
                
                //clear and report
                fi.Delete();
                return true;
            }
            //catch
            {
                //clear and report
                fi.Delete();
                return false;
            }
        }
        private static void ProcessSingleRoi(TifFileInfo fi, ROI roi, FileDirData dirData, double[] background)
        {
            if (!dirData.InputDir.EndsWith("_CompositeRegistred.tif") &&
                               !dirData.InputDir.EndsWith("_Q2.tif") &&
                               !dirData.InputDir.EndsWith("_1.tif"))
                    fi = MultiCropper.ProcessROI(dirData.OutputDir, roi, fi);
            else
            {
                MultiCropper.RecalculateOriginalROI(roi, null, fi);
            }

            if (fi == null) return;

            fi.yAxisTB = 1;
            fi.xAxisTB = 1;
            fi.autoDetectBandC = true;

            //Save the original image
            fi.Dir = dirData.OutputDir;
            if (!dirData.InputDir.EndsWith("_CompositeRegistred.tif") &&
                                !dirData.InputDir.EndsWith("_Q2.tif") &&
                                !dirData.InputDir.EndsWith("_1.tif"))
                MultiCrop.FileEncoder.SaveTif(fi, dirData.OutputDir);

            //Apply expression normalizer and save the image
            fi.Dir = dirData.OutputDir_NormExp;
            for (int c = 0; c < fi.sizeC; c++)
            {
                fi.cValue = c;

                if(c<background.Length)
                    ExpressionNormalizer.Process(background[c], fi);
                else
                    ExpressionNormalizer.Process(background[0], fi);
            }
            MultiCrop.FileEncoder.SaveTif(fi, dirData.OutputDir_NormExp);
            //apply sobel operator
            fi.Dir = dirData.OutputDir_Soble;
            for (int c = 0; c < fi.sizeC; c++)
            {
                fi.cValue = c;
                SobelOperator.applySobel(fi);
            }

            switch (fi.bitsPerPixel)
            {
                case 8:
                    fi.image8bit = fi.image8bitFilter;
                    break;
                case 16:
                    fi.image16bit = fi.image16bitFilter;
                    break;
            }
            MultiCrop.FileEncoder.SaveTif(fi, dirData.OutputDir_Soble);
            //measure results
            CalculateAllRois(fi);
            Cell_Tool_3.ExportTxtResults.ExportAllResults(fi);
            //delete info
            fi.Delete();
        }
        public static void CalculateAllRois(TifFileInfo fi)
        {
            if (fi.roiList != null)
                for (int i = 0; i < fi.sizeC; i++)
                    if (fi.roiList[i] != null)
                        foreach (ROI roi in fi.roiList[i])
                        {
                            Cell_Tool_3.RoiMeasure.Measure(roi, fi, i);
                        }
        }
    }
}
