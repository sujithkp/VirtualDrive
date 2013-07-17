using System;
using SolFSDrv;
using System.IO;
using System.Configuration;

namespace SolFSDriveMountEx
{
    class Program
    {
        private static string _strDiskDriverFileName = @"C:\Program Files\EldoS\SolFS.OS\Drivers\debug\Win32\soldisk.sys";
        private static string _strFileSystemDriverName = @"C:\Program Files\EldoS\SolFS.OS\Drivers\debug\Win32\solfs.sys";

        private enum InstallStatus { Installed, RebootRequired, UnInstalled };

        static string getNextDriveLetter()
        {
            for (char c = 'P'; c >= 'A'; c--)
                if (!Directory.Exists(c + ":" + Path.DirectorySeparatorChar))
                    return c.ToString();

            throw new Exception("All Drive Letters are in Use !");
            return string.Empty;
        }

        static InstallStatus InstallDriver(String pDiskDriverFileName, string pFileSystemDriverName)
        {
            bool blnRebootNeeded = false;
            if (!(File.Exists(_strDiskDriverFileName) && File.Exists(_strFileSystemDriverName))) throw new Exception("Drivers Not Found.");
            SolFSStorage.InstallDrivers(pDiskDriverFileName, pFileSystemDriverName, out blnRebootNeeded, "NomaDesk");
            if (blnRebootNeeded) return InstallStatus.RebootRequired;
            return InstallStatus.Installed;
        }

        static bool IsDriverInitialized()
        {
            uint fileVersionLow = 0;
            uint fileVersionHigh = 0;
            bool blnInstalled = false;
            DriverStatus objDriverStatus;

            SolFSDrv.SolFSStorage.GetDriversStatus(true, out blnInstalled, out fileVersionHigh,
                out fileVersionLow, out objDriverStatus, "VirtualDrive");

            return blnInstalled;
        }

        static void Main(string[] args)
        {
            var strRegKey = ConfigurationManager.AppSettings["RegistrationKey"];
            SolFSStorage.SetRegistrationKey(strRegKey);

            var mountFileName = ConfigurationManager.AppSettings["MountFileName"];
            mountFileName = (mountFileName.Length == 0) ? "SolFS001.img" : mountFileName;

            var strNextDriveLetter = getNextDriveLetter();
            if (strNextDriveLetter.Length == 0)
                throw new Exception("No Drive letter Available !");

            if (!IsDriverInitialized())
            {
                Console.WriteLine("Driver Not Installed ! \nInstalling Driver..");
                if (InstallDriver(_strDiskDriverFileName, _strFileSystemDriverName) == InstallStatus.RebootRequired)
                {
                    Console.WriteLine("Driver Installed. \nReboot Required. \nExiting..");
                    Environment.Exit(0);
                }
            }

            var storage = new SolFSStorage()
            {
                FileName = (args.Length == 0) ? mountFileName : args[0],
                DestroyOnProcessTerminated = true,
                PageSize = 32768,
                UseTransactions = true,
                UseAccessTime = true,
            };

            try
            {
                storage.Open((File.Exists(storage.FileName) ? StorageOpenMode.somOpenExisting : StorageOpenMode.somCreateNew));
                storage.AddMountingPoint(strNextDriveLetter + ":");
                storage.Logo = "SkyDrive";
                Console.WriteLine("Mounted " + storage.FileName + " to " + strNextDriveLetter + ":");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                storage.Close();
            }

            Console.WriteLine("Press Any Key to Exit.");
            Console.ReadKey();
            storage.Dispose();
        }
    }
}
