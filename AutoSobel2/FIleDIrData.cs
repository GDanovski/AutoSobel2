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

namespace AutoSobel2
{
    class FileDirData
    {
        private string _InputDir;//.tif
        private string _InputRoi;//.RoiSet
        private string _OutputDir;//.tif
        private string _OutputDir_NormExp;//_NormExp.tif
        private string _OutputDir_Sobel;//_Sobel2.tif
        private string _OutputDir_Results;//_Results_So2.txt
        private string _OutputDir_CTResults;//_So2.txt

        public FileDirData(string InputDir, string InputRoi = "")
        {
            InputDir = RemoveExtention(InputDir);
            InputRoi = RemoveExtention(InputRoi);

            this._InputDir = InputDir;
            this._InputRoi = InputRoi;
            this._OutputDir = InputDir;
            this._OutputDir_NormExp = InputDir;
            this._OutputDir_Sobel = InputDir;
            this._OutputDir_Results = InputDir;
            this._OutputDir_CTResults = InputDir;
        }
        /// <summary>
        /// Add as extention string to the end of output directories
        /// </summary>
        /// <param name="index"></param>
        public void AddCellIndex(string index)
        {
            this._OutputDir += "_" + index;
            this._OutputDir_NormExp += "_" + index;
            this._OutputDir_Sobel += "_" + index;
            this._OutputDir_Results += "_" + index;
            this._OutputDir_CTResults += "_" + index;
        }
        /// <summary>
        /// Replace the old parent directory with new directory
        /// </summary>
        /// <param name="oldDir">Old parent directory</param>
        /// <param name="newDir">New parent directory</param>
        public void ChangeOutputPath(string oldDir, string newDir)
        {
            this._OutputDir = _OutputDir.Replace(oldDir, newDir);
            this._OutputDir_NormExp = _OutputDir_NormExp.Replace(oldDir, newDir);
            this._OutputDir_Sobel = _OutputDir_Sobel.Replace(oldDir, newDir);
            this._OutputDir_Results = _OutputDir_Results.Replace(oldDir, newDir);
            this._OutputDir_CTResults = _OutputDir_CTResults.Replace(oldDir, newDir);
        }
        /// <summary>
        /// Directory of the input image
        /// </summary>
        public string InputDir
        {
            get { return AddExtention(this._InputDir, ".tif"); }
            set { this._InputDir = RemoveExtention(value); }
        }
        /// <summary>
        /// DIrectory of the input ROI set. If equals "" - load the ROI set from the image
        /// </summary>
        public string InputRoi
        {
            get { return AddExtention(this._InputRoi, ".RoiSet"); }
            set { this._InputRoi = RemoveExtention(value); }
        }
        /// <summary>
        /// Directory for the cropped image (RAW)
        /// </summary>
        public string OutputDir
        {
            get { return AddExtention(this._OutputDir, ".tif"); }
            set { this._OutputDir = RemoveExtention(value); }
        }
        /// <summary>
        /// Directory for saving the result filtered image (only with normalized expression)
        /// </summary>
        public string OutputDir_NormExp
        {
            get { return AddExtention(this._OutputDir_NormExp, "_NormExp.tif"); }
            set { this._OutputDir_NormExp = RemoveExtention(value); }
        }
        /// <summary>
        /// Directory for saving the result filtered image after sobel operator
        /// </summary>
        public string OutputDir_Soble
        {
            get { return AddExtention(this._OutputDir_Sobel, "_Sobel2.tif"); }
            set { this._OutputDir_Sobel = RemoveExtention(value); }
        }
        /// <summary>
        /// Directory for saving the results
        /// </summary>
        public string OutputDir_Results
        {
            get { return AddExtention(this._OutputDir_Results, "_Results_So2.txt"); }
            set { this._OutputDir_Results = RemoveExtention(value); }
        }
        /// <summary>
        /// DIrectory for saving the CellTool results
        /// </summary>
        public string OutputDir_CTResults
        {
            get { return AddExtention(this._OutputDir_CTResults, "_So2.txt"); }
            set { this._OutputDir_CTResults = RemoveExtention(value); }
        }
        /// <summary>
        /// Removes the file extention
        /// </summary>
        /// <param name="dir"></param>
        /// <returns> File directory without extention</returns>
        public string RemoveExtention(string dir)
        {
            if (dir.Contains("."))
                return dir.Substring(0, dir.LastIndexOf("."));
            else
                return dir;
        }
        /// <summary>
        /// Add extention to the directory
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="ext"></param>
        /// <returns>File directory with extention</returns>
        public string AddExtention(string dir,string ext)
        {
            return dir + ext;
        }
        /// <summary>
        /// Copy the current file directory data
        /// </summary>
        /// <returns>New File directory data</returns>
        public FileDirData Duplicate()
        {
            FileDirData output = new FileDirData(InputDir, InputRoi);
            output.OutputDir = this._OutputDir;
            output.OutputDir_CTResults = this._OutputDir_CTResults;
            output.OutputDir_NormExp = this._OutputDir_NormExp;
            output.OutputDir_Results = this._OutputDir_Results;
            output.OutputDir_CTResults = this._OutputDir_CTResults;
            output.OutputDir_Soble = this._OutputDir_Sobel;
            return output;
        }
        /// <summary>
        /// Formats the name to cell[clip][cell]
        /// </summary>
        public void ExtractClipNumber()
        {
            string parentDir = this._OutputDir.Substring(0, this._OutputDir.LastIndexOf("\\"));
            string name = this._OutputDir.Substring(parentDir.Length + 1, 1);
            int i = 0;
            if(int.TryParse(name,out i))
            {
                name = parentDir + "\\cell" + name;

                this._OutputDir = name;
                this._OutputDir_CTResults = name;
                this._OutputDir_NormExp = name;
                this._OutputDir_Results = name;
                this._OutputDir_CTResults = name;
                this._OutputDir_Sobel = name;
            }

        }
    }
}
