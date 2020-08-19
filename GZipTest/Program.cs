using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Compression;
using GZipTest.Interfaces;
using GZipTest.Classes;
using GZipTest.Enums;

namespace GZipTest
{
    class Program
    {
        static IArchiver archiver;

        static int Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

            if (!ValidCheck(args))
            {
                Console.ReadLine();
                return 1;
            }               

            try
            {
                switch (args[0].ToLower())
                {
                    case "compress":
                        //archiver = new Compressor(args[1], args[2], true);
                        archiver = new GZipper(args[1], args[2], WorkMode.Compress, true);
                        break;
                    case "decompress":
                        //archiver = new Decompressor(args[1], args[2], true);
                        archiver = new GZipper(args[1], args[2], WorkMode.Decompress, true);
                        break;
                    default :
                        return 1;
                }

                if (!SizeCheck(args[1], archiver))
                {
                    Console.ReadLine();
                    return 1;
                }

                archiver.Launch();
                Console.ReadLine();
                return archiver.CallBackResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is occured!\n Method: {0}\n Error description {1}", ex.TargetSite, ex.Message);
                return 1;
            }
        }

        static void CancelKeyPress(object sender, ConsoleCancelEventArgs _args)
        {
            if (_args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("Cancelling...");
                _args.Cancel = true;

                if (archiver != null)
                    archiver.Cancel();
            }
        }

        static bool ValidCheck(string[] args)
        {
            if (args.Count() != 3)
            {
                Console.WriteLine("Uncorrect arguments count");
                return false;
            }

            if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress")
            {
                Console.WriteLine("Uncorrect first argument");
                return false;
            }

            if (args[1].Length == 0 || args[2].Length == 0)
            {
                Console.WriteLine("Uncorrect file name argument");
                return false;
            }

            FileInfo sourceFile = new FileInfo(args[1]);

            if (!sourceFile.Exists)
            {
                Console.WriteLine("Source file not found");
                return false;
            }

            if (sourceFile.Extension != ".gz" && args[0].ToLower() == "decompress")
            {
                Console.WriteLine("Source file not archive");
                return false;
            }

            FileInfo destinationFile;
            if (args[0].ToLower() == "compress")
            {
                destinationFile = new FileInfo($"{args[2]}.gz");
            }
            else
            {
                destinationFile = new FileInfo(args[2]);
            }

            if (destinationFile.Exists)
            {
                Console.WriteLine("Destination file already exist");
                return false;
            }

            return true;
        }

        static bool SizeCheck(string fileName, IArchiver archiver)
        {
            FileInfo sourceFile = new FileInfo(fileName);

            if (sourceFile.Length > archiver.GetLimitSize())
            {
                Console.WriteLine("Source file is too big");
                return false;
            }

            return true;
        }
    }
}
