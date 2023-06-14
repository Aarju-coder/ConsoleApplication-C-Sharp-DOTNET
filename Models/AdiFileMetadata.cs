using System.IO;

namespace ConsoleApp1.Models
{
    public class AdiFileMetadata : UploadStatus
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public FileStream FileStream { get; set; }
        public string WellOID { get; set; }
        public string FileError { get; set; }


        #region ADI key attributes

        public string ApiUwiNumber { get; set; }
        public string CustomerName { get; set; }
        public string JobTicketNumber { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string WellName { get; set; }
        #endregion


        #region ADI optional attributes

        public string InsiteVersion { get; set; }
        public string InsiteUser { get; set; }
        public string FileGuid { get; set; }
        public long FileVersion { get; set; }
        public string RigName { get; set; }
        //public string DeletionDate { get; set; }
        //public string MsaReferenceCustomerName { get; set; }
        //public string StandardCustomerName { get; set; }

        // ADI Parquet special attributes
        public string InsiteRecord { get; set; }
        public string InsiteDescription { get; set; }
        public string InsiteRun { get; set; }
        public string AdiRowKey { get; set; }
        #endregion
    }
}
