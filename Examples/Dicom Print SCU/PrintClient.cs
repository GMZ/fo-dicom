using System.Linq;
using Dicom;
using Dicom.Imaging;
using Dicom.IO;
using Dicom.IO.Buffer;
using Dicom.Network;
using Dicom.Printing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Dicom_Print_SCU
{
    public class PrintClient
    {
        #region Private Members

        private FilmBox _currentFilmBox;
        private List<string> _files;
        private string _filmSessionLabel;
        private string _mediumType;
        private int _numberOfCopies;
        private string _filmDestination;
        private string _ownerID;
        private string _printPriority;
        private Int32 _totalPageCount;
        private Int32 _maxPerPage;
        private int _numberofFrames;
        private List<String> _filesPerSheet;

        #endregion

        #region Properties

        public string CallingAE { get; set; }
        public string CalledAE { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Patient detail anonymizeing is enabled in the Overlays.
        /// </summary>
        /// <value>
        ///   <c>true</c> if anonymize; otherwise, <c>false</c>.
        /// </value>
        public bool Anonymize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not overlays will be burnt in.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [burn in overlays]; otherwise, <c>false</c>.
        /// </value>
        public bool BurnInOverlays { get; set; }

        /// <summary>
        ///   Specifies whether minimum pixel values (after VOI LUT transformation) are to printed black or white.
        /// </summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet"></list>
        /// <item>
        ///   <term>NORMAL</term>
        ///   <description>pixels shall be printed as specified by the Photometric Interpretation (0028,0004)</description>
        /// </item>
        /// <item>
        ///   <term>REVERSE</term>
        ///   <description>pixels shall be printed with the opposite polarity as specified by the Photometric 
        ///   Interpretation (0028,0004)</description>
        /// </item>
        /// 
        /// If Polarity (2020,0020) is not specified by the SCU, the SCP shall print with NORMAL polarity.
        /// </remarks>
        public string Polarity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [true size].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [true size]; otherwise, <c>false</c>.
        /// </value>
        public bool TrueSize { get; set; }

        /// <summary>Identification of annotation display format. The definition of the annotation 
        /// display formats and the annotation box position sequence are defined in the Conformance 
        /// Statement.</summary>
        public string AnnotationDisplayFormatID { get; set; }

        /// <summary>Character string that contains either the ID of the printer configuration 
        /// table that contains a set of values for implementation specific print parameters 
        /// (e.g. perception LUT related parameters) or one or more configuration data values, 
        /// encoded as characters. If there are multiple configuration data values encoded in 
        /// the string, they shall be separated by backslashes. The definition of values shall 
        /// be contained in the SCP's Conformance Statement.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="">
        /// <item>
        ///   <term>CS000-CS999</term>
        ///   <description>Implementation specific curve type.</description></item>
        /// </list>
        /// 
        /// Note: It is recommended that for SCPs, CS000 represent the lowest contrast and CS999 
        /// the highest contrast levels available.
        /// </remarks>
        public string ConfigurationInformation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [fuji printer].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [fuji printer]; otherwise, <c>false</c>.
        /// </value>
        public bool SpecifyReferenceSequance { get; set; }

        /// <summary>Type of image display format.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item>
        ///   <term>STANDARD\C,R</term>
        ///   <description>film contains equal size rectangular image boxes with R rows of image 
        ///   boxes and C columns of image boxes; C and R are integers.</description>
        /// </item>
        /// <item>
        ///   <term>ROW\R1,R2,R3, etc.</term>
        ///   <description>film contains rows with equal size rectangular image boxes with R1 
        ///   image boxes in the first row, R2 image boxes in second row, R3 image boxes in third 
        ///   row, etc.; R1, R2, R3, etc. are integers.</description>
        /// </item>
        /// <item>
        ///   <term>COL\C1,C2,C3, etc.</term>
        ///   <description>film contains columns with equal size rectangular image boxes with C1 
        ///   image boxes in the first column, C2 image boxes in second column, C3 image boxes in 
        ///   third column, etc.; C1, C2, C3, etc. are integers.</description>
        /// </item>
        /// <item>
        ///   <term>SLIDE</term>
        ///   <description>film contains 35mm slides; the number of slides for a particular film 
        ///   size is configuration dependent.</description>
        /// </item>
        /// <item>
        ///   <term>SUPERSLIDE</term>
        ///   <description>film contains 40mm slides; the number of slides for a particular film 
        ///   size is configuration dependent.</description>
        /// </item>
        /// <item>
        ///   <term>CUSTOM\i</term>
        ///   <description>film contains a customized ordering of rectangular image boxes; i identifies 
        ///   the image display format; the definition of the image display formats is defined in the 
        ///   Conformance Statement; i is an integer.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string ImageDisplayFormat { get; set; }

        /// <summary>Film orientation.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item>
        ///   <term>PORTRAIT</term>
        ///   <description>vertical film position</description>
        /// </item>
        /// <item>
        ///   <term>LANDSCAPE</term>
        ///   <description>horizontal film position</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string FilmOrientation { get; set; }

        /// <summary> Film size identification.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>8INX10IN</description></item>
        /// <item><description>8_5INX11IN</description></item>
        /// <item><description>10INX12IN</description></item>
        /// <item><description>10INX14IN</description></item>
        /// <item><description>11INX14IN</description></item>
        /// <item><description>11INX17IN</description></item>
        /// <item><description>14INX14IN</description></item>
        /// <item><description>14INX17IN</description></item>
        /// <item><description>24CMX24CM</description></item>
        /// <item><description>24CMX30CM</description></item>
        /// <item><description>A4</description></item>
        /// <item><description>A3</description></item>
        /// </list>
        /// 
        /// Notes:
        /// 10INX14IN corresponds with 25.7CMX36.4CM
        /// A4 corresponds with 210 x 297 millimeters
        /// A3 corresponds with 297 x 420 millimeters
        /// </remarks>
        public string FilmSizeID { get; set; }

        /// <summary>Type of medium on which the print job will be printed.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>PAPER</description></item>
        /// <item><description>CLEAR FILM</description></item>
        /// <item><description>BLUE FILM</description></item>
        /// <item><description>MAMMO CLEAR FILM</description></item>
        /// <item><description>MAMMO BLUE FILM</description></item>
        /// </list>
        /// </remarks>
        public string MediumType
        {
            get { return _mediumType; }
            set
            {
                _mediumType = value;
                FilmSession.MediumType = value;
            }
        }

        /// <summary>Film destination.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item>
        ///   <term>MAGAZINE</term>
        ///   <description>the exposed film is stored in film magazine</description>
        /// </item>
        /// <item>
        ///   <term>PROCESSOR</term>
        ///   <description>the exposed film is developed in film processor</description>
        /// </item>
        /// <item>
        ///   <term>BIN_i</term>
        ///   <description>the exposed film is deposited in a sorter bin where “I” represents the bin 
        ///   number. Film sorter BINs shall be numbered sequentially starting from one and no maxium 
        ///   is placed on the number of BINs. The encoding of the BIN number shall not contain leading
        ///   zeros.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string FilmDestination
        {
            get { return _filmDestination; }
            set
            {
                _filmDestination = value;
                FilmSession.FilmDestination = value;
            }
        }

        /// <summary>Human readable label that identifies the film session.</summary>
        public string FilmSessionLabel
        {
            get { return _filmSessionLabel; }
            set
            {
                _filmSessionLabel = value;
                FilmSession.FilmSessionLabel = value;
            }
        }

        /// <summary>Identification of the owner of the film session.</summary>
        public string OwnerID
        {
            get { return _ownerID; }
            set
            {
                _ownerID = value;
                FilmSession.OwnerId = value;
            }
        }

        /// <summary>Number of copies to be printed for each film of the film session.</summary>
        public int NumberOfCopies
        {
            get { return _numberOfCopies; }
            set
            {
                _numberOfCopies = value;
                FilmSession.NumberOfCopies = value;
            }
        }

        /// <summary>Specifies the priority of the print job.</summary>
        /// <remarks>
        /// Enumerated values:
        /// <list type="bullet">
        /// <item><description>HIGH</description></item>
        /// <item><description>MED</description></item>
        /// <item><description>LOW</description></item>
        /// </list>
        /// </remarks>
        public string PrintPriority
        {
            get { return _printPriority; }
            set
            {
                _printPriority = value;
                FilmSession.PrintPriority = value;
            }
        }

        /// <summary>The BorderDensity Color</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>BLACK</description></item>
        /// <item><description>WHITE</description></item>
        /// </list>
        /// </remarks>
        public string BorderDensity { get; set; }

        /// <summary>Maximum density of the images on the film, expressed in hundredths of 
        /// OD. If Max Density is higher than maximum printer density than Max Density is set 
        /// to maximum printer density.</summary>
        public ushort MaxDensity { get; set; }

        /// <summary>Minimum density of the images on the film, expressed in hundredths of 
        /// OD. If Min Density is lower than minimum printer density than Min Density is set 
        /// to minimum printer density.</summary>
        public ushort MinDensity { get; set; }

        /// <summary>Specifies whether a trim box shall be printed surrounding each image 
        /// on the film.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item><description>YES</description></item>
        /// <item><description>NO</description></item>
        /// </list>
        /// </remarks>
        public string Trim { get; set; }

        /// <summary>Luminance of lightbox illuminating a piece of transmissive film, or for 
        /// the case of reflective media, luminance obtainable from diffuse reflection of the 
        /// illumination present. Expressed as L0, in candelas per square meter (cd/m2).</summary>
        public ushort Illumination { get; set; }

        /// <summary>For transmissive film, luminance contribution due to reflected ambient 
        /// light. Expressed as La, in candelas per square meter (cd/m2).</summary>
        public ushort ReflectedAmbientLight { get; set; }

        /// <summary>Specifies the resolution at which images in this Film Box are to be printed.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item>
        ///   <term>STANDARD</term>
        ///   <description>approximately 4k x 5k printable pixels on a 14 x 17 inch film</description>
        /// </item>
        /// <item>
        ///   <term>HIGH</term>
        ///   <description>Approximately twice the resolution of STANDARD.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string RequestedResolutionID { get; set; }

        /// <summary>The EmptyImageDensity Color</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>BLACK</description></item>
        /// <item><description>WHITE</description></item>
        /// </list>
        /// </remarks>
        public string EmptyImageDensity { get; set; }

        /// <summary>The MagnificationType</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>NONE</description></item>
        /// <item><description>BILINEAR</description></item>
        /// <item><description>CUBIC</description></item>
        /// <item><description>REPLICATE</description></item>
        /// </list>
        /// </remarks>
        public string MagnificationType { get; set; }

        /// <summary>Further specifies the type of the interpolation function. Values 
        /// are defined in Conformance Statement.
        /// 
        /// Only valid for Magnification Type (2010,0060) = CUBIC</summary>
        public string SmoothingType { get; set; }

        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>
        /// The files.
        /// </value>
        public List<string> Files
        {
            get { return _files; }
            set
            {
                _files = value;
                _numberofFrames = _files.Count;
                _maxPerPage = CalculateImagesPerSheet(ImageDisplayFormat);
                GetTotalPageCount();
                
                for (var i = 0; i < _totalPageCount; i++)
                {
                    OpenFilmBox();
                    _filesPerSheet = new List<string>();
                    _filesPerSheet = (from fileNames in _files
                                     select fileNames).Skip(_maxPerPage * i).Take(_maxPerPage).ToList();
                    ParseFiles(_filesPerSheet);
                    CloseFilmBox();

                }
            }
        }

        public FilmSession FilmSession { get; private set; }

        #endregion

        #region Constructor

        public PrintClient()
        {
            _files = new List<string>();
            FilmSession = new FilmSession(DicomUID.BasicFilmSessionSOPClass);
            FilmSession.PresentationLuts.Add(new PresentationLut { PresentationLutShape = "IDENTITY" });
        }

        #endregion

        #region FilmBox Manipulation

        private void OpenFilmBox()
        {
            var filmBox = new FilmBox(FilmSession, null, DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                AnnotationDisplayFormatID = AnnotationDisplayFormatID,
                ConfigurationInformation = ConfigurationInformation,
                BorderDensity = BorderDensity,
                FilmOrientation = FilmOrientation,
                FilmSizeID = FilmSizeID,
                Illumination = Illumination,
                ImageDisplayFormat = ImageDisplayFormat,
                MagnificationType = MagnificationType,
                MaxDensity = MaxDensity,
                MinDensity = MinDensity,
                ReflectedAmbientLight = ReflectedAmbientLight,
                RequestedResolutionID = RequestedResolutionID,
                SmoothingType = SmoothingType,
                Trim = Trim
            };
            if (!SpecifyReferenceSequance)
            {
                filmBox.EmptyImageDensity = EmptyImageDensity;
            }

            filmBox.Initialize();
            FilmSession.BasicFilmBoxes.Add(filmBox);

            _currentFilmBox = filmBox;
        }

        private void CloseFilmBox()
        {
            _currentFilmBox = null;
        }

        #endregion

        #region Image Manipulation

        public void AddImage(Bitmap bitmap, int index)
        {
            if (FilmSession.IsColor)
            {
                AddColorImage(bitmap, index);
            }
            else
            {
                AddGreyscaleImage(bitmap, index);
            }
        }

        private void AddGreyscaleImage(Bitmap bitmap, int index)
        {
            if (_currentFilmBox == null)
                throw new InvalidOperationException("Start film box first!");

            if (index < 0 || index > _currentFilmBox.BasicImageBoxes.Count)
                // ReSharper disable once NotResolvedInText
                throw new ArgumentOutOfRangeException("Image box index out of range");

            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb &&
                bitmap.PixelFormat != PixelFormat.Format32bppArgb &&
                bitmap.PixelFormat != PixelFormat.Format32bppRgb
                )
                throw new ArgumentException("Not supported bitmap format");

            var dataset = new DicomDataset
            {
                {DicomTag.Columns, (ushort) bitmap.Width},
                {DicomTag.Rows, (ushort) bitmap.Height}
            };

            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            pixelData.BitsAllocated = 8;
            pixelData.SamplesPerPixel = 1;
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = (ushort)PixelRepresentation.Unsigned;
            pixelData.PlanarConfiguration = (ushort)PlanarConfiguration.Interleaved;
            pixelData.PhotometricInterpretation = PhotometricInterpretation.Monochrome1;

            var pixels = GetGreyBytes(bitmap);
            var buffer = new MemoryByteBuffer(pixels.Data);

            pixelData.AddFrame(buffer);

            var imageBox = _currentFilmBox.BasicImageBoxes[index];
            imageBox.ImageSequence = dataset;
            imageBox.Polarity = Polarity;

            pixels.Dispose();
        }

        private void AddColorImage(Bitmap bitmap, int index)
        {
            if (_currentFilmBox == null)
                throw new InvalidOperationException("Start film box first!");
            if (index < 0 || index > _currentFilmBox.BasicImageBoxes.Count)
                // ReSharper disable once NotResolvedInText
                throw new ArgumentOutOfRangeException("Image box index out of range");

            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb &&
               bitmap.PixelFormat != PixelFormat.Format32bppArgb &&
               bitmap.PixelFormat != PixelFormat.Format32bppRgb
              )
            {
                throw new ArgumentException("Not supported bitmap format");
            }

            var dataset = new DicomDataset
            {
                {DicomTag.Columns, (ushort) bitmap.Width},
                {DicomTag.Rows, (ushort) bitmap.Height}
            };
            //var dataset = new DicomDataset();
            //dataset.Add<ushort>(DicomTag.Columns, (ushort)bitmap.Width)
            //       .Add<ushort>(DicomTag.Rows, (ushort)bitmap.Height)
            //       .Add<ushort>(DicomTag.BitsAllocated, 8)
            //       .Add<ushort>(DicomTag.BitsStored, 8)
            //       .Add<ushort>(DicomTag.HighBit, 7)
            //       .Add(DicomTag.PixelRepresentation, (ushort)PixelRepresentation.Unsigned)
            //       .Add(DicomTag.PlanarConfiguration, (ushort)PlanarConfiguration.Interleaved)
            //       .Add<ushort>(DicomTag.SamplesPerPixel, 3)
            //       .Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);

            //var pixelData = DicomPixelData.Create(dataset, true);

            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            pixelData.BitsAllocated = 8;
            pixelData.SamplesPerPixel = 3;
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = (ushort)PixelRepresentation.Unsigned;
            pixelData.PlanarConfiguration = (ushort)PlanarConfiguration.Interleaved;
            pixelData.PhotometricInterpretation = PhotometricInterpretation.Rgb;

            var pixels = GetColorbytes(bitmap);
            var buffer = new MemoryByteBuffer(pixels.Data);

            pixelData.AddFrame(buffer);

            var imageBox = _currentFilmBox.BasicImageBoxes[index];
            imageBox.ImageSequence = dataset;

            pixels.Dispose();
        }

        //private Bitmap GenerateBitmap(DicomDataset dataset)
        //{
        //    var dcmImage = new DicomImage(dataset) { OverlayColor = -1 };
        //    var bitmap = dcmImage.RenderImage();
        //    if (BurnInOverlays)
        //    {
        //        var modality = dataset.Get<String>(DicomTag.Modality);

        //        var markers = Anonymize ?
        //            AnnotationText.GenerateAnnotationItems(modality, dataset, true) :
        //            AnnotationText.GenerateAnnotationItems(modality, dataset);

        //        var perentRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        //        var bmp = new Bitmap(bitmap);
        //        var gph = Graphics.FromImage(bmp);

        //        foreach (var annotationItem in markers)
        //        {

        //            var clientRectangle = AnnotationText.CalculateSubRectangle(perentRectangle, annotationItem.AnnotationRect);
        //            // Deflate the client rectangle by 4 pixels to allow some space 
        //            // between neighbouring rectangles whose borders coincide.
        //            Rectangle.Inflate(clientRectangle, -4, -4);

        //            var fontSize = clientRectangle.Height - 1;

        //            //allow p's and q's, etc to extend slightly beyond the bounding rectangle.  Only completely visible lines are shown.
        //            var format = new StringFormat();
        //            format.FormatFlags = StringFormatFlags.NoClip;
        //            format.FormatFlags |= StringFormatFlags.NoWrap;

        //            AnnotationJustification(annotationItem, format);

        //            AnnotationVerticalAlignment(annotationItem, format);

        //            var style = FontStyle.Regular;
        //            if (annotationItem.Bold)
        //            {
        //                style |= FontStyle.Bold;
        //            }
        //            if (annotationItem.Italics)
        //            {
        //                style |= FontStyle.Italic;
        //            }

        //            // Font
        //            var font = CreateFont("Century Gothic", fontSize, style, GraphicsUnit.Pixel);

        //            // Drop Shadow
        //            clientRectangle.Offset(1, 1);
        //            gph.DrawString(annotationItem.AnnotationText, font, Brushes.Black, clientRectangle, format);

        //            // Foreground Colour
        //            clientRectangle.Offset(-1, -1);
        //            gph.DrawString(annotationItem.AnnotationText, font, Brushes.White, clientRectangle, format);
        //            font.Dispose();
        //        }
        //        // Clean Up
        //        gph.Dispose();
        //        return bmp;
        //    }
        //    return (Bitmap)bitmap;
        //}

        private void ParseFiles(IList<string> files)
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var df = DicomFile.Open(file);
                var filmBmp = ImageHelper.GenerateBitmap(df.Dataset, BurnInOverlays, Anonymize);
                AddImage(filmBmp, i);
                filmBmp.Dispose();
            }
        }

        #endregion

        #region Dicom Print

        public void Print()
        {
            var dicomClient = new DicomClient();
            dicomClient.AddRequest(new DicomNCreateRequest(FilmSession.SOPClassUID, FilmSession.SOPInstanceUID, 0)
            {
                Dataset = FilmSession
            });


            foreach (var filmbox in FilmSession.BasicFilmBoxes)
            {

                var imageBoxRequests = new List<DicomNSetRequest>();

                var filmBoxRequest = new DicomNCreateRequest(FilmBox.SOPClassUID, filmbox.SOPInstanceUID, 0)
                {
                    Dataset = filmbox,
                    OnResponseReceived = (request, response) =>
                    {
                        if (response.HasDataset)
                        {
                            var seq = response.Dataset.Get<DicomSequence>(DicomTag.ReferencedImageBoxSequence);
                            for (var i = 0; i < seq.Items.Count; i++)
                            {
                                var req = imageBoxRequests[i];
                                var imageBox = req.Dataset;
                                var sopInstanceUid = seq.Items[i].Get<string>(DicomTag.ReferencedSOPInstanceUID);
                                imageBox.Add(DicomTag.SOPInstanceUID, sopInstanceUid);
                                req.Command.Add(DicomTag.RequestedSOPInstanceUID, sopInstanceUid);
                            }
                        }
                    }
                };
                dicomClient.AddRequest(filmBoxRequest);



                foreach (
                    var req in
                        filmbox.BasicImageBoxes.Select(
                            image => new DicomNSetRequest(image.SOPClassUID, image.SOPInstanceUID)
                            {
                                Dataset = image
                            }))
                {
                    imageBoxRequests.Add(req);
                    dicomClient.AddRequest(req);
                }
            }

            dicomClient.AddRequest(new DicomNActionRequest(FilmSession.SOPClassUID, FilmSession.SOPInstanceUID, 0x0001));

            dicomClient.Send(RemoteAddress, RemotePort, false, CallingAE, CalledAE);
        }

        #endregion
        
        #region Private Methods

        private static unsafe PinnedByteArray GetGreyBytes(Bitmap bitmap)
        {
            var pixels = new PinnedByteArray(bitmap.Width * bitmap.Height);

            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var srcComponents = bitmap.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;

            var dstLine = (byte*)pixels.Pointer;
            var srcLine = (byte*)data.Scan0.ToPointer();

            for (int i = 0; i < data.Height; i++)
            {
                for (int j = 0; j < data.Width; j++)
                {
                    var pixel = srcLine + j * srcComponents;
                    var grey = (int)(pixel[0] * 0.3 + pixel[1] * 0.59 + pixel[2] * 0.11);
                    dstLine[j] = (byte)grey;
                }

                srcLine += data.Stride;
                dstLine += data.Width;
            }
            bitmap.UnlockBits(data);

            return pixels;
        }

        private static unsafe PinnedByteArray GetColorbytes(Bitmap bitmap)
        {
            var pixels = new PinnedByteArray(bitmap.Width * bitmap.Height * 3);

            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var srcComponents = bitmap.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;

            var dstLine = (byte*)pixels.Pointer;
            var srcLine = (byte*)data.Scan0.ToPointer();

            for (int i = 0; i < data.Height; i++)
            {
                for (int j = 0; j < data.Width; j++)
                {
                    var srcPixel = srcLine + j * srcComponents;
                    var dstPixel = dstLine + j * 3;

                    //convert from bgr to rgb
                    dstPixel[0] = srcPixel[2];
                    dstPixel[1] = srcPixel[1];
                    dstPixel[2] = srcPixel[0];
                }

                srcLine += data.Stride;
                dstLine += data.Width * 3;
            }
            bitmap.UnlockBits(data);

            return pixels;
        }

        private void GetTotalPageCount()
        {
            _totalPageCount = ((Int32)Math.Ceiling(_numberofFrames / (Double)_maxPerPage)) == 0
                                 ? 1
                                 : (Int32)Math.Ceiling(_numberofFrames / (Double)_maxPerPage);
        }

        private static int CalculateImagesPerSheet(String format)
        {
            int cols = 0, rows = 0;

            if (String.IsNullOrEmpty(format))
                return 0;

            var parts = format.Split('\\');
            if (parts[0].ToUpper() == "STANDARD" && parts.Length == 2)
            {
                parts = parts[1].Split(',');
                if (parts.Length == 2)
                {
                    cols = int.Parse(parts[0]);
                    rows = int.Parse(parts[1]);
                }
            }

            return cols * rows;
        }

        //private static void AnnotationVerticalAlignment(AnnotationItem annotationItem, StringFormat format)
        //{
        //    switch (annotationItem.VerticalAlignment)
        //    {
        //        case AnnotationItem.VAlignment.Top:
        //            format.LineAlignment = StringAlignment.Near;
        //            break;
        //        case AnnotationItem.VAlignment.Center:
        //            format.LineAlignment = StringAlignment.Center;
        //            break;
        //        default:
        //            format.LineAlignment = StringAlignment.Far;
        //            break;
        //    }
        //}

        //private static void AnnotationJustification(AnnotationItem annotationItem, StringFormat format)
        //{
        //    switch (annotationItem.Justification)
        //    {
        //        case AnnotationItem.HAlignment.Right:
        //            format.Alignment = StringAlignment.Far;
        //            break;
        //        case AnnotationItem.HAlignment.Center:
        //            format.Alignment = StringAlignment.Center;
        //            break;
        //        default:
        //            format.Alignment = StringAlignment.Near;
        //            break;
        //    }
        //}

        //private static Font CreateFont(string fontName, float fontSize, FontStyle fontStyle, GraphicsUnit graphicsUnit)
        //{
        //    try
        //    {
        //        return new Font(fontName, fontSize, fontStyle, graphicsUnit);
        //    }
        //    catch
        //    {
        //        return new Font("Arial", 3, fontStyle, graphicsUnit);
        //    }
        //}

        #endregion
    }
}
