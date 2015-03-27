using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dicom;
using Dicom.Imaging;
using Dicom.Printing;
using Brushes = System.Drawing.Brushes;
using FontStyle = System.Drawing.FontStyle;

namespace Dicom_Print_SCU
{
    internal class ImageHelper
    {
        public static Bitmap GenerateBitmap(DicomDataset dataset, bool burnInOverlays = true, bool anonymize = false)
        {
            var dcmImage = new DicomImage(dataset) { OverlayColor = -1 };
            var bitmap = dcmImage.RenderImage();
            if (burnInOverlays)
            {
                var modality = dataset.Get<String>(DicomTag.Modality);

                var markers = anonymize ?
                    AnnotationText.GenerateAnnotationItems(modality, dataset, true) :
                    AnnotationText.GenerateAnnotationItems(modality, dataset);

                var perentRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                var bmp = new Bitmap(bitmap);
                var gph = Graphics.FromImage(bmp);

                foreach (var annotationItem in markers)
                {

                    var clientRectangle = AnnotationText.CalculateSubRectangle(perentRectangle, annotationItem.AnnotationRect);
                    // Deflate the client rectangle by 4 pixels to allow some space 
                    // between neighbouring rectangles whose borders coincide.
                    Rectangle.Inflate(clientRectangle, -4, -4);

                    var fontSize = clientRectangle.Height - 1;

                    //allow p's and q's, etc to extend slightly beyond the bounding rectangle.  Only completely visible lines are shown.
                    var format = new StringFormat();
                    format.FormatFlags = StringFormatFlags.NoClip;
                    format.FormatFlags |= StringFormatFlags.NoWrap;

                    AnnotationJustification(annotationItem, format);

                    AnnotationVerticalAlignment(annotationItem, format);

                    var style = FontStyle.Regular;
                    if (annotationItem.Bold)
                    {
                        style |= FontStyle.Bold;
                    }
                    if (annotationItem.Italics)
                    {
                        style |= FontStyle.Italic;
                    }

                    // Font
                    var font = CreateFont("Century Gothic", fontSize, style, GraphicsUnit.Pixel);

                    // Drop Shadow
                    clientRectangle.Offset(1, 1);
                    gph.DrawString(annotationItem.AnnotationText, font, Brushes.Black, clientRectangle, format);

                    // Foreground Colour
                    clientRectangle.Offset(-1, -1);
                    gph.DrawString(annotationItem.AnnotationText, font, Brushes.White, clientRectangle, format);
                    font.Dispose();
                }
                // Clean Up
                gph.Dispose();
                return bmp;
            }
            return (Bitmap)bitmap;
        }

        public static ImageSource GenerateImageSource(DicomDataset dataset, bool burnInOverlays = true, bool anonymize = false)
        {
            using (var bmp = GenerateBitmap(dataset, burnInOverlays, anonymize))
            {
                return Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
            }
        }

        private static void AnnotationVerticalAlignment(AnnotationItem annotationItem, StringFormat format)
        {
            switch (annotationItem.VerticalAlignment)
            {
                case AnnotationItem.VAlignment.Top:
                    format.LineAlignment = StringAlignment.Near;
                    break;
                case AnnotationItem.VAlignment.Center:
                    format.LineAlignment = StringAlignment.Center;
                    break;
                default:
                    format.LineAlignment = StringAlignment.Far;
                    break;
            }
        }

        private static void AnnotationJustification(AnnotationItem annotationItem, StringFormat format)
        {
            switch (annotationItem.Justification)
            {
                case AnnotationItem.HAlignment.Right:
                    format.Alignment = StringAlignment.Far;
                    break;
                case AnnotationItem.HAlignment.Center:
                    format.Alignment = StringAlignment.Center;
                    break;
                default:
                    format.Alignment = StringAlignment.Near;
                    break;
            }
        }

        private static Font CreateFont(string fontName, float fontSize, FontStyle fontStyle, GraphicsUnit graphicsUnit)
        {
            try
            {
                return new Font(fontName, fontSize, fontStyle, graphicsUnit);
            }
            catch
            {
                return new Font("Arial", 3, fontStyle, graphicsUnit);
            }
        }
    }
}
