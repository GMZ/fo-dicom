using System;
using System.Reflection;

namespace Dicom.PrintScp
{
    public class Printer : DicomDataset
    {
        #region Properties and Attributes

        public string PrinterAet { get; private set; }

        /// <summary>
        /// Printer device status
        /// </summary>
        /// <remarks>
        /// Enumerated values:
        /// <list type="bullet">
        /// <item><description>NORMAL</description></item>
        /// <item><description>WARNING</description></item>
        /// <item><description>FAILURE</description></item>
        /// </list>
        /// </remarks>
        public string PrinterStatus
        {
            get { return Get(DicomTag.PrinterStatus, "NORMAL"); }
            private set { Add(DicomTag.PrinterStatus, value); }
        }

        /// <summary>
        /// Additional information about printer status (2110,0020)
        /// </summary>
        /// <remarks>
        /// Defined terms when the printer status is equal to NORMAL: NORMAL
        /// See section C.13.9.1 for defined terms when the printer status is equal to WARNING or FAILURE
        /// </remarks>
        public string PrinterStatusInfo
        {
            get { return Get(DicomTag.PrinterStatusInfo, "NORMAL"); }
            private set { Add(DicomTag.PrinterStatusInfo, value); }
        }

        /// <summary>
        /// User defined name identifying the printer
        /// </summary>
        public string PrinterName
        {
            get { return Get(DicomTag.PrinterName, string.Empty); }
            private set { Add(DicomTag.PrinterName, value); }
        }

        /// <summary>
        /// Manufacturer of the printer
        /// </summary>
        public string Manufacturer
        {
            get { return Get(DicomTag.Manufacturer, "Manufacturer Goes Here"); }
            private set { Add(DicomTag.Manufacturer, value); }

        }

        /// <summary>
        /// Manufacturer's model number of the printer
        /// </summary>
        public string ManufacturerModelName
        {
            get { return Get(DicomTag.ManufacturerModelName, "Uber Printer"); }
            private set { Add(DicomTag.ManufacturerModelName, value); }
        }

        /// <summary>
        /// Manufacturer's serial number of the printer
        /// </summary>
        public string DeviceSerialNumber
        {
            get { return Get(DicomTag.DeviceSerialNumber, "007-AWESOME"); }
            private set { Add(DicomTag.DeviceSerialNumber, value); }
        }

        /// <summary>
        /// Manufacturer's designation of software version of the printer
        /// </summary>
        public string SoftwareVersions
        {

            get { return Get(DicomTag.SoftwareVersions, Assembly.GetExecutingAssembly().GetName().Version.ToString()); }
        }

        /// <summary>
        /// Date and Time when the printer was last calibrated
        /// </summary>
        public DateTime DateTimeOfLastCalibration
        {
            get { return this.GetDateTime(DicomTag.DateOfLastCalibration, DicomTag.TimeOfLastCalibration); }
            private set
            {
                Add(DicomTag.DateOfLastCalibration, value);
                Add(DicomTag.TimeOfLastCalibration, value);

            }
        }

        public Boolean PreviewOnly { get; set; }

        #endregion

        #region Constructors and Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="Printer"/> class.
        /// </summary>
        /// <param name="aet">The aet.</param>
        /// <param name="printerName">Name of the printer.</param>
        /// <param name="manufacturer">The manufacturer.</param>
        /// <param name="modelNumber">The model number.</param>
        /// <param name="serial">The serial.</param>
        public Printer(string aet, string printerName, string manufacturer, string modelNumber, string serial)
        {
            PrinterAet = aet;
            DateTimeOfLastCalibration = DateTime.Now;

            PrinterStatus = "NORMAL";
            PrinterStatusInfo = "NORMAL";
            if (!string.IsNullOrEmpty(printerName))
                PrinterName = printerName;
            Manufacturer = manufacturer;
            ManufacturerModelName = modelNumber;
            DeviceSerialNumber = serial;
        }

        #endregion
    }
}
