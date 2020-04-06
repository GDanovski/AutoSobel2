using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellToolDK;
using System.IO;


namespace Cell_Tool_3
{
    class ExportTxtResults
    {
        public static void ExportAllResults(TifFileInfo fi)
        {
            string dir = fi.Dir.Replace(".tif", "");

            //background worker

            //var bgw = new BackgroundWorker();
            //bgw.WorkerReportsProgress = true;
            fi.available = false;
            //Add event for projection here
            //bgw.DoWork += new DoWorkEventHandler(delegate (Object o, DoWorkEventArgs a)
            {
                for (int c = 0; c < fi.sizeC; c++)
                    if (fi.roiList[c] != null && fi.roiList[c].Count != 0)
                    {
                        string dir1 = dir + "_Ch" + c + "_" +
                        fi.LutList[c].ToString().Replace("Color [", "").Replace("]", "") +
                        ".txt";
                        string dir2 = dir + "_Ch" + c + "_" +
                        fi.LutList[c].ToString().Replace("Color [", "").Replace("]", "") +
                        "_Results.txt";
                        //calculate the size of the result row
                        int resultSize = 0;
                        foreach (ROI roi in fi.roiList[c])
                            if (roi.Checked == true)
                            {
                                if (roi.Results == null) RoiMeasure.Measure(roi, fi, c);
                                if (roi.Shape == 0 || roi.Shape == 1)
                                    resultSize += roi.Results[c].Length;
                                else if (roi.Shape == 2 || roi.Shape == 3 || roi.Shape == 4 || roi.Shape == 5)
                                    resultSize += 4 + roi.Stack * 4;

                            }
                        //chart result
                        string val = GetResults(fi, c);
                        if (val != "")
                        {
                            try
                            {
                                File.WriteAllText(dir1, val);
                            }
                            catch
                            {
                                Console.Write("File is used by other program!");
                                //((BackgroundWorker)o).ReportProgress(1);
                            }
                        }
                        //standart results
                        if (resultSize == 0) continue;

                        {
                            //create result matrix
                            double[] result;

                            int t = 1;
                            int z = 1;
                            int position;
                            string str;

                            double time = 0;
                            int timeIndex = 0;
                            double timeT = fi.TimeSteps[timeIndex];
                            try
                            {
                                if (File.Exists(dir2))
                                    File.Delete(dir2);
                            }
                            catch
                            {
                                //((BackgroundWorker)o).ReportProgress(1);
                                Console.Write("File is used by other program!");
                                continue;
                            }

                            using (StreamWriter write = new StreamWriter(dir2))
                            {
                                //titles part
                                List<string> titles = new List<string>();
                                titles.Add("ImageN");
                                if (fi.sizeT > 1) titles.Add("T");
                                if (fi.sizeT > 1) titles.Add("T(sec.)");
                                if (fi.sizeZ > 1) titles.Add("Z");

                                int roiN = 1;
                                foreach (ROI roi in fi.roiList[c])
                                {
                                    if (roi.Checked == true && roi.Results[c] != null)
                                    {
                                        string com = "";
                                        if (roi.Comment != "") com = ": " + roi.Comment;

                                        titles.Add("Area" + roiN.ToString() + com);
                                        titles.Add("Mean" + roiN.ToString() + com);
                                        titles.Add("Min" + roiN.ToString() + com);
                                        titles.Add("Max" + roiN.ToString() + com);
                                        if (roi.Stack > 0)
                                            if (roi.Shape == 0 || roi.Shape == 1)
                                                for (int n = 1; n <= roi.Stack; n++)
                                                {
                                                    titles.Add("Area" + roiN.ToString() + "." + n.ToString() + ".LeftUp" + com);
                                                    titles.Add("Mean" + roiN.ToString() + "." + n.ToString() + ".LeftUp" + com);
                                                    titles.Add("Min" + roiN.ToString() + "." + n.ToString() + ".LeftUp" + com);
                                                    titles.Add("Max" + roiN.ToString() + "." + n.ToString() + ".LeftUp" + com);

                                                    titles.Add("Area" + roiN.ToString() + "." + n.ToString() + ".RightUp" + com);
                                                    titles.Add("Mean" + roiN.ToString() + "." + n.ToString() + ".RightUp" + com);
                                                    titles.Add("Min" + roiN.ToString() + "." + n.ToString() + ".RightUp" + com);
                                                    titles.Add("Max" + roiN.ToString() + "." + n.ToString() + ".RightUp" + com);

                                                    titles.Add("Area" + roiN.ToString() + "." + n.ToString() + ".LeftDown" + com);
                                                    titles.Add("Mean" + roiN.ToString() + "." + n.ToString() + ".LeftDown" + com);
                                                    titles.Add("Min" + roiN.ToString() + "." + n.ToString() + ".LeftDown" + com);
                                                    titles.Add("Max" + roiN.ToString() + "." + n.ToString() + ".LeftDown" + com);

                                                    titles.Add("Area" + roiN.ToString() + "." + n.ToString() + ".RightDown" + com);
                                                    titles.Add("Mean" + roiN.ToString() + "." + n.ToString() + ".RightDown" + com);
                                                    titles.Add("Min" + roiN.ToString() + "." + n.ToString() + ".RightDown" + com);
                                                    titles.Add("Max" + roiN.ToString() + "." + n.ToString() + ".RightDown" + com);

                                                }
                                            else if (roi.Shape == 2 || roi.Shape == 3 || roi.Shape == 4 || roi.Shape == 5)
                                                for (int n = 1; n <= roi.Stack; n++)
                                                {
                                                    titles.Add("Area" + roiN.ToString() + "." + n.ToString() + com);
                                                    titles.Add("Mean" + roiN.ToString() + "." + n.ToString() + com);
                                                    titles.Add("Min" + roiN.ToString() + "." + n.ToString() + com);
                                                    titles.Add("Max" + roiN.ToString() + "." + n.ToString() + com);
                                                }
                                    }
                                    roiN++;
                                }
                                write.WriteLine(string.Join("\t", titles));
                                //calculations
                                for (int i = c; i < fi.imageCount; i += fi.sizeC)
                                {
                                    //extract row from rois
                                    position = 0;
                                    result = new double[resultSize];
                                    foreach (ROI roi in fi.roiList[c])
                                    {
                                        if (roi.Checked == true)
                                        {
                                            if (roi.Shape == 0 || roi.Shape == 1)
                                            {
                                                if (roi.Results[i] != null
                                            && roi.FromT <= t && roi.ToT >= t
                                            && roi.FromZ <= z && roi.ToZ >= z)
                                                {
                                                    Array.Copy(roi.Results[i], 0, result, position, roi.Results[i].Length);
                                                }

                                                position += roi.Results[c].Length;
                                            }
                                            else if (roi.Shape == 2 || roi.Shape == 3 || roi.Shape == 4 || roi.Shape == 5)
                                            {
                                                if (roi.Results[i] != null
                                            && roi.FromT <= t && roi.ToT >= t
                                            && roi.FromZ <= z && roi.ToZ >= z)
                                                {
                                                    //main roi
                                                    Array.Copy(roi.Results[i], 0, result, position, 4);
                                                    position += 4;
                                                    //layers
                                                    for (int p = 4; p < roi.Results[i].Length; p += 16)
                                                    {
                                                        Array.Copy(roi.Results[i], p, result, position, 4);
                                                        position += 4;
                                                    }
                                                }
                                                else
                                                {
                                                    position += 4;
                                                    //layers
                                                    for (int p = 4; p < roi.Results[i].Length; p += 16)
                                                    {
                                                        position += 4;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //write the line
                                    if (CheckArrayForValues(result))
                                    {
                                        str = string.Join("\t", result);

                                        if (fi.sizeZ > 1) str = z.ToString() + "\t" + str;
                                        if (fi.sizeT > 1)
                                        {
                                            str = t.ToString() + "\t" + time.ToString() + "\t" + str;
                                        }
                                        str = i.ToString() + "\t" + str;
                                        write.WriteLine(str);
                                    }
                                    //recalculate z and t

                                    z += 1;
                                    if (z > fi.sizeZ)
                                    {
                                        z = 1;
                                        t += 1;

                                        if (t > fi.sizeT)
                                        {
                                            t = 1;
                                        }

                                        if (t <= timeT)
                                        {
                                            time += fi.TimeSteps[timeIndex + 1];
                                        }
                                        else
                                        {
                                            timeIndex += 2;

                                            if (timeIndex < fi.TimeSteps.Count)
                                                timeT += fi.TimeSteps[timeIndex];
                                            else
                                            {
                                                timeIndex -= 2;
                                                timeT += fi.imageCount;
                                            }

                                            time += fi.TimeSteps[timeIndex + 1];
                                        }

                                    }
                                }
                            }
                        }
                    }

                //((BackgroundWorker)o).ReportProgress(0);
            }//);

            // bgw.ProgressChanged += new ProgressChangedEventHandler(delegate (Object o, ProgressChangedEventArgs a)
            {
                //if (a.ProgressPercentage == 0)
                {
                    fi.available = true;
                }
                //else
                //{
                //    MessageBox.Show("File is used by other program!");
                // }
            }//);
            //Start background worker
            //IA.FileBrowser.StatusLabel.Text = "Saving results...";
            //start bgw
            //bgw.RunWorkerAsync();

            fi.available = true;
        }
        private static bool CheckArrayForValues(double[] input)
        {
            foreach (double val in input)
                if (val != 0)
                    return true;

            return false;
        }
        public static string GetResults(TifFileInfo fi, int c)
        {
            string val = "";
            List<double[]> data = new List<double[]>();
            #region Calculate Original Data Set

            int ind, row, fromT, toT, fromZ, toZ,
                t, z, position, stack, boolStart;
            double[] mainRoi;
            List<string> RoiNames = new List<string>() { "Time(s)" };
            List<string> Comments = new List<string>() { "Comments" };

            ROI roi;

            List<int> factorsT = new List<int>();
            List<int> factorsZ = new List<int>();
            
            if (data == null) data = new List<double[]>();

            if (fi.roiList[c] != null)
            {
                data.Clear();

                for (ind = 0; ind < fi.roiList[c].Count; ind++)
                {
                    roi = fi.roiList[c][ind];
                    if (roi.Results == null || roi.Checked == false) continue;

                    fromT = roi.FromT;
                    toT = roi.ToT;
                    fromZ = roi.FromZ;
                    toZ = roi.ToZ;
                    //main roi part

                    t = 1;
                    z = 1;

                    if (true)
                    {
                        mainRoi = new double[roi.Results.Length];

                        for (row = c; row < roi.Results.Length; row += fi.sizeC)
                        {
                            if (roi.Results[row] != null &&
                                t >= fromT && t <= toT && z >= fromZ && z <= toZ)
                                if (fi.yAxisTB == 4)
                                    mainRoi[row] = roi.Results[row][0] * roi.Results[row][1];
                                 else
                                    mainRoi[row] = roi.Results[row][fi.yAxisTB];
                            //apply change t and z

                            z++;
                            if (z > fi.sizeZ)
                            {
                                z = 1;
                                t++;
                                if (t > fi.sizeT) t = 1;
                            }
                        }
                        factorsT.Add(toT - fromT + 1);
                        factorsZ.Add(toZ - fromZ + 1);
                        data.Add(mainRoi);
                        RoiNames.Add(" Mean_ROI" + (ind + 1).ToString());
                        Comments.Add(roi.Comment);
                    }

                    //layers
                    if (roi.Stack == 0) continue;

                    if (fi.yAxisTB > 4) continue;
                    position = 4;

                    for (stack = 0; stack < roi.Stack; stack++)
                    {
                        t = 1;
                        z = 1;

                        mainRoi = new double[roi.Results.Length];

                        int factor = 0;
                        if (roi.Shape == 0 || roi.Shape == 1)
                        {
                            for (boolStart = 1 + stack * 4; boolStart < 5 + stack * 4; boolStart++, position += 4)
                                if (roi.ChartUseIndex[boolStart] == true)
                                {
                                    for (row = c; row < roi.Results.Length; row += fi.sizeC)
                                    {
                                        if (roi.Results[row] != null &&
                                            t >= fromT && t <= toT && z >= fromZ && z <= toZ)
                                            if (fi.yAxisTB == 4)
                                                mainRoi[row] += roi.Results[row][position] * roi.Results[row][position + 1];
                                            else if (fi.yAxisTB < 2)
                                                mainRoi[row] += roi.Results[row][position + fi.yAxisTB];
                                            else if (fi.yAxisTB == 2 && (mainRoi[row] == 0 || mainRoi[row] > roi.Results[row][position + fi.yAxisTB]))
                                                mainRoi[row] = roi.Results[row][position + fi.yAxisTB];
                                            else if (fi.yAxisTB == 3 && mainRoi[row] < roi.Results[row][position + fi.yAxisTB])
                                                mainRoi[row] = roi.Results[row][position + fi.yAxisTB];
                                        //apply change t and z

                                        z++;
                                        if (z > fi.sizeZ)
                                        {
                                            z = 1;
                                            t++;
                                            if (t > fi.sizeT) t = 1;
                                        }
                                    }

                                    factor++;
                                }
                        }
                        else
                        {
                            boolStart = 1 + stack * 4;

                            if (roi.ChartUseIndex[boolStart] == true)
                            {
                                for (row = c; row < roi.Results.Length; row += fi.sizeC)
                                {
                                    if (roi.Results[row] != null &&
                                        t >= fromT && t <= toT && z >= fromZ && z <= toZ)
                                        if (fi.yAxisTB == 4)
                                            mainRoi[row] = roi.Results[row][position] * roi.Results[row][position + 1];
                                        else if (fi.yAxisTB < 2)
                                            mainRoi[row] = roi.Results[row][position + fi.yAxisTB];
                                        else if (fi.yAxisTB == 2)
                                            mainRoi[row] = roi.Results[row][position + fi.yAxisTB];
                                        else if (fi.yAxisTB == 3)
                                            mainRoi[row] = roi.Results[row][position + fi.yAxisTB];
                                    //apply change t and z

                                    z++;
                                    if (z > fi.sizeZ)
                                    {
                                        z = 1;
                                        t++;
                                        if (t > fi.sizeT) t = 1;
                                    }
                                }

                                factor++;
                            }

                            position += 16;
                        }

                        if (fi.yAxisTB == 1)
                            for (int i = 0; i < mainRoi.Length; i++)
                                if (mainRoi[i] != 0) mainRoi[i] /= factor;

                        factorsT.Add(toT - fromT + 1);
                        factorsZ.Add(toZ - fromZ + 1);
                        data.Add(mainRoi);
                        RoiNames.Add(" Mean_ROI" + (ind + 1).ToString() + ".Layer" + (stack + 1).ToString());
                        Comments.Add(roi.Comment);
                    }
                }


                var XaxisData = new double[fi.imageCount];
                t = 1;
                z = 1;

                double time = 0;
                int timeIndex = 0;
                double timeT = fi.TimeSteps[timeIndex];

                for (row = c; row < fi.imageCount; row += fi.sizeC)
                {
                    switch (fi.xAxisTB)
                    {
                        case 0:
                            //T slice
                            XaxisData[row] = t;
                            break;
                        case 1:
                            //T sec
                            XaxisData[row] = time;
                            break;
                        case 2:
                            //Z slice
                            XaxisData[row] = z;
                            break;
                        case 3:
                            //T sec
                            XaxisData[row] = time / 60;
                            break;
                        case 4:
                            //T sec
                            XaxisData[row] = time / 3600;
                            break;
                    }
                    //apply change t and z

                    z++;
                    if (z > fi.sizeZ)
                    {
                        z = 1;
                        t++;
                        if (t > fi.sizeT) t = 1;

                        if (t <= timeT)
                        {
                            time += fi.TimeSteps[timeIndex + 1];
                        }
                        else
                        {
                            timeIndex += 2;

                            if (timeIndex < fi.TimeSteps.Count)
                                timeT += fi.TimeSteps[timeIndex];
                            else
                            {
                                timeIndex -= 2;
                                timeT += fi.imageCount;
                            }

                            time += fi.TimeSteps[timeIndex + 1];
                        }
                    }
                }
                #endregion Calculate Original Data Set

                #region recalculate original data set
                if (fi.xAxisTB < 2 || fi.xAxisTB == 3 || fi.xAxisTB == 4)
                {
                    double[] res;
                    int counter;

                    for (ind = 0; ind < data.Count; ind++)
                    {
                        res = new double[fi.sizeT];
                        counter = 0;

                        t = 1;
                        z = 1;

                        for (row = c; row < fi.imageCount; row += fi.sizeC)
                        {
                            z++;
                            if (fi.yAxisTB < 2)
                                res[counter] += data[ind][row];
                            else if (fi.yAxisTB == 2 && (res[counter] > data[ind][row] || res[counter] == 0))
                                res[counter] = data[ind][row];
                            else if (fi.yAxisTB == 3 && res[counter] < data[ind][row])
                                res[counter] = data[ind][row];
                            else
                                res[counter] += data[ind][row];

                            if (z > fi.sizeZ)
                            {
                                z = 1;
                                t++;
                                if (fi.yAxisTB == 1)
                                    res[counter] /= factorsZ[ind];
                                counter++;
                            }
                        }

                        data[ind] = res;
                    }
                    //x axis
                    res = new double[fi.sizeT];
                    counter = 0;
                    t = 1;
                    z = 1;

                    for (row = c; row < fi.imageCount; row += fi.sizeC)
                    {
                        z++;
                        if (z > fi.sizeZ)
                        {
                            z = 1;
                            t++;
                            res[counter] = XaxisData[row];
                            counter++;
                        }
                    }

                    XaxisData = res;
                }
                else if (fi.xAxisTB == 2)
                {
                    double[] res;
                    int counter;

                    for (ind = 0; ind < data.Count; ind++)
                    {
                        res = new double[fi.sizeZ];
                        counter = 0;

                        t = 1;
                        z = 1;

                        for (row = c; row < fi.imageCount; row += fi.sizeC)
                        {
                            z++;

                            if (fi.yAxisTB < 2)
                                res[counter] += data[ind][row];
                            else if (fi.yAxisTB == 2 && (res[counter] > data[ind][row] || res[counter] == 0))
                                res[counter] = data[ind][row];
                            else if (fi.yAxisTB == 3 && res[counter] < data[ind][row])
                                res[counter] = data[ind][row];
                            else
                                res[counter] += data[ind][row];

                            counter++;

                            if (z > fi.sizeZ)
                            {
                                z = 1;
                                counter = 0;
                                t++;
                            }
                        }
                        //popravi tuk s faktor list w gornata chast
                        if (fi.yAxisTB == 1)
                            for (int i = 0; i < res.Length; i++)
                                res[i] /= factorsT[ind];

                        data[ind] = res;
                    }

                    //x axis
                    res = new double[fi.sizeZ];
                    counter = 0;
                    t = 1;
                    z = 1;

                    for (row = c; row < fi.imageCount; row += fi.sizeC)
                    {
                        res[counter] = XaxisData[row];
                        counter++;

                        z++;
                        if (z > fi.sizeZ) break;
                    }

                    XaxisData = res;
                }
                #endregion recalculate original data set
                //System.IO.File.WriteAllText(fi.Dir.Replace(".tif", "_c" + c + ".text"),MeargeResult());
                #region Prepare string
                string[] resList = new string[XaxisData.Length + 3];
                //system description row
                string[] temp = new string[RoiNames.Count];
                temp[0] = "CTResults:  Mean";
                temp[1] = fi.Dir;
                resList[0] = string.Join("\t", temp);
                temp = null;
                //comments
                resList[1] = string.Join("\t", Comments);
                Comments = null;
                //titles
                resList[2] = string.Join("\t", RoiNames);
                RoiNames = null;
                for (int i = 0; i < XaxisData.Length; i++)
                {
                    val = XaxisData[i].ToString();
                    foreach (double[] dList in data)
                    {
                        val += "\t" + dList[i];
                    }
                    resList[i + 3] = val;
                }
                XaxisData = null;
                data = null;

                val = string.Join("\n", resList);
                resList = null;
                #endregion Prepare string
            }

            return val;
        }
    }
}
