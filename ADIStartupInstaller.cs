using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
//using Microsoft.Win32;
namespace HPE.ADIUploader.service
{
    [RunInstaller(true)]
    public partial class ADIStartupInstaller : System.Configuration.Install.Installer
    {
        public ADIStartupInstaller()
        {
            InitializeComponent();
        }
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            
            
            // Get the installation folder.
            string installationFolder = "C:\\ADI_Uploader_Service";
            string folderToDelete = "C:\\HPE.AdiUploader.Service";

            // The command to change directory and start your service.
            string command = "@echo off\ncd /d \"" + installationFolder + "\"\nSTART /MIN ADI_Uploader_Service.exe /adipath=C:\\ADI_Archive /r=1 /ms=2400";

            // The location of the bat file.
            string batFilePath = installationFolder + "\\ADI_Uploader_Service.bat";

            // The startup folder.
            //string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string commonStartupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\StartUp");

            // Path of the bat file in the startup folder.
            string startupBatFilePath = Path.Combine(commonStartupFolder, "ADI_Uploader_Service.bat");
            string startupBatFilePathOldService = Path.Combine(commonStartupFolder, "HPE.AdiUploader.Service.bat");
            // Close the application if it is already running.
            foreach (var process in Process.GetProcessesByName("ADI_Uploader_Service"))
            {
                process.Kill();
            }
            // Set the second parameter to false if you only want to delete empty folders.
            if (Directory.Exists(folderToDelete))
            {
                Directory.Delete(folderToDelete, true);
            }
            // Delete the existing bat file in the startup folder.
            if (File.Exists(startupBatFilePath))
            {
                File.Delete(startupBatFilePath);
            }
            // Delete the existing bat file in the startup folder.
            if (File.Exists(startupBatFilePathOldService))
            {
                File.Delete(startupBatFilePath);
            }

            // Write the command to the bat file.
            File.WriteAllText(batFilePath, command);

            // Move the bat file to the startup folder.
            File.Move(batFilePath, startupBatFilePath);

            // Run the batch file.
            //Process.Start(startupBatFilePath);
            
        }

    }

}
