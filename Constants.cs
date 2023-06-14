using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public static class Constants
    {
        public static class CmdArgs
        {
            public static bool isCycleEnded = false;

            public static string BaseFolder = $"C:\\HPE.AdiUploader.Service";

            public static long ChunkedFileSize = 20000000; // default = 20 MB

            public static string Environment = "dev";
            public static string EnvironmentAuthority = "https://login.microsoftonline.com/b7be7686-6f97-4db7-9081-a23cf09a96b5/oauth2/v2.0/token";
            public static string EnvironmentBaseAddress = ""; // Dynamic
            public static string EnvironmentClientId = ""; // Dynamic
            public static string EnvironmentClientSecret = ""; // Dynamic
            public static string EnvironmentSatoriScope = "xavi-dev.azurewebsites.net/.default";
            public static string EnvironmentServerName = "SatoriService";

            public static bool IsLoadingRecursive = false; // == true means loading the nested folders
            public static bool IsStimulationMode = false; // == true means running in stimualtion mode: randomly returns error codes. No file will be uploaded. Successfully.txt also will not be updated
            public static bool IsUploadingToSatori = false; // == true means uploading files to Satori 
            public static bool IsUploadingToXspace = false; // == true means uploading files to Xspace 

            public static string LogFolder = $"{BaseFolder}\\Logs";

            public static long MinimumFileSize = 2500000; // default = 2.5MB

            public static int RetryThreshold = 5; // Max number of retries threshold for XaviUploader.CreateFile

            public static string UploaderVersion = "2022.07.25"; // yyyy.mm.dd.A-Z

            public static string UploadingFolder = $"{BaseFolder}\\AdiFiles";
        }

        public static class EnvironmentCode
        {
            public static string Development = "dev";
            public static string DevelopmentBaseAddress = "https://fdsatoridev.azurefd.net/xavi/api/";
            public static string DevelopmentClientId = "83556649-af97-4f62-8d04-35930d4617d4";
            public static string DevelopmentClientSecret = "p-.qrj8af.CoGG6z_T_OM94Eb_T-OH8Qs7";
            //public static string DevelopmentClientId = "a764baa0-df16-4156-8f53-8905a851bce3";
            //public static string DevelopmentClientSecret = "Bl_8Q~8sAqXRTVoYv8eaWqWh_Wq7O6lc5gkrfc8R";


            public static string Production = "prod";
            public static string ProductionBaseAddress = "https://satori.halliburton.com/xavi/api/";
            public static string ProductionClientId = "6c06b72d-1eab-4614-9402-df23ecf61e0d";
            public static string ProductionClientSecret = "Old7Q~i~II3YFjdRjTNVg4ujbkS2R.29FsMhu";


            public static string UAT = "uat";
            public static string UATBaseAddress = "https://satoritest.halliburton.com/xavi/api/";
            public static string UATClientId = "83556649-af97-4f62-8d04-35930d4617d4";
            public static string UATClientSecret = "p-.qrj8af.CoGG6z_T_OM94Eb_T-OH8Qs7";
            //public static string UATClientId = "47671865-ee5a-4421-82b2-9c898b47aa38";
            //public static string UATClientSecret = "gop.--27fV1r3r7ao-qh4Tas7-mXfCs9~q";
        }

        public static class ErrorMessages
        {
            public static string AuthenticationServiceFailed = "Authentication service failed.";

            public static string AccessTokenEmpty = "Access token is empty";

            public static string SatoriServiceFailed = "Satori service failed.";

            public static string IntegrationServiceFailed = "Integration service failed.";
        }

        public static class FileName
        {
            public static string SatoriSuccessfully = "AdiToSatoriSuccessfully.txt";

            public static string SatoriFailed = "AdiToSatoriFailed.txt";
        }

        public static class FileType
        {
            public static string AdiParquet = "AdiParquet";

            public static string AdiRaw = "AdiRaw";
        }

        public static class FileList
        {
            public static List<string> DonotRetryList = new List<string>();
        }

        public static class Messages
        {
            public static string SwaggerSecurityDescription = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"";
        }

        public static class SectionName
        {
            public static string CronJob = "CronJob";
            public static string NameHttpClientFactories = "NameHttpClientFactories";
            public static string Swagger = "Swagger";
        }

        public static class WorkerLog
        {
            public static string Running = "Worker is running....";
            public static string Executing = "Worker is excuting....";
            public static string Executed = "Worker is excuted....";
        }
    }
}
