using AdiParser.Essentials;
using AdiParser.Main;
using AdiParser.Main.DatasetInfo;
using ConsoleApp1;
using ConsoleApp1.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    public class FileHelper
    {
        private static readonly string path = Constants.CmdArgs.BaseFolder;

        public static List<UploadStatus> GetAllFilesUploadedFromSuccessfullyTxtFile(ILogger logger)
        {
            var filePath = $"{path}\\{Constants.FileName.SatoriSuccessfully}";
            logger.LogInformation($"GetAllFilesUploadedSuccessfully - filepath: {filePath}");

            var jobs = new List<UploadStatus>();

            if (File.Exists(filePath))
            {
                // Read content of successfully.txt file if existed
                using StreamReader sr = File.OpenText(filePath);
                var s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(s)) continue;

                    string[] arrValues = Regex.Split(s, ";(?=(?:[^']*'[^']*')*[^']*$)");

                    var dto = new UploadStatus
                    {
                        FileNameWithFullPath = arrValues[0],
                        UploadingPlatform = arrValues[1],
                        IsUploadedFile = bool.Parse(arrValues[2]),
                        SatoriLocationUrl = arrValues[3],
                        UploadingTime = arrValues[4],
                    };

                    if (dto.FileNameWithFullPath[0] == '\'' && dto.FileNameWithFullPath[dto.FileNameWithFullPath.Length - 1] == '\'')
                    {
                        dto.FileNameWithFullPath = dto.FileNameWithFullPath.Remove(dto.FileNameWithFullPath.Length - 1);
                        dto.FileNameWithFullPath = dto.FileNameWithFullPath.Remove(0, 1);
                    }

                    jobs.Add(dto);
                }
                sr.Close();
            }
            logger.LogInformation($"GetAllFilesUploadedSuccessfully - already uploaded counts: {jobs.Count}");

            return jobs;
        }

        public static string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static List<string> GetListOfAdiFilesNeedToUpload(ILogger logger, List<string> successJobs)
        {
            // Get all Adi files in uploading folder.
            // Get files in children folders if IsLoadingRecursive
            var allfilePaths = Constants.CmdArgs.IsLoadingRecursive ?
                Directory.GetFileSystemEntries(Constants.CmdArgs.UploadingFolder, "*", SearchOption.AllDirectories)
                : Directory.GetFileSystemEntries(Constants.CmdArgs.UploadingFolder, "*");
            var filePaths = allfilePaths.Where(i => Path.GetExtension(i).ToLower().Contains(".adi")).ToList();

            // Log all files detected
            logger.LogInformation($"GetListOfAdiFilesNeedToUpload - all adi files detected:");
            foreach (var item in filePaths)
            {
                logger.LogInformation($"GetListOfAdiFilesNeedToUpload - adi files detected - filepath: {item}");
            }

            // Filter out all successfully-uploaded files
            var result = filePaths.Where(i => !successJobs.Contains(i.Trim())).ToList();

            // Log all files' path
            logger.LogInformation($"GetListOfAdiFilesNeedToUpload - job counts: {result.Count}");
            foreach (var item in result)
            {
                logger.LogInformation($"GetListOfAdiFilesNeedToUpload - need to upload - filepath: {item}");
            }

            return result.Distinct().ToList();
        }

        public static AdiFileMetadata ReadAdiFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            var details = GetUploadedFileInfo(path, new UploadStatus());

            try
            {
                details.FileStream = File.OpenRead(path);
                details.FilePath = details.FileStream.Name;
                details.FileName = GetFilename(details.FilePath);
                details.FileType = "AdiRaw";

                var parser = AdiParserFactory.GetParser(details.FileStream);
                var ds = parser.GetRecordData(details.FileStream,
                    x => x.RecordName == AdiRecordNames.WellInfo && x.RunNumber == 0).FirstOrDefault();
                if (ds == null) return details;

                var results = ds.GetRecordData(details.FileStream,
                    DatasetInfoCategory.Bag | DatasetInfoCategory.Comment);

                // Adi Metadata key attributes
                details.ApiUwiNumber = GetValueOrDefault(results.BagDataRecord, AdiVariables.API_SN);
                details.CustomerName = GetValueOrDefault(results.BagDataRecord, AdiVariables.CustomerName);
                details.JobTicketNumber = GetValueOrDefault(results.BagDataRecord, AdiVariables.JobTicketNumb);
                details.Latitude = GetValueOrDefault(results.BagDataRecord, AdiVariables.Latitude);
                details.Longitude = GetValueOrDefault(results.BagDataRecord, AdiVariables.Longitude);
                details.WellName = GetValueOrDefault(results.BagDataRecord, AdiVariables.WellName);

                // Adi Metadata optional attributes
                details.FileGuid = Guid.NewGuid().ToString(); ; //$"{DateTime.Now:yy-MM-dd-HH-mm-ss}";
                details.FileVersion = parser.Version;
                details.RigName = GetValueOrDefault(results.BagDataRecord, AdiVariables.IPSRigName);
            }
            catch (Exception e)
            {
                details.FileStream = null;
                details.FileError = e.Message;
            }

            return details;
        }

        public static void SaveJobIntoTxtFile(ILogger logger, UploadStatus fileDto)
        {
            var filePath = $"{path}\\{Constants.FileName.SatoriSuccessfully}";
            logger.LogInformation($"SaveJobsIntoTxtFile - filepath: {filePath}");
            logger.LogInformation($"SaveJobsIntoTxtFile - file content:");

            try
            {
                var temp = $"\'{fileDto.FileNameWithFullPath}\';{fileDto.UploadingPlatform};{fileDto.IsUploadedFile};{fileDto.UploadingTime};{fileDto.SatoriLocationUrl}";
                logger.LogInformation($"data line: {temp}");

                var sw = new StreamWriter(filePath, true);
                sw.WriteLineAsync(temp);
                sw.Close();
            }
            catch (Exception e)
            {
                logger.LogError($"SaveJobsIntoTxtFile - Error Message: {e.Message}");
                logger.LogError($"SaveJobsIntoTxtFile - InnerException: {e.InnerException}");
            }
        }

        #region Private

        private static string GetFilename(string filePath)
        {
            //var originFileName = Path.GetFileName(filePath.Trim()); // origin filename
            //var filename = originFileName.Replace(";", " ");
            return Path.GetFileName(filePath.Trim());
        }

        private static AdiFileMetadata GetUploadedFileInfo(string filePath, UploadStatus job)
        {
            var fileInfo = new AdiFileMetadata()
            {
                FileNameWithFullPath = filePath,
                SatoriLocationUrl = job == null ? "" : job.SatoriLocationUrl,
                UploadingPlatform = job == null ? "Satori" : job.UploadingPlatform,
                IsUploadedFile = job is { IsUploadedFile: true },
                FileError = string.Empty
            };

            return fileInfo;
        }

        private static string GetValueOrDefault(IResult dataRecord, string fieldName)
        {
            var result = !dataRecord.DoesFieldExist(fieldName) ? (object)"NA" : dataRecord.GetAdiValue(fieldName).NullOrValue();
            return result == null ? "NA" : result.ToString();
        }

        #endregion
    }
}
