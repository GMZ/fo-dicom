using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Dicom;
using Dicom.Imaging;
using Dicom.Printing;

namespace Dicom_Print_SCP
{
    public class PrintJob2 : DicomDataset
    {
        private PrintPreviewDialog _previewDialog;

        #region Properties and Attributes

        //border in 100th of inches
        protected const float Hundredths = (float)(100 * 2 / 25.4);

        public bool SendNEventReport { get; set; }

        public Printer Printer { get; private set; }

        public FilmSession Session { get; private set; } //CH

        public List<FilmBox> FilmBoxList { get; private set; } //CH

        public PrintJobStatus Status { get; private set; }

        public string PrintJobFolder { get; private set; }

        public string FullPrintJobFolder { get; private set; }

        public Exception Error { get; private set; }

        private int _currentPage;
        private FilmBox _currentFilmBox;

        /// <summary>
        /// Print job SOP class UID
        /// </summary>
        public readonly DicomUID SOPClassUID = DicomUID.PrintJobSOPClass;

        /// <summary>
        /// Print job SOP instance UID
        /// </summary>
        public DicomUID SOPInstanceUID { get; private set; }

        /// <summary>
        /// Execution status of print job.
        /// </summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item><description>PENDING</description></item>
        /// <item><description>PRINTING</description></item>
        /// <item><description>DONE</description></item>
        /// <item><description>FAILURE</description></item>
        /// </list>
        /// </remarks> 
        public string ExecutionStatus
        {
            get { return Get(DicomTag.ExecutionStatus, string.Empty); }
            set { Add(DicomTag.ExecutionStatus, value); }
        }

        /// <summary>
        /// Additional information about Execution Status (2100,0020).
        /// </summary>
        public string ExecutionStatusInfo
        {
            get { return Get(DicomTag.ExecutionStatusInfo, string.Empty); }
            set { Add(DicomTag.ExecutionStatusInfo, value); }
        }

        /// <summary>
        /// Specifies the priority of the print job.
        /// </summary>
        /// <remarks>
        /// Enumerated values:
        /// <list type="bullet">
        ///     <item><description>HIGH</description></item>
        ///     <item><description>MED</description></item>
        ///     <item><description>LOW</description></item>
        /// </list>
        /// </remarks>
        public string PrintPriority
        {
            get { return Get(DicomTag.PrintPriority, "MED"); }
            set { Add(DicomTag.PrintPriority, value); }
        }

        /// <summary>
        /// Date/Time of print job creation.
        /// </summary>
        public DateTime CreationDateTime
        {
            get { return this.GetDateTime(DicomTag.CreationDate, DicomTag.CreationTime); }
            set
            {
                Add(DicomTag.CreationDate, value);
                Add(DicomTag.CreationTime, value);
            }
        }

        /// <summary>
        /// User defined name identifying the printer.
        /// </summary>
        public string PrinterName
        {
            get { return Get(DicomTag.PrinterName, string.Empty); }
            set { Add(DicomTag.PrinterName, value); }
        }

        /// <summary>
        /// DICOM Application Entity Title that issued the print operation.
        /// </summary>
        public string Originator
        {
            get { return Get(DicomTag.Originator, string.Empty); }
            set { Add(DicomTag.Originator, value); }
        }

        public Dicom.Log.Logger Log { get; private set; }

        public IList<string> FilmBoxFolderList { get; private set; }

        public event EventHandler<StatusUpdateEventArgs> StatusUpdate;
        #endregion

        #region Constructors

