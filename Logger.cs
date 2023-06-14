using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
//using System.Reflection.PortableExecutable;


namespace ConsoleApp1
{
    public class Logger
    {
        public string _logFilePath = "";

        public bool sucessOpeningFileHeader = false;
        public bool sucessOpeningFileCount = false;
        public int lastLine = 0;
        public List<KeyValuePair<string,int>> logList = new List<KeyValuePair<string, int>>();
        public Logger(string logFilePath)
        {
            _logFilePath = logFilePath;
            sucessOpeningFileCount = false;
            // Create the file with the header if it dosesn't exist


            var header = "time_local,time_utc,d_full,d_base,d_size,d_time,u_version,u_ip,u_time,u_fileguid,u_status,u_message";

            Console.WriteLine("Logger Constructor ");

            try
            {
                List<string> currentData = new List<string>();
                currentData = ReaderCSV(logFilePath);
                Console.WriteLine("current data " + currentData.Count);
                if (currentData.Count == 0)
                {
                    currentData.Add(header);
                    Console.WriteLine("current data1 " + currentData.Count);
                    WriterCSV(currentData, logFilePath);
                }
                sucessOpeningFileHeader = true;
                currentData = ReaderCSV(logFilePath);
                _logFilePath = logFilePath;
                Console.WriteLine("current data2 " + currentData.Count);
            }
            catch (Exception err)
            {
               // Console.WriteLine($"Error Opening File {err.Message}");
                sucessOpeningFileHeader = false;
            }


        }

        public List<string> ReaderCSV(string logFilePath)
        {
            Console.WriteLine("Reader CSV");
            var currentData = new List<string>();
            using (FileStream fileStream = new FileStream(logFilePath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (var reader = new StreamReader(_logFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        currentData.Add(reader.ReadLine());
                    }
                }
            }

            return currentData;
        }

        public void WriterCSV(List<string> currentData, string logFilePath)
        {
            Console.WriteLine("Writer CSV" + currentData);

            using (var writer = new StreamWriter(logFilePath))
            {
                foreach (var line in currentData)
                {
                    writer.WriteLine(line);
                }
            }

        }
        public void Log(string d_full, string d_base, long d_size, IPAddress u_ip, string u_status, string u_message, int rowIndex, string fileguid)
        {
            /*var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var time_local = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var u_time = File.GetCreationTimeUtc(_logFilePath);*/
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
            var time_local = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            var u_time = File.GetCreationTimeUtc(_logFilePath).ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
            var d_time = File.GetLastWriteTime(d_full).ToString("o");
            d_full = d_full.Replace("\\", "/");
            string u_version = "2023.06.13";
            string logEntry = $"{time_local},{timestamp},{d_full},{d_base},{d_size},{d_time},{u_version},{u_ip},{u_time},{fileguid},{u_status},{EscapeCsvField(u_message)}";
            AddUniqueKeyValuePair(logList,new KeyValuePair<string,int>(logEntry, rowIndex));

            //logList.ForEach(log => Console.WriteLine($"{log} in LogList "));
            while (logList.Count > 0)
            {
                if (sucessOpeningFileHeader && sucessOpeningFileCount)
                {
                    try
                    {

                        List<string> currentData = ReaderCSV(_logFilePath);
                        
                        int index= logList[0].Value - 1;
                      //  Console.WriteLine("current data3 " + currentData.Count + " " + index);
                        if (index > 0 && index < currentData.Count)
                        {
                            currentData[index] = logList[0].Key;
                        }
                        else
                        {
                            currentData.Add(logList[0].Key);
                        }
                      //  Console.WriteLine("current data4 " + currentData.Count);
                        WriterCSV(currentData, _logFilePath);
                        logList.RemoveAt(0);

                    }
                    catch (Exception e)
                    {
                    //    Console.WriteLine($"Error in writing log {e.Message}");
                        Thread.Sleep(10000);
                    }
                }
            }

        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            // Escape quotes with double quotes and wrap the field in quotes if it contains a comma, newline or quote
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
        public int GetLastLine()
        {

            try
            {

                List<string> lines = ReaderCSV(_logFilePath);
                lastLine = lines.Count;
              //  Console.WriteLine("lastLine " + lastLine);
                sucessOpeningFileCount = true;
            }
            catch (Exception err)
            {
              //  Console.WriteLine($"Error Opening File getting count {err.Message}");
                sucessOpeningFileCount = false;

            }
            return lastLine;
        }
        public string[] GetFilesToProcess(string folderPath)
        {
            var loggedFiles = new Dictionary<string, List<string>>();
            string[] filesToProcess = new string[] { };

            try
            {
                List<string> data = ReaderCSV(_logFilePath);


                int rowCount = data.Count;
                if (rowCount > 0)
                {
                    for (int row = 1; row < rowCount; row++)
                    {
                       // Console.WriteLine("single data " + rowCount + " " + row);

                        string[] singledataValues = data[row].Split(',');
                       // Console.WriteLine("single data " + singledataValues[9]);
                        string fileName = singledataValues[3];
                        List<string> filedata = new List<string>();

                        filedata.Add(singledataValues[5]);
                        filedata.Add(singledataValues[4]);
                        filedata.Add(singledataValues[10]);

                        loggedFiles[fileName] = filedata;
                    }
                }
                // var allAdiFiles = Directory.GetFiles(folderPath, "*.adi");
                var allAdiFiles = Directory.GetFiles(folderPath, "*.adi", SearchOption.AllDirectories);

                filesToProcess = allAdiFiles
                        .Where(file => !loggedFiles.ContainsKey(Path.GetFileName(file).Replace(",", " ")) ||
                        ((loggedFiles[Path.GetFileName(file).Replace(",", " ")][2] == "AlreadyFound" || 
                        loggedFiles[Path.GetFileName(file).Replace(",", " ")][2] == "Success") && 
                        loggedFiles[Path.GetFileName(file).Replace(",", " ")][0] != File.GetLastWriteTime(file).ToString("o")) ||
                        loggedFiles[Path.GetFileName(file).Replace(",", " ")][2] != "Success" &&
                        loggedFiles[Path.GetFileName(file).Replace(",", " ")][2] != "AlreadyFound"
                        )
                        .ToArray();
            }
            catch (Exception err)
            {
              //  Console.WriteLine($"Error in Getting Files {err.Message}");
            }

            return filesToProcess;
        }
        static void AddUniqueKeyValuePair(List<KeyValuePair<string, int>> keyValuePairs, KeyValuePair<string, int> newPair)
        {
            bool keyExists = false;

            foreach (KeyValuePair<string, int> pair in keyValuePairs)
            {
                if (pair.Key.Equals(newPair.Key))
                {
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                keyValuePairs.Add(newPair);
            }
        }
    }
}