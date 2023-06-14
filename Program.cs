using AdiParser.Main;
using ConsoleApp1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using OfficeOpenXml;
using System.IO.Packaging;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Win32;


namespace ConsoleApp1{
    class Program
    {
        private static string filePath = "";
        public static Logger logger;
        public static string buildFolderPath = "";
        public static string folderPath = "";
        public static string logfolderpath = "";
        public static string rec = "1";
        public static long minADIFileSie = 2400;
        public static IPAddress mainIp;
        //public static string folderPath = @"C:\test";
        //public static string logfolderpath = @"C:\logs\";


        static async Task Main(string[] args)
        {
            
            String hostname = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostname);
            Console.WriteLine("Hostname : " + hostname);
            mainIp = null;
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    mainIp = address;
                    break;
                }
            }
           // Console.WriteLine("Argument--" + args);
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("Argument i--" + args[i]);

                if (args[i].ToLower().StartsWith("/adipath="))
                {
                        Console.WriteLine("Argument substring adipath --" + args[i].Substring("/adipath=".Length));
                        folderPath = args[i].Substring("/adipath=".Length);
                }
                else if (args[i].ToLower().StartsWith("/r="))
                {
                    Console.WriteLine("Argument substring reccursion --" + args[i].Substring("/r=".Length));
                    rec = args[i].Substring("/r=".Length);
                }
                else if (args[i].ToLower().StartsWith("/ms="))
                {
                    Console.WriteLine("Argument substring minimum ADI SIZE --" + args[i].Substring("/ms=".Length));
                    try
                    {
                        minADIFileSie = int.Parse(args[i].Substring("/ms=".Length));
                    }catch(FormatException)
                    {
                        Console.WriteLine("Invalid parameter Minimum ADI File Size");
                    }
                    
                }
            }

            if (!string.IsNullOrEmpty(folderPath))
            {
                Console.WriteLine($"ADI Path: {folderPath}");
            }
            else
            {
                folderPath = $"C:/ADI_Archive";
                Console.WriteLine("ADI Folder Path default" + folderPath);
            }

            if (rec == "1")
            {
                TimerCallback timerCallback = new TimerCallback(async (state) =>
                {
                    await uploader();
                });

                Timer timer = new Timer(timerCallback, null, 0, 900000);//50000); //s

                while (true)
                {
                    Thread.Sleep(Timeout.Infinite);
                }
            }
            else
            {
                await uploader();
            }
          


        }
        public static string GetApplicationInstallLocation(string applicationName)
        {
            string displayName;
            string installLocation = "";
            RegistryKey key;
            Console.WriteLine($"application name - > {applicationName}");

            // search in: CurrentUser
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName") as string;
                Console.WriteLine($"display name - > {displayName}");
                if (applicationName.Equals(displayName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"path - > {subkey.GetValue("UrlUpdateInfo")}");
                    return subkey.GetValue("UrlUpdateInfo") as string;
                }
            }
            return installLocation;
        }
        private static async Task uploader()
        {
            try
            {
                string val = GetApplicationInstallLocation("HalDuFileUploadManager");
                if (val == "")
                    Console.WriteLine("Restart the Service from common Startup or Restart your System.");
                else
                {

                // Get the installation folder.
                string newPath = new Uri(val).LocalPath;
                // Remove the file name from the path because 'cd' command works with directories, not files.
                string directoryPath = Path.GetDirectoryName(newPath);
                Console.WriteLine($"directory path.{directoryPath}");
                string serviceName = "HalDuFileUploadManager.application";
                string shortPath = Path.Combine(directoryPath, serviceName);
                    bool isRunning = Process.GetProcessesByName("HalDuFileUploadManager").Length > 0;
                    if (!isRunning && File.Exists(shortPath))
                    {   
                        Process.Start(shortPath);
                        Thread.Sleep(10000);
                    }
                    else
                {
                    Console.WriteLine($"Could not Find {serviceName}.");
                }
                
                Console.Clear();
                buildFolderPath = AppDomain.CurrentDomain.BaseDirectory;

                
                logfolderpath = Path.Combine(buildFolderPath, "logs");
                Console.WriteLine("Log Folder Path" + logfolderpath);
                Console.WriteLine("PROGRAM WILL BE MANUALLY CLOSED WITH CTRL+C ");
                Console.WriteLine("Program running...");
                string fileName = $"LogFile_{DateTime.Today:yyyy-MM-dd}.csv";
                filePath = Path.Combine(logfolderpath, fileName);
                if (!Directory.Exists(logfolderpath))
                {
                    Console.WriteLine("directory does not exist ");
                    Directory.CreateDirectory(logfolderpath);
                }
                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine("directory does not exist ");
                    Directory.CreateDirectory(folderPath);

                }

                if (logger != null && !logger.sucessOpeningFileHeader)
                    logger = new Logger(filePath);
                else if (logger == null)
                    logger = new Logger(filePath);
                Console.WriteLine("Program running...", folderPath);
                var lastRowIndex = logger.GetLastLine();
                string[] filesToProcess = logger.GetFilesToProcess(folderPath);
                Console.WriteLine("" + logger.sucessOpeningFileCount + "    " + logger.sucessOpeningFileHeader + "    " + logger.logList.Count + "    " + filesToProcess.Length);
                if (logger.sucessOpeningFileCount && logger.sucessOpeningFileHeader && logger.logList.Count == 0 && filesToProcess.Length > 0)
                {
                    await PostFileUploader(filesToProcess);
                }
                else
                {
                        if(!(filesToProcess.Length > 0))
                        {
                            Console.WriteLine($"/* * \n *  Skipping this iteration \n * No files found to upload. Next upload time is {DateTime.Now.AddMinutes(15).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}.\n*/");
                        }
                        else
                        {
                            Console.WriteLine($"/* * \n *  Skipping this iteration , \n * {filePath} can't be opened. \n*/");
                        }
                    
                }

                }
            }
            catch(Exception er) 
            {
                Console.WriteLine($"/* * \n * Skipping this iteration \n There is an Exception - {er.ToString()} \n*/");
            }
           
            
            
               
        }
        static async Task PostFileUploader(string[] fileList)
        {
            Console.WriteLine("PostFileUploader call is in progress ...");

            try
            {
                UploadFileAttributes[] fileAttributesList = extractFileAttributesFromFolder(fileList);

                var tasks = new List<Task<RestResponse>>();
                var lastRowIndex = logger.GetLastLine();

                foreach (UploadFileAttributes fileAttributes in fileAttributesList)
                {
                    try
                    {
                        lastRowIndex += 1;
                        if (fileAttributes.FileMetadata.Size > minADIFileSie)
                        {
                            tasks.Add(saveDataAsync(fileAttributes, lastRowIndex));
                        }
                        else
                        {
                            logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Error", $"File size less than minimum file size specified - {minADIFileSie}.", lastRowIndex, "");
                        }
                        //Console.WriteLine(JsonConvert.SerializeObject(rs, Formatting.Indented));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error Uploading Payload for: " + fileAttributes.FileMetadata.FileName.Replace(",", " "));
                        Console.WriteLine(ex.Message);
                    }
                }

                Console.WriteLine("testing");
                // Console.ReadKey();
                await Task.WhenAll(tasks);
                Console.WriteLine("/*\n*\n* End of iteration\n*\n*/");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Uploading files in postfileuploader ");
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<RestResponse> saveDataAsync(UploadFileAttributes fileAttributes, int lastrowindex)
        {
            // Convert the object to JSON
            string data = JsonConvert.SerializeObject(fileAttributes);
            
            string json = JsonConvert.SerializeObject(fileAttributes);

            // Parse the JSON into a dynamic object
            JObject jsonObject = JObject.Parse(json);

            // Remove the properties that shouldn't be logged
            jsonObject.Property("Secret")?.Remove();
            jsonObject.Property("ClientID")?.Remove();

            // Convert the modified object back to JSON and print it
            string filteredJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            Console.WriteLine(filteredJson);
            //Console.WriteLine(JsonConvert.SerializeObject(fileAttributes, Formatting.Indented));
                // Console.ReadKey();
                string endpoint = "http://localhost:9000";
                string resource = "/api/FileUpload";

                
            //Console.ReadKey();
            try
            {
                logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Queued", "In queue.", lastrowindex, "");
                if (await CheckInternetConnection())
                {
                    using (RestClient client = new RestClient(endpoint))
                    {

                        RestResponse res = IsApiAvailable(endpoint + resource);
                        Console.WriteLine("res-> " + res.StatusCode);
                        // Console.ReadKey(); 
                        if (fileAttributes.FileMetadata.Size > 0)
                        {
                            RestRequest request = new RestRequest(resource, Method.Post);
                            request.AddHeader("Content-Type", "application/json");

                            request.AddParameter("application/json",data, ParameterType.RequestBody);
                            Console.WriteLine("request -> "+ request.ToString());
                            // Console.ReadKey();

                            logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Running", "Running.", lastrowindex, "");
                            Console.WriteLine("status: running");
                            // Console.ReadKey();


                            RestResponse rs = await client.ExecuteAsync(request);
                            Console.WriteLine("response -> "+ rs.ToString());
                            Console.WriteLine("content-> " + rs.Content);
                            Console.WriteLine("statuscode-> " + rs.StatusCode);
                            Console.WriteLine("IsSuccessfull-> " + rs.IsSuccessful);
                            //Console.ReadKey();
                            if (rs.IsSuccessful)
                            {
                                logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Success", "File upload successful.", lastrowindex, fileAttributes.FileMetadata.FileGuid);
                                Console.WriteLine($"The file got uploaded successfully");
                                // Console.ReadKey();
                            }
                            else
                            {

                                if (rs.Content != null)
                                {
                                    JsonDocument jsonResponse = JsonDocument.Parse(rs.Content);

                                    // Extract the "Message" value
                                    string message = jsonResponse.RootElement.GetProperty("Message").GetString();
                                    logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "AlreadyFound", "File already uploaded to Satori.", lastrowindex, "");
                                }

                                else if (rs.ErrorMessage != null)
                                {
                                    logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "NoComm", rs.ErrorMessage + "(Check if the Satori API service is running.)", lastrowindex, "");
                                    Console.WriteLine("status: error");
                                    Console.WriteLine($"File upload failed for reason: {rs.ErrorMessage } (Check if the Satori API service is running.)");
                                }

                                // Console.ReadKey();
                              
                            }
                            return rs;
                        }
                        else
                        {
                            logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Failed", "File size is zero.", lastrowindex, "");

                            Console.WriteLine("file size is 0");

                            return res;
                        }

                    };
                }
                else
                {
                    logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Failed", "Internet connection error.", lastrowindex, "");

                    RestResponse rs = new RestResponse();
                    rs.StatusCode = HttpStatusCode.Forbidden;
                    return rs;
                }
            }catch(Exception ex)
            {
                logger.Log(fileAttributes.FileMetadata.FilePath, fileAttributes.FileMetadata.FileName.Replace(",", " "), fileAttributes.FileMetadata.Size, mainIp, "Error", "Failed while uploading data. Will retry in next iteration.", lastrowindex, "");
                RestResponse rs = new RestResponse();
                rs.StatusCode = HttpStatusCode.Forbidden;
                return rs;
            }

        }
        private static async Task<bool> CheckInternetConnection()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync("8.8.8.8", 1000); // 8.8.8.8 is Google's DNS server
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking internet connection: {ex.Message}");
                return false;
            }
        }
        static RestResponse IsApiAvailable(string apiUrl)
        {
            try
            {
                Uri uri = new Uri(apiUrl);
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(uri.Host);
                    return new RestResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = "api Available"
                    };
                }
            }
            catch (PingException)
            {
                return new RestResponse
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = "api unAvailable"
                };
            }
        }
        static UploadFileAttributes[] extractFileAttributesFromFolder(string[] fileList)
        {
            var lastRowIndex = logger.GetLastLine();
            List<UploadFileAttributes> fileAttributeList = new List<UploadFileAttributes>();

            foreach (string filePath in fileList)
            {
                lastRowIndex += 1;
                UploadFileAttributes fileAttributes = extractFileAttributes(filePath, lastRowIndex);
                if (fileAttributes != null)
                {
                    fileAttributeList.Add(fileAttributes);
                }
            }

            return fileAttributeList.ToArray();
        }

        static UploadFileAttributes extractFileAttributes(string filePath, int lastRowIndex)
        {
            Console.WriteLine($"Extracting...");
            Console.WriteLine($"for file at: {filePath}");
            UploadFileAttributes fileAttributes = new UploadFileAttributes();
            fileAttributes.ClientID = "83556649-af97-4f62-8d04-35930d4617d4";
            fileAttributes.Secret = "cPA8Q~HOcX~KiYqSqPh6WNjJV9rgLgQQ3LaBXcWe";
            try
            {

                if (IsADIFileInUse(filePath))
                {
                    Console.WriteLine($"This ADI file is open in another service/application {filePath}");
                    logger.Log(filePath, Path.GetFileName(filePath).Replace(",", " "), 0, mainIp, "SizeZero", "This ADI file was open in another service/application.", lastRowIndex, "");
                    return null;
                }
                AdiFileMetadata afm = new AdiFileMetadata();
                afm = FileHelper.ReadAdiFile(filePath);
                if (!ValidateAdiFileMetadata(afm))
                {
                    Console.WriteLine($"Invalid AdiFileMetadata for file at: {filePath}");
                    logger.Log(filePath, afm.FileName.Replace(",", " "), afm.FileStream.Length, mainIp, "ValidationFailed", "Some ADI file metadata/well info data is missing.", lastRowIndex, "");
                    return null; // Return null if validation fails
                }
                fileAttributes.FileMetadata = new Filemetadata
                {
                    FilePath = filePath,
                    FileName = afm.FileName,
                    FileType = afm.FileType,
                    ApiUwiNumber = afm.ApiUwiNumber,
                    CustomerName = afm.CustomerName,
                    JobTicketNumber = afm.JobTicketNumber,
                    Latitude = afm.Latitude,
                    Longitude = afm.Longitude,
                    WellName = afm.WellName,
                    FileGuid = afm.FileGuid,
                    Size = afm.FileStream.Length,
                    PslKey = "hpe", // always will hpe
                    HalRegionKey = "NAL" // will always Satori 
                };
                fileAttributes.OptionalAttributes = new Dictionary<string, object>();
                fileAttributes.OptionalAttributes.Add("location", "San Diego");
                fileAttributes.OptionalAttributes.Add("jobName", "Satori-test");
                fileAttributes.OptionalAttributes.Add("wellNumber", "100021");
                Console.WriteLine($"returning extracted values for {filePath}");
                return fileAttributes;
            }
            catch(Exception ex)
            {
                FileInfo fileInfo = new FileInfo(filePath);

                // Get the size of the file
                long fileSizeInBytes = fileInfo.Length;
                Console.WriteLine($"This file is Corruted {filePath} \n Error - {ex.ToString()}");
                logger.Log(filePath, Path.GetFileName(filePath).Replace(",", " "), fileSizeInBytes, mainIp, "Corrupted", "File is corrupted so re-export dataset from IFS.", lastRowIndex, "");
                return null;
            }
        }
        static bool IsADIFileInUse(string filePath)
        {
            try
            {
                // Attempt to open the file exclusively.
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    fileStream.Close();
                }
            }
            catch (IOException)
            {
                // If we can't open the file exclusively, it's likely in use by another process.
                return true;
            }

            return false;
        }
        private static bool ValidateAdiFileMetadata(AdiFileMetadata afm)
        {
            // Check for required fields and return false if any are missing
            if (string.IsNullOrWhiteSpace(afm.FileName) ||
                string.IsNullOrWhiteSpace(afm.FileType) ||
                string.IsNullOrWhiteSpace(afm.ApiUwiNumber) ||
                string.IsNullOrWhiteSpace(afm.CustomerName) ||
                string.IsNullOrWhiteSpace(afm.JobTicketNumber) ||
                string.IsNullOrWhiteSpace(afm.WellName) ||
                string.IsNullOrWhiteSpace(afm.FileGuid) ||
                afm.FileStream == null)
            {
                return false;
            }

            // Validate latitude
            if (double.TryParse(afm.Latitude, out double latitude))
            {
                if (latitude < -90 || latitude > 90)
                {
                    return false;
                }
            }
            else
            {
                return false; // Return false if parsing fails
            }

            // Validate longitude
            if (double.TryParse(afm.Longitude, out double longitude))
            {
                if (longitude < -180 || longitude > 180)
                {
                    return false;
                }
            }
            else
            {
                return false; // Return false if parsing fails
            }

            // Can add more validation checks here, depending on our requirements

            return true;
        }


        public class UploadFileAttributes
        {
            public string ClientID { get; set; }
            public string Secret { get; set; }
            public Filemetadata FileMetadata { get; set; }
            public Dictionary<string, object> OptionalAttributes { get; set; }
        }
        public class Filemetadata
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string FileType { get; set; }
            public string ApiUwiNumber { get; set; }
            public string CustomerName { get; set; }
            public string JobTicketNumber { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string WellName { get; set; }
            public string FileGuid { get; set; }
            public long Size { get; set; }
            public string PslKey { get; set; }
            public string HalRegionKey { get; set; }
        }
       
    }
}