        /// <summary>
        /// Construct new print job using specified SOP instance UID. If passed SOP instance UID is missing, new UID will
        /// be generated
        /// </summary>
        /// <param name="sopInstance">New print job SOP instance uID</param>
        /// <param name="printer">The printer.</param>
        /// <param name="originator">The originator.</param>
        /// <param name="log">The log.</param>
        /// <param name="session">The session.</param>/
        /// <exception cref="System.ArgumentNullException">printer</exception>
        public PrintJob2(DicomUID sopInstance, Printer printer, string originator, Dicom.Log.Logger log, FilmSession session)
        {
            if (printer == null)
            {
                throw new ArgumentNullException("printer");
            }

            Log = log;

            if (sopInstance == null || sopInstance.UID == string.Empty)
                SOPInstanceUID = DicomUID.Generate();
            else
                SOPInstanceUID = sopInstance;

            Add(DicomTag.SOPClassUID, SOPClassUID);
            Add(DicomTag.SOPInstanceUID, SOPInstanceUID);

            Printer = printer;

            Status = PrintJobStatus.Pending;

            PrinterName = Printer.PrinterAet;
            Session = session;

            Originator = originator;

            if (CreationDateTime == DateTime.MinValue)
            {
                CreationDateTime = DateTime.Now;
            }

            PrintJobFolder = SOPInstanceUID.UID;

            var receivingFolder = Environment.CurrentDirectory + @"\PrintJobs";

            FullPrintJobFolder = string.Format(@"{0}\{1}", receivingFolder.TrimEnd('\\'), PrintJobFolder);
            FilmBoxFolderList = new List<string>();
        }

        #endregion

        #region Printing Methods

        public void Print(List<FilmBox> filmBoxList)
        {

            FilmBoxList = filmBoxList;
            try
            {
                Status = PrintJobStatus.Pending;

                OnStatusUpdate("Preparing films for printing");

                var printJobDir = new System.IO.DirectoryInfo(FullPrintJobFolder);
                if (!printJobDir.Exists)
                {
                    printJobDir.Create();
                }

                int filmsCount = FilmBoxFolderList.Count;
                for (int i = 0; i < filmBoxList.Count; i++)
                {
                    var filmBox = filmBoxList[i];
                    var filmBoxDir = printJobDir.CreateSubdirectory(string.Format("F{0:000000}", i + 1 + filmsCount));

                    var file = new DicomFile(filmBox.FilmSession);
                    file.Save(string.Format(@"{0}\FilmSession.dcm", filmBoxDir.FullName));

                    FilmBoxFolderList.Add(filmBoxDir.Name);
                    filmBox.Save(filmBoxDir.FullName);
                }

                //FilmSessionLabel = filmBoxList.First().FilmSession.FilmSessionLabel;

                var thread = new Thread(DoPrint)
                {
                    Name = string.Format("PrintJob {0}", SOPInstanceUID.UID),
                    IsBackground = true
                };
                thread.Start();
            }
            catch (Exception ex)
            {
                Error = ex;
                Status = PrintJobStatus.Failure;
                OnStatusUpdate("Print failed");
                DeletePrintFolder();
            }
        }

        private void DoPrint()
        {
            PrintDocument printDocument = null;
            try
            {
                Status = PrintJobStatus.Printing;
                OnStatusUpdate("Printing Started");

                var printerSettings = new PrinterSettings
                {
                    PrinterName = "Microsoft XPS Document Writer",
                    PrintToFile = true,
                    PrintFileName = string.Format("{0}\\{1}.xps", FullPrintJobFolder, SOPInstanceUID.UID)
                };

                printDocument = new PrintDocument
                {
                    PrinterSettings = printerSettings,
                    DocumentName = Thread.CurrentThread.Name,
                    PrintController = new StandardPrintController()
                };

                printDocument.PrinterSettings.Collate = true;
                printDocument.PrinterSettings.Copies = (short)Session.NumberOfCopies;
                printDocument.QueryPageSettings += OnQueryPageSettings;
                printDocument.PrintPage += OnPrintPage;

                //PreviewProc(printDocument);
                printDocument.Print();

                Status = PrintJobStatus.Done;

                OnStatusUpdate("Printing Done");
            }
            catch
            {
                Status = PrintJobStatus.Failure;
                OnStatusUpdate("Printing failed");
            }
            finally
            {
                if (printDocument != null)
                {
                    //dispose the print document and unregister events handlers to avoid memory leaks
                    printDocument.QueryPageSettings -= OnQueryPageSettings;
                    printDocument.PrintPage -= OnPrintPage;
                    printDocument.Dispose();
                }
            }
        }

