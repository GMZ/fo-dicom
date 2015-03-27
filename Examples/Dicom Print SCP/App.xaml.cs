using System;
using System.Configuration;
using System.Globalization;
using System.Windows;

namespace Dicom.PrintScp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private Configuration _appConfig;
        private ExeConfigurationFileMap _exeConfigurationFileMap;
        private String _printerName;
        private String _manufacturer;
        private String _manufacturerModelName;
        private String _deviceSerialNumber;

        internal Int32 PortNumber;
        internal String AETitle;
        internal Printer DicomPrinter;
        

        protected override void OnStartup(StartupEventArgs e)
        {
            LoadConfigSettings();
            DicomPrinter = new Printer(AETitle, _printerName, _manufacturer, _manufacturerModelName, _deviceSerialNumber);
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveConfigSettings();
            base.OnExit(e);
        }

        private void LoadConfigSettings()
        {
            _exeConfigurationFileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };
            _appConfig = ConfigurationManager.OpenMappedExeConfiguration(_exeConfigurationFileMap,
                                                                         ConfigurationUserLevel.None);

            PortNumber = ConfigurationManager.AppSettings["Port"] != null 
                ? Convert.ToInt32(_appConfig.AppSettings.Settings["Port"].Value) 
                : 8000;
            AETitle = ConfigurationManager.AppSettings["Aetitle"] != null 
                ? _appConfig.AppSettings.Settings["Aetitle"].Value 
                : "PRINTSCP";
            _printerName = ConfigurationManager.AppSettings["PrinterName"] != null 
                ? _appConfig.AppSettings.Settings["PrinterName"].Value 
                : String.Empty;
            _manufacturer = ConfigurationManager.AppSettings["Manufacturer"] != null 
                ? _appConfig.AppSettings.Settings["Manufacturer"].Value 
                : "Manufacturer Goes Here";
            _manufacturerModelName = ConfigurationManager.AppSettings["ManufacturerModelName"] != null ? _appConfig.AppSettings.Settings["ManufacturerModelName"].Value : "Uber Printer";
            _deviceSerialNumber = ConfigurationManager.AppSettings["DeviceSerialNumber"] != null 
                ? _appConfig.AppSettings.Settings["DeviceSerialNumber"].Value 
                : "007-AWESOME";


        }

        private void SaveConfigSettings()
        {

            _appConfig.AppSettings.Settings["Port"].Value = PortNumber.ToString(CultureInfo.InvariantCulture);
            _appConfig.AppSettings.Settings["Aetitle"].Value = AETitle;
            _appConfig.Save(); 
        }
    }
}
