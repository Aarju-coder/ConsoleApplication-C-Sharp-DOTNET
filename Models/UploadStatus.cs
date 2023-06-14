using System;

namespace ConsoleApp1.Models
{
    public class UploadStatus
    { 
        public string FileNameWithFullPath { get; set; }

        public string SatoriLocationUrl { get; set; }
        
        public bool IsUploadedFile { get; set; }

        public string UploadingPlatform { get; set; }

        public string UploadingTime { get; set; }
    }
}
