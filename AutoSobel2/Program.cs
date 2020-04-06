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
using System.Threading;

namespace AutoSobel2
{
    class Program
    {
        static void Main(string[] args)
        {
            //intro words
            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.WriteLine(@" AutoSobel2 - software for image and data analysis.
 Copyright (C) 2019  Georgi Danovski.

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.If not, see<http://www.gnu.org/licenses/>.
-------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" >>> Hello!");
            Console.WriteLine(" >>> Press any key to continue...");
            Console.WriteLine("");
            Console.ReadKey();

            bool repeat = true;

            while (repeat)
            {
                Body();

                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine(" >>> Type Y to exit the program or press any kay to continue...");
                
                if (Console.ReadKey().Key.ToString().ToUpper() == "y".ToUpper()) repeat = false;
            }
            
        }
        private static void Body()
        {
            Console.ForegroundColor = ConsoleColor.White;
           
            string input = "", output = "", str = "";
            double[] background = new double[2];
            int count, total;
            List<string> errorList;
            Dictionary<string, FileDirData> fileDirs;

            Console.WriteLine("");
            //input directory
            do
            {
                Console.WriteLine(" >>> Input directory:");
                Console.ForegroundColor = ConsoleColor.Green;
                input = Console.ReadLine();

                if (!Directory.Exists(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" >>> Incorrect directory!");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }
            while (!Directory.Exists(input));
            //output directory
            do
            {
                Console.WriteLine(" >>> Output directory:");
                Console.ForegroundColor = ConsoleColor.Green;
                output = Console.ReadLine();

                if (!Directory.Exists(output))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" >>> Incorrect directory!");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }
            while (!Directory.Exists(output));
            output += @"\Results";

            if (!Directory.Exists(output)) Directory.CreateDirectory(output);

            //background value
            do
            {
                Console.WriteLine(" >>> Background value (Chanel 1):");
                Console.ForegroundColor = ConsoleColor.Green;
                str = Console.ReadLine();

                if (!double.TryParse(str, out background[0]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" >>> Incorrect background value!");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }
            while (!double.TryParse(str, out background[0]));

            do
            {
                Console.WriteLine(" >>> Background value (Chanel 2):");
                Console.ForegroundColor = ConsoleColor.Green;
                str = Console.ReadLine();

                if (!double.TryParse(str, out background[1]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" >>> Incorrect background value!");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }
            while (!double.TryParse(str, out background[1]));

            Console.ForegroundColor = ConsoleColor.DarkGray;
            //prepare FileDir data
            Console.WriteLine(" >>> Digging for files...");
            fileDirs = FileDigger.StartDigging(input, output);
            if(fileDirs.Count == 0)
            {
                //exit
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No files!");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("-------------------------------------------------------------------");
                return;
            }
            //loop the files
            Console.WriteLine(" >>> Processing the files...");
            count = 1;
            total = fileDirs.Count();

            errorList = new List<string>();

            foreach (var kvp in fileDirs)
            {
                //report progress
                ProgressBar(count++, total);
                //perform analysis
                if (!ImageAnalyser.Analyse(kvp.Value,background))
                    errorList.Add("\n >>> Error: " + kvp.Key);

            }

            //report errors
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var s in errorList)
                Console.Write(s);

            Console.ForegroundColor = ConsoleColor.White;
            //clean
            fileDirs = null;
            errorList = null;
            //exit
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n >>> Done!\n-------------------------------------------------------------------");
        }
        private static void ProgressBar(int progress, int tot)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / tot;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + tot.ToString() + "    "); //blanks at the end remove any excess
        }
    }
   
}
