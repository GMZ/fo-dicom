using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Dicom;
using Dicom.Log;
using Dicom_Print_SCU.Configuration;
using Microsoft.Win32;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Dicom_Print_SCU
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly App _appRef;
        private List<DicomDataset> _dataSets = new List<DicomDataset>();
        private int _currentFrameNo;
        private int _numberofFrames;
        private ImageSource _imgSource;
        private List<String> _filesToOpen;
        private Point _screenPosition;
        private Boolean _isMouseCaptured;
        private String _format;
        private Boolean _staticRef;

        #region Logging

        private void InitializeLog()
        {
            //var config = new LoggingConfiguration();

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

            //LogManager.Default = new NLogManager();
        }

        #endregion // Logging

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            _appRef = (App) Application.Current;
            OpenButton.Click += OpenButtonClick;
            PrintButton.Click += PrintButtonClick;
            ClearButton.Click += ClearButtonClick;
            ResetButton.Click += ResetButtonClick;
            SaveLog.Click += SaveLogOnClick;
            DicomImage.MouseWheel += DicomImage_MouseWheel;
            PreDefinedConnection.SelectionChanged += PreDefinedConnectionOnSelectionChanged;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            Loaded += OnLoaded;
            PreviewKeyDown += OnPreviewKeyDown;

            InitializeLog();
        }

        #endregion
        
        #region Window Events

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            PreDefinedConnection.DisplayMemberPath = "DisplayName";
            _appRef.AETitleConfigSection.AeTitleCollection.InsertAt(new AeTitleConfigElement
            {
                DisplayName = "",
                LocalAeTitle = "",
                RemoteAeTitle = "",
                RemoteIpAddress = "",
                RemotePort = ""
            });
            foreach (var item in _appRef.AETitleConfigSection.AeTitleCollection)
            {
                PreDefinedConnection.Items.Add(item);
            }
        }

        private void PreDefinedConnectionOnSelectionChanged(object sender,
                                                            SelectionChangedEventArgs selectionChangedEventArgs)
        {
            var selectedValue = (AeTitleConfigElement)PreDefinedConnection.SelectedItem;

            if (string.IsNullOrEmpty(selectedValue.DisplayName))
                ClearConnectionDetails();
            else
                SetConnectionDetails(selectedValue);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Up && _dataSets.Count > 0)
            {
                _currentFrameNo += 1;
                if (_currentFrameNo >= _numberofFrames)
                {
                    _currentFrameNo = 0;
                }
                SetCurrentImage();
            }
            if (keyEventArgs.Key == Key.Down && _dataSets.Count > 0)
            {
                _currentFrameNo -= 1;
                if (_currentFrameNo <= 0)
                {
                    _currentFrameNo = _numberofFrames - 1;
                }
                SetCurrentImage();
            }
        }

        #endregion // Window Events

        #region Mouse Events

        private void DicomImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_numberofFrames > 1)
            {
                if (e.Delta >= 0)
                {
                    _currentFrameNo -= 1;
                    if (_currentFrameNo <= 0)
                    {
                        _currentFrameNo = _numberofFrames - 1;
                    }
                }
                else
                {
                    _currentFrameNo += 1;
                    if (_currentFrameNo >= _numberofFrames)
                    {
                        _currentFrameNo = 0;
                    }
                }
                SetCurrentImage();
            }
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (DicomImage.IsMouseDirectlyOver)
            {
                _isMouseCaptured = true;
                ((UIElement)sender).CaptureMouse();
                ((FrameworkElement)sender).Cursor = Cursors.Hand;
                _screenPosition = mouseButtonEventArgs.GetPosition(DicomImage);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!_isMouseCaptured)
            {
                return;
            }
            Point currentPosition = mouseEventArgs.GetPosition(DicomImage);
            double delta = currentPosition.Y - _screenPosition.Y;

            if (delta <= -1)
            {
                _currentFrameNo -= 1;
                if (_currentFrameNo <= 0)
                {
                    _currentFrameNo = 0;
                }
            }
            else if (delta > 1)
            {
                _currentFrameNo += 1;
                if (_currentFrameNo >= _numberofFrames - 1)
                {
                    _currentFrameNo = _numberofFrames - 1;
                }
            }
            _screenPosition = currentPosition;
            SetCurrentImage();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var item = sender as FrameworkElement;
            _isMouseCaptured = false;
            if (item != null)
            {
                item.ReleaseMouseCapture();
                item.Cursor = null;
            }
            _screenPosition = new Point(0, 0);
        }

        #endregion // Mouse Events

        #region Button Events

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                var names = dlg.FileNames;
                _filesToOpen = new List<String>();
                _filesToOpen.AddRange(names);
                foreach (var fileName in names)
                {
                    DicomFile df;
                    try
                    {
                        df = DicomFile.Open(fileName);
                    }
                    catch (DicomFileException fileException)
                    {
                        MessageBox.Show(fileException.Message);
                        df = fileException.File;
                    }
                    if (df != null) _dataSets.Add(df.Dataset);
                }
                _numberofFrames = _dataSets.Count;
                if (_numberofFrames >= 20)
                {
                    FormatComboBox.SelectedIndex = 10;
                }
                SetCurrentImage();
            }
        }

        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            ClearButtonClick(this, null);
            _currentFrameNo = 0;
            _numberofFrames = 0;
            _filesToOpen.Clear();
            _filesToOpen = null;
            _filesToOpen = new List<String>();
            DicomImage.Source = null;

            _dataSets.Clear();
            _dataSets = null;
            _dataSets = new List<DicomDataset>();
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

        private void PrintButtonClick(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CallingAeText.Text) || String.IsNullOrEmpty(CalledAeText.Text) ||
                String.IsNullOrEmpty(RemoteHostText.Text) || String.IsNullOrEmpty(RemotePortText.Text))
            {
                MessageBox.Show(
                    "Please select a pre-defined server from the drop down box or specify the connection details",
                    "Connection not specified", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }
            _format = GetFormat();
            DicomPrint();
        }

        #endregion

        #region Private Functions

        private void SetCurrentImage()
        {
            var overlays = Convert.ToBoolean(((ComboBoxItem) OverlaysComboBox.SelectedItem).Content.ToString());
            var anon = Convert.ToBoolean(((ComboBoxItem) AnnonymizeComboBox.SelectedItem).Content.ToString());
            _imgSource = ImageHelper.GenerateImageSource(_dataSets[_currentFrameNo], overlays, anon);
            
            DicomImage.Source = _imgSource.CloneCurrentValue();
            ImageCountText.Text = string.Format("{0} / {1} Images", _currentFrameNo + 1, _numberofFrames);
            _imgSource = null;
        }

        private String GetFormat()
        {
            String retVal;
            var selectedFormat = ((ComboBoxItem)FormatComboBox.SelectedItem).Content.ToString();
            switch (selectedFormat)
            {
                case "Standard 1,1":
                    retVal = @"STANDARD\1,1";
                    break;
                case "Standard 1,2":
                    retVal = @"STANDARD\1,2";
                    break;
                case "Standard 2,1":
                    retVal = @"STANDARD\2,1";
                    break;
                case "Standard 2,2":
                    retVal = @"STANDARD\2,2";
                    break;
                case "Standard 2,3":
                    retVal = @"STANDARD\2,3";
                    break;
                case "Standard 2,4":
                    retVal = @"STANDARD\2,4";
                    break;
                case "Standard 3,3":
                    retVal = @"STANDARD\3,3";
                    break;
                case "Standard 3,4":
                    retVal = @"STANDARD\3,4";
                    break;
                case "Standard 3,5":
                    retVal = @"STANDARD\3,5";
                    break;
                case "Standard 4,4":
                    retVal = @"STANDARD\4,4";
                    break;
                case "Standard 4,5":
                    retVal = @"STANDARD\4,5";
                    break;
                default:
                    retVal = @"STANDARD\1,1";
                    break;
            }
            return retVal;
        }
        
        private void SetConnectionDetails(AeTitleConfigElement selectedValue)
        {
            CallingAeText.Text = string.IsNullOrEmpty(selectedValue.LocalAeTitle)
                                     ? Environment.MachineName.ToUpper() + @"_SCU"
                                     : selectedValue.LocalAeTitle;
            CalledAeText.Text = selectedValue.RemoteAeTitle;
            RemoteHostText.Text = selectedValue.RemoteIpAddress;
            RemotePortText.Text = selectedValue.RemotePort;
            _staticRef = !string.IsNullOrEmpty(selectedValue.StaticReference) && Convert.ToBoolean(selectedValue.StaticReference);
        }

        private void ClearConnectionDetails()
        {
            CallingAeText.Text = String.Empty;
            CalledAeText.Text = String.Empty;
            RemoteHostText.Text = String.Empty;
            RemotePortText.Text = String.Empty;
        }

        private void DicomPrint(List<String> filelist = null)
        {
            var maxDensity = Convert.ToDouble(MaxDensityText.Text);
            var minDensity = Convert.ToDouble(MinDensityText.Text);

            var client = new PrintClient
            {
                RemoteAddress = RemoteHostText.Text,
                RemotePort = int.Parse(RemotePortText.Text),
                CallingAE = CallingAeText.Text.ToUpper(),
                CalledAE = CalledAeText.Text.ToUpper(),
                NumberOfCopies = int.Parse(CopiesText.Text),
                PrintPriority = ((ComboBoxItem) PriortyComboBox.SelectedItem).Content.ToString(),
                MediumType = ((ComboBoxItem) MediumComboBox.SelectedItem).Content.ToString(),
                FilmDestination = ((ComboBoxItem) DestinationComboBox.SelectedItem).Content.ToString(),
                FilmSessionLabel = DateTime.Now.ToString("yyyyMMdd_hhmmss.fff"),
                OwnerID = "ME",
                ImageDisplayFormat = _format,
                FilmOrientation = ((ComboBoxItem) OrientationComboBox.SelectedItem).Content.ToString(),
                FilmSizeID = ((ComboBoxItem) FilmSizeComboBox.SelectedItem).Content.ToString(),
                MagnificationType = ((ComboBoxItem) MagnificationComboBox.SelectedItem).Content.ToString(),
                MaxDensity = (ushort) maxDensity,
                BorderDensity = ((ComboBoxItem) BorderDensityComboBox.SelectedItem).Content.ToString(),
                MinDensity = (ushort) minDensity,
                EmptyImageDensity = ((ComboBoxItem) EmptyImageDensityComboBox.SelectedItem).Content.ToString(),
                Trim = "NO",
                SmoothingType = _staticRef ? "SMOOTH" : "5", // Fjui = "SMOOTH", Kodac = 5
                Illumination = 2000,
                ReflectedAmbientLight = 10,
                RequestedResolutionID = "STANDARD",
                Polarity = ((ComboBoxItem) PolarityComboBox.SelectedItem).Content.ToString(),
                TrueSize = Convert.ToBoolean(((ComboBoxItem) TrueSizeComboBox.SelectedItem).Content.ToString()),
                BurnInOverlays = Convert.ToBoolean(((ComboBoxItem) OverlaysComboBox.SelectedItem).Content.ToString()),
                Anonymize = Convert.ToBoolean(((ComboBoxItem) AnnonymizeComboBox.SelectedItem).Content.ToString()),
                SpecifyReferenceSequance = _staticRef,
                Files = filelist ?? _filesToOpen
            };
            client.Print();
        }

        #endregion // Private Functions

        #region UI Support

        private void txtCalledAE_GotFocus(object sender, RoutedEventArgs e)
        {
            CalledAeText.SelectAll();
        }

        private void txtRemoteHost_GotFocus(object sender, RoutedEventArgs e)
        {
            RemoteHostText.SelectAll();
        }

        private void txtRemotePort_GotFocus(object sender, RoutedEventArgs e)
        {
            RemotePortText.SelectAll();
        }

        private void txtMinDensity_GotFocus(object sender, RoutedEventArgs e)
        {
            MinDensityText.SelectAll();
        }

        private void txtMaxDensity_GotFocus(object sender, RoutedEventArgs e)
        {
            MaxDensityText.SelectAll();
        }

        private void txtCallingAE_GotFocus(object sender, RoutedEventArgs e)
        {
            CallingAeText.SelectAll();
        }

        #endregion
    }
}