        private void PreviewProc(PrintDocument document)
        {
            try
            {

                _previewDialog = new PrintPreviewDialog
                {
                    Text = @"DICOM Print Preview",
                    ShowInTaskbar = true,
                    WindowState = FormWindowState.Maximized,
                    Document = document
                };
                _previewDialog.FormClosed += OnPreviewDialogOnFormClosed;
                _previewDialog.KeyDown += PreviewDialogOnKeyDown;
                _previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
            }
        }
        
        private void PreviewDialogOnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.KeyCode == Keys.Escape)
            {
                _previewDialog.Close();
            }
        }

        private void OnPreviewDialogOnFormClosed(object sender, FormClosedEventArgs args)
        {
            _previewDialog = null;
        }

        void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.InterpolationMode = _currentFilmBox.MagnificationType == "CUBIC"
                ? InterpolationMode.HighQualityBicubic
                : InterpolationMode.HighQualityBilinear;
            
            var format = _currentFilmBox.ImageDisplayFormat;

            if (String.IsNullOrEmpty(format))
                return;

            var parts = _currentFilmBox.ImageDisplayFormat.Split('\\', ',');

            if (parts.Length >= 3)
            {
                int columns = int.Parse(parts[1]);
                int rows = int.Parse(parts[2]);

                var boxSize = new SizeF(e.MarginBounds.Width / columns, e.MarginBounds.Height / rows);


                var boxes = new List<RectangleF>();
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {

                        boxes.Add(new RectangleF
                        {
                            X = e.MarginBounds.X + c * boxSize.Width,
                            Y = e.MarginBounds.Y + r * boxSize.Height,
                            Width = boxSize.Width,
                            Height = boxSize.Height
                        });
                    }
                }

                for (int i = 0; i < _currentFilmBox.BasicImageBoxes.Count; i++)
                {
                    DrawImageBox(_currentFilmBox.BasicImageBoxes[i], e.Graphics, boxes[i], 100);
                }
            }

            _currentFilmBox = null;
            _currentPage++;

            e.HasMorePages = _currentPage < FilmBoxList.Count;
            
        }

        void OnQueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
            OnStatusUpdate(string.Format("Printing film {0} of {1}", _currentPage + 1, FilmBoxList.Count));
            _currentFilmBox = FilmBoxList[_currentPage];

            e.PageSettings.Margins.Left = 25;
            e.PageSettings.Margins.Right = 25;
            e.PageSettings.Margins.Top = 25;
            e.PageSettings.Margins.Bottom = 25;

            e.PageSettings.Landscape = _currentFilmBox.FilmOrientation == "LANDSCAPE";

        }

        private void DeletePrintFolder()
        {
            var folderInfo = new System.IO.DirectoryInfo(FullPrintJobFolder);
            if (folderInfo.Exists)
            {
                folderInfo.Delete(true);
            }
        }

        public void DrawImageBox(ImageBox imgBox, Graphics graphics, RectangleF box, int imageResolution)
        {
            var imageSequence = imgBox.ImageSequence;
            var state = graphics.Save();

            FillBox(box, graphics);

            var imageBox = box;
            if (_currentFilmBox.Trim == "YES")
            {
                imageBox.Inflate(-Hundredths, -Hundredths);
            }

            if (imageSequence != null && imageSequence.Contains(DicomTag.PixelData))
            {
                Image bitmap = null;
                try
                {
                    var image = new DicomImage(imageSequence);
                    var frame = image.RenderImage();

                    bitmap = frame;
                    if (imgBox.Polarity == "REVERSE")
                    {
                        bitmap = Transform((Bitmap)bitmap);
                    }

                    DrawBitmap(graphics, box, bitmap, imageResolution);
                }
                finally
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }

            graphics.Restore(state);
        }

        private void FillBox(RectangleF box, Graphics graphics)
        {
            if (_currentFilmBox.EmptyImageDensity == "BLACK")
            {
                var fillBox = box;
                if (_currentFilmBox.BorderDensity == "WHITE" && _currentFilmBox.Trim == "YES")
                {
                    fillBox.Inflate(-Hundredths, -Hundredths);
                }
                using (var brush = new SolidBrush(Color.Black))
                {
                    graphics.FillRectangle(brush, fillBox);
                }
            }
        }

        private void DrawBitmap(Graphics graphics, RectangleF box, Image bitmap, int imageResolution)
        {
            // ReSharper disable PossibleLossOfFraction
            var imageSizeInInch = new SizeF(100 * bitmap.Width / imageResolution, 100 * bitmap.Height / imageResolution);
            // ReSharper restore PossibleLossOfFraction
            double factor = Math.Min(box.Height / imageSizeInInch.Height, box.Width / imageSizeInInch.Width);

            if (factor > 1)
            {
                var targetSize = new Size
                {
                    Width = (int)(imageResolution * box.Width / 100),
                    Height = (int)(imageResolution * box.Height / 100)
                };
                

                using (var membmp = new Bitmap(targetSize.Width, targetSize.Height))
                {
                    membmp.SetResolution(imageResolution, imageResolution);

                    using (var memg = Graphics.FromImage(membmp))
                    {

                        memg.InterpolationMode = InterpolationMode.Bicubic;
                        memg.SmoothingMode = SmoothingMode.AntiAlias;

                        if (_currentFilmBox.EmptyImageDensity == "BLACK")
                        {
                            using (var brush = new SolidBrush(Color.Black))
                            {
                                memg.FillRectangle(brush, 0, 0, targetSize.Width, targetSize.Height);
                            }
                        }

                        factor = Math.Min(targetSize.Height / (double)bitmap.Height,
                            targetSize.Width / (double)bitmap.Width);

                        var srcRect = new RectangleF(0, 0, bitmap.Width, bitmap.Height);
                        var dstRect = new RectangleF
                        {
                            X = (float)((targetSize.Width - bitmap.Width * factor) / 2.0f),
                            Y = (float)((targetSize.Height - bitmap.Height * factor) / 2.0f),
                            Width = (float)(bitmap.Width * factor),
                            Height = (float)(bitmap.Height * factor),
                        };
                        memg.DrawImage(bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
                    }
                    graphics.DrawImage(membmp, box.X, box.Y, box.Width, box.Height);
                }
            }
            else
            {
                var dstRect = new RectangleF
                {
                    X = box.X + (float)(box.Width - imageSizeInInch.Width * factor) / 2.0f,
                    Y = box.Y + (float)(box.Height - imageSizeInInch.Height * factor) / 2.0f,
                    Width = (float)(imageSizeInInch.Width * factor),
                    Height = (float)(imageSizeInInch.Height * factor),
                };

                graphics.DrawImage(bitmap, dstRect);
            }

        }

        public Bitmap Transform(Bitmap source)
        {
            //create a blank bitmap the same size as original
            var newBitmap = new Bitmap(source.Width, source.Height);

            //get a graphics object from the new image
            var g = Graphics.FromImage(newBitmap);

            // create the negative color matrix
            var colorMatrix = new ColorMatrix(new[]
            {
                new float[] {-1, 0, 0, 0, 0},
                new float[] {0, -1, 0, 0, 0},
                new float[] {0, 0, -1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {1, 1, 1, 0, 1}
            });

            // create some image attributes
            var attributes = new ImageAttributes();

            attributes.SetColorMatrix(colorMatrix);

            g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                        0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();

            return newBitmap;
        }
        #endregion

        #region Notification Methods

        protected virtual void OnStatusUpdate(string info)
        {
            ExecutionStatus = Status.ToString();
            ExecutionStatusInfo = info;

            if (Status != PrintJobStatus.Failure)
            {
                Log.Info("Print Job {0} Status {1}: {2}", SOPInstanceUID.UID.Split('.').Last(), Status, info);
            }
            else
            {
                Log.Error("Print Job {0} Status {1}: {2}", SOPInstanceUID.UID.Split('.').Last(), Status, info);
            }
            if (StatusUpdate != null)
            {
                var args = new StatusUpdateEventArgs((ushort)Status, info, Session.FilmSessionLabel, PrinterName);
                StatusUpdate(this, args);
            }
        }

        #endregion

    }
}
