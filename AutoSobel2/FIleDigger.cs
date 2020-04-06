/*AutoSobel2 - software for image and data analysis
 Copyright(C) 2018  Georgi Danovski

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.If not, see<http://www.gnu.org/licenses/>.*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AutoSobel2
{
    class FileDigger
    {
        /// <summary>
        /// Returns dictionary that contains all directories for input and output files
        /// </summary>
        /// <param name="dir">Input directory</param>
        /// <param name="output">Output directory</param>
        /// <returns></returns>
        public static Dictionary<string, FileDirData> StartDigging(string dir, string output)
        {
            Dictionary<string, FileDirData> LinkedFiles = new Dictionary<string, FileDirData>();

            List<string> files = DigForFiles(dir);
            List<string> normFiles = NormalizeFileNames(files).ToList();
            FileDirData curData = null;
            string name, normName;
            int ind = 0;
            //foreach (var name in files)
            for (int i = 0; i < files.Count; i++)
            {
                name = files[i];
                normName = normFiles[i];

                if (!name.EndsWith(".RoiSet"))
                {
                    if (name.EndsWith("_Q2.tif"))
                    {
                        curData = new FileDirData(name);
                    }
                    else if (normFiles.Contains(normName.Replace(".tif", ".RoiSet")))
                    {
                        ind = normFiles.IndexOf(normName.Replace(".tif", ".RoiSet"));

                        curData = new FileDirData(name, files[ind]);
                    }
                    else if (normFiles.Contains(normName.Replace(".tif", "_G.RoiSet")))
                    {
                        ind = normFiles.IndexOf(normName.Replace(".tif", "_G.RoiSet"));

                        curData = new FileDirData(name, files[ind]);
                    }
                    else if (normFiles.Contains(normName.Replace(".tif", "_R.RoiSet")))
                    {
                        // ind = normFiles.IndexOf(normName.Replace(".tif", "_R.RoiSet"));

                        // curData = new FileDirData(name, files[ind]);
                        curData = null;
                    }
                    else
                    {
                        curData = null;
                    }

                    if (curData != null)
                    {
                        curData.ChangeOutputPath(dir, output);
                        LinkedFiles.Add(name, curData);
                    }
                }
            }

            return LinkedFiles;
        }
        private static string[] NormalizeFileNames(List<string> input)
        {
            string[] output = new string[input.Count];

            for (int i = 0; i < input.Count; i++)
                output[i] = input[i].Replace("_Merged", "").Replace("_Processed", "");

            return output;
        }
        private static List<string> DigForFiles(string dir)
        {
            string[] Extentions = new string[] {
                "_Q2.tif",// tuka nqma roi set - cheti metadatata
                "_Merged.tif",
                "_CompositeRegistred.tif",
                "_1.tif",
                ".RoiSet"};

            List<string> result = new List<string>();
            List<string> dirs = new List<string>() { dir };
            List<string> temp;

            while (dirs.Count > 0)
            {
                temp = new List<string>();

                foreach (string str in dirs)
                {
                    foreach (string name in GetFiles(str,Extentions))
                        result.Add(name);

                    foreach (string name in GetDirectories(str))
                        temp.Add(name);
                }

                dirs = temp;
            }

            dirs = null;
            temp = null;

            return result;
        }
        private static List<string> GetFiles(string dir, string[] Extentions)
        {
            List<string> result = new List<string>();

            DirectoryInfo di = new DirectoryInfo(dir);

            foreach (var fi in di.GetFiles())
                foreach (var ext in Extentions)
                    if (fi.FullName.EndsWith(ext))
                    {
                        result.Add(fi.FullName);
                        break;
                    }

            di = null;

            return result;
        }
        private static List<string> GetDirectories(string dir)
        {
            List<string> result = new List<string>();

            DirectoryInfo di = new DirectoryInfo(dir);

            foreach (var fi in di.GetDirectories())
                result.Add(fi.FullName);

            di = null;

            return result;
        }
    }
}
