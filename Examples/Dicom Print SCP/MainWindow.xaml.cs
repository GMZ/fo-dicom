using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Dicom.Log;
using Dicom.Network;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Dicom.PrintScp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly App _appRef;

        #region Logging

        private void InitializeLog()
        {
            var target = new WpfRichTextBoxTarget(Color.FromRgb(0, 0, 0))
            {
                Name = "console",
                Layout = "${message}",
                ControlName = LogRitchTextBox.Name,
                FormName = Name,
                AutoScroll = true,
                MaxLines = 100000,
                UseDefaultRowColoringRules = true
            };
            var asyncWrapper = new AsyncTargetWrapper
            {
                Name = "console",
                WrappedTarget = target
            };
            SimpleConfigurator.ConfigureForTargetLogging(asyncWrapper, NLog.LogLevel.Debug);

            LogManager.Default = new NLogManager();
        }

        #endregion // Logging

        public MainWindow()
        {
            InitializeComponent();
            _appRef = (App)Application.Current;

            InitializeLog();

            StartButton.Click += StartButtonOnClick;
            StopButton.Click += StopButtonOnClick;
            ClearButton.Click += ClearButtonClick;
            SaveLog.Click += SaveLogOnClick;
            Loaded += OnLoaded;
            PortTextBox.GotFocus += TextBoxOnGotFocus;
            PortTextBox.LostFocus += PortTextBoxOnLostFocus;
            AeTextBox.GotFocus += TextBoxOnGotFocus;
            AeTextBox.LostFocus += AeTextBoxOnLostFocus;
        }

        #region Window Events

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            PortTextBox.Text = _appRef.PortNumber.ToString(CultureInfo.InvariantCulture);
            AeTextBox.Text = _appRef.AETitle;

            PrintService.Start(_appRef.PortNumber, _appRef.DicomPrinter,
                SavePrintJob.IsChecked == true);
        }

        private static void TextBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            var tb = sender as TextBox;
            if (tb != null) tb.SelectAll();
        }

        private void AeTextBoxOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            _appRef.PortNumber = Convert.ToInt32(PortTextBox.Text);
        }

        private void PortTextBoxOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            _appRef.AETitle = AeTextBox.Text;
        }

        #endregion // Window Events

        #region Button Events

        private void StartButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            PrintService.Start(_appRef.PortNumber, _appRef.DicomPrinter,
                SavePrintJob.IsChecked == true);
            PortTextBox.IsEnabled = false;
            AeTextBox.IsEnabled = false;
            SavePrintJob.IsEnabled = false;
        }

        private void StopButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            PrintService.Stop();
            PortTextBox.IsEnabled = true;
            AeTextBox.IsEnabled = true;
            SavePrintJob.IsEnabled = true;
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            LogRitchTextBox.Document = new FlowDocument();
        }

        private void SaveLogOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(baseDir + @"\Logs\"))
            {
                Directory.CreateDirectory(baseDir + @"\Logs\");
            }
            // Save 
            var source = LogRitchTextBox.Document;
            var range = new TextRange(source.ContentStart, source.ContentEnd);
            using (var stream = File.Create(baseDir + @"\Logs\" + DateTime.Now.ToString("yyyyMMddhhmmssfff") + ".txt"))
            {
                range.Save(stream, DataFormats.Text);
            }
        }

        #endregion
    }
}
