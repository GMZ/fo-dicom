using System;
using System.Collections.Generic;
using System.Drawing;
using Dicom.Imaging.Mathematics;

namespace Dicom.Printing
{
    /// <summary>
    /// Creates a list of <see cref="AnnotationItem"/>s that are populated from the <see cref="DicomDataset"/>
    /// </summary>
    public class AnnotationText
    {
        #region Constructor

        /// <summary>
        /// Prevents a default instance of the <see cref="AnnotationText"/> class from being created.
        /// </summary>
        private AnnotationText() { }

        #endregion

        #region Generate Text Annotation

        /// <summary>
        /// Generates the annotation items.
        /// </summary>
        /// <param name="modality">The modality type.</param>
        /// <param name="currentDataSet">The current DICOM data set.</param>
        /// <param name="anonymize"></param>
        /// <returns></returns>
        public static List<AnnotationItem> GenerateAnnotationItems(string modality, DicomDataset currentDataSet, Boolean anonymize = false)
        {
            // TODO turn into an Interface and use this as the Defualt implimentation
            // TODO Replace hard coded Rectangle positions with an XML setting file for each Modality

            var list = new List<AnnotationItem>();
            RectangleF normalizedRectangle;

            switch (modality.ToUpper())
            {
                // Markers for CT Images
                case "CT":

                    #region Top Left

                    GenerateRectangle("0.000000\\0.000000\\0.400000\\0.025000", out normalizedRectangle);
                    var tl1 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get(DicomTag.Manufacturer, String.Empty));
                    list.Add(tl1);
                    GenerateRectangle("0.000000\\0.025000\\0.500000\\0.050000", out normalizedRectangle);
                    var tl2 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get(DicomTag.ManufacturerModelName, String.Empty));
                    list.Add(tl2);
                    GenerateRectangle("0.000000\\0.050000\\0.500000\\0.075000", out normalizedRectangle);
                    var tl3 = new AnnotationItem(normalizedRectangle, GetDescription(currentDataSet));
                    list.Add(tl3);
                    GenerateRectangle("0.000000\\0.075000\\0.500000\\0.100000", out normalizedRectangle);
                    var tv4 = new AnnotationItem(normalizedRectangle, GetSeriesNo(currentDataSet));
                    list.Add(tv4);
                    GenerateRectangle("0.000000\\0.100000\\0.500000\\0.125000", out normalizedRectangle);
                    var tl5 = new AnnotationItem(normalizedRectangle, GetInstanceNo(currentDataSet));
                    list.Add(tl5);
                    GenerateRectangle("0.000000\\0.125000\\0.500000\\0.150000", out normalizedRectangle);
                    var tl6 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get(DicomTag.PositionReferenceIndicator, String.Empty));
                    list.Add(tl6);
                    GenerateRectangle("0.000000\\0.150000\\0.500000\\0.175000", out normalizedRectangle);
                    var tl7 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get(DicomTag.ImageComments, String.Empty));
                    list.Add(tl7);

                    #endregion //Top Left

                    #region Bottom Left

                    GenerateRectangle("0.000000\\0.850000\\0.500000\\0.875000", out normalizedRectangle);
                    var bl6 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.KVP) + "kv");
                    list.Add(bl6);
                    GenerateRectangle("0.000000\\0.875000\\0.500000\\0.900000", out normalizedRectangle);
                    var bl5 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.XRayTubeCurrent) + "mA");
                    list.Add(bl5);
                    GenerateRectangle("0.000000\\0.900000\\0.500000\\0.925000", out normalizedRectangle);
                    var bl4 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.ExposureTime) + "ms");
                    list.Add(bl4);
                    GenerateRectangle("0.000000\\0.925000\\0.500000\\0.950000", out normalizedRectangle);
                    var bl3 = new AnnotationItem(normalizedRectangle, SliceThicknessAndSpacing(currentDataSet));
                    list.Add(bl3);
                    GenerateRectangle("0.000000\\0.950000\\0.500000\\0.975000", out normalizedRectangle);
                    var bl2 = new AnnotationItem(normalizedRectangle,
                                                 "Tilt:" + currentDataSet.Get<String>(DicomTag.GantryDetectorTilt));
                    list.Add(bl2);
                    GenerateRectangle("0.000000\\0.975000\\0.400000\\1.000000", out normalizedRectangle);
                    var bl1 = new AnnotationItem(normalizedRectangle, GetWidthAndLevel(currentDataSet))
                    {
                        Bold = true
                    };
                    list.Add(bl1);

                    #endregion //Bottom Left

                    #region Top Right

                    GenerateRectangle("0.600000\\0.000000\\1.000000\\0.025000", out normalizedRectangle);
                    var tr1 = new AnnotationItem(normalizedRectangle, currentDataSet.Get<String>(DicomTag.InstitutionName))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(tr1);
                    GenerateRectangle("0.500000\\0.025000\\1.000000\\0.050000", out normalizedRectangle);
                    var tr2 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 currentDataSet.Get<String>(DicomTag.PatientName).Replace("^", ","))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(tr2);
                    GenerateRectangle("0.500000\\0.050000\\1.000000\\0.075000", out normalizedRectangle);
                    var tr3 = new AnnotationItem(normalizedRectangle, anonymize ?
                                             "Anonymised" :
                                             GetDobAndSex(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(tr3);
                    GenerateRectangle("0.500000\\0.075000\\1.000000\\0.100000", out normalizedRectangle);
                    var tr4 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "MRN:" + currentDataSet.Get<String>(DicomTag.PatientID))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true,
                        Italics = true
                    };
                    list.Add(tr4);
                    GenerateRectangle("0.500000\\0.100000\\1.000000\\0.125000", out normalizedRectangle);
                    var tr5 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "Acc:" + currentDataSet.Get<String>(DicomTag.AccessionNumber))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(tr5);
                    GenerateRectangle("0.500000\\0.125000\\1.000000\\0.150000", out normalizedRectangle);
                    var tr6 = new AnnotationItem(normalizedRectangle, GetAcquisitionTime(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(tr6);
                    GenerateRectangle("0.500000\\0.150000\\1.000000\\0.175000", out normalizedRectangle);
                    var tr7 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.Columns) + "x" +
                                                 currentDataSet.Get<String>(DicomTag.Rows))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(tr7);
                    GenerateRectangle("0.500000\\0.175000\\1.000000\\0.200000", out normalizedRectangle);
                    var tr8 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.ConvolutionKernel))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(tr8);

                    #endregion //Top Right

                    #region Bottom Right

                    GenerateRectangle("0.500000\\0.950000\\1.000000\\0.975000", out normalizedRectangle);
                    var br2 = new AnnotationItem(normalizedRectangle,
                                                 "PP: " + currentDataSet.Get<String>(DicomTag.PatientPosition))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(br2);
                    GenerateRectangle("0.600000\\0.975000\\1.000000\\1.000000", out normalizedRectangle);
                    var br1 = new AnnotationItem(normalizedRectangle, GetFieldOfView(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(br1);

                    #endregion //Bottom Right

                    break;
                // Markers for Mamo
                case "MG":

                    #region Top Left

                    GenerateRectangle("0.000000\\0.000000\\0.400000\\0.025000", out normalizedRectangle);
                    var mgTl1 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.Manufacturer));
                    list.Add(mgTl1);
                    GenerateRectangle("0.000000\\0.025000\\0.500000\\0.050000", out normalizedRectangle);
                    var mgTl2 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ManufacturerModelName));
                    list.Add(mgTl2);
                    GenerateRectangle("0.000000\\0.050000\\0.500000\\0.075000", out normalizedRectangle);
                    var mgTl3 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.StationName));
                    list.Add(mgTl3);
                    GenerateRectangle("0.000000\\0.075000\\0.500000\\0.100000", out normalizedRectangle);
                    var mgTl4 = new AnnotationItem(normalizedRectangle, GetDescription(currentDataSet));
                    list.Add(mgTl4);
                    GenerateRectangle("0.000000\\0.100000\\0.500000\\0.125000", out normalizedRectangle);
                    var mgTl5 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.BodyPartExamined));
                    list.Add(mgTl5);
                    GenerateRectangle("0.000000\\0.125000\\0.500000\\0.150000", out normalizedRectangle);
                    var mgTl6 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ViewPosition));
                    list.Add(mgTl6);
                    GenerateRectangle("0.000000\\0.150000\\0.500000\\0.175000", out normalizedRectangle);
                    var mgTl7 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.CassetteOrientation));
                    list.Add(mgTl7);
                    GenerateRectangle("0.000000\\0.175000\\0.500000\\0.200000", out normalizedRectangle);
                    var mgTl8 = new AnnotationItem(normalizedRectangle, GetSeriesNo(currentDataSet));
                    list.Add(mgTl8);
                    GenerateRectangle("0.000000\\0.225000\\0.500000\\0.250000", out normalizedRectangle);
                    var mgTl9 = new AnnotationItem(normalizedRectangle, GetInstanceNo(currentDataSet));
                    list.Add(mgTl9);
                    GenerateRectangle("0.000000\\0.225000\\0.500000\\0.250000", out normalizedRectangle);
                    var mgTl10 = new AnnotationItem(normalizedRectangle,
                                                    currentDataSet.Get<String>(DicomTag.Laterality));
                    list.Add(mgTl10);
                    GenerateRectangle("0.000000\\0.250000\\0.500000\\0.275000", out normalizedRectangle);
                    var mgTl11 = new AnnotationItem(normalizedRectangle,
                                                    currentDataSet.Get<String>(DicomTag.PositionReferenceIndicator));
                    list.Add(mgTl11);
                    GenerateRectangle("0.000000\\0.275000\\0.500000\\0.300000", out normalizedRectangle);
                    var mgTl12 = new AnnotationItem(normalizedRectangle,
                                                    currentDataSet.Get<String>(DicomTag.ImageComments));
                    list.Add(mgTl12);

                    #endregion //Top Left

                    #region Bottom Left

                    GenerateRectangle("0.000000\\0.975000\\0.400000\\1.000000", out normalizedRectangle);
                    var mgb1 = new AnnotationItem(normalizedRectangle, GetWidthAndLevel(currentDataSet))
                    {
                        Bold = true
                    };
                    list.Add(mgb1);

                    #endregion //Bottom Left

                    #region Top Right

                    GenerateRectangle("0.600000\\0.000000\\1.000000\\0.025000", out normalizedRectangle);
                    var mgtr1 = new AnnotationItem(normalizedRectangle, currentDataSet.Get<String>(DicomTag.InstitutionName))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mgtr1);
                    GenerateRectangle("0.500000\\0.025000\\1.000000\\0.050000", out normalizedRectangle);
                    var mgtr2 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 currentDataSet.Get<String>(DicomTag.PatientName).Replace("^", ","))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(mgtr2);
                    GenerateRectangle("0.500000\\0.050000\\1.000000\\0.075000", out normalizedRectangle);
                    var mgtr3 = new AnnotationItem(normalizedRectangle, anonymize ?
                                             "Anonymised" :
                                             GetDobAndSex(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mgtr3);
                    GenerateRectangle("0.500000\\0.075000\\1.000000\\0.100000", out normalizedRectangle);
                    var mgtr4 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "MRN:" + currentDataSet.Get<String>(DicomTag.PatientID))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true,
                        Italics = true
                    };
                    list.Add(mgtr4);
                    GenerateRectangle("0.500000\\0.100000\\1.000000\\0.125000", out normalizedRectangle);
                    var mgtr5 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "Acc:" + currentDataSet.Get<String>(DicomTag.AccessionNumber))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(mgtr5);
                    GenerateRectangle("0.500000\\0.125000\\1.000000\\0.150000", out normalizedRectangle);
                    var mgtr6 = new AnnotationItem(normalizedRectangle, GetAcquisitionTime(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mgtr6);
                    GenerateRectangle("0.500000\\0.150000\\1.000000\\0.175000", out normalizedRectangle);
                    var mgtr7 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.Columns) + "x" +
                                                 currentDataSet.Get<String>(DicomTag.Rows))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mgtr7);
                    GenerateRectangle("0.500000\\0.175000\\1.000000\\0.200000", out normalizedRectangle);
                    var mgtr8 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.ConvolutionKernel))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mgtr8);

                    #endregion //Top Right

                    break;
                // Markers for MR
                case "MR":

                    #region Top Left

                    GenerateRectangle("0.000000\\0.000000\\0.400000\\0.025000", out normalizedRectangle);
                    var mrTl1 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.Manufacturer));
                    list.Add(mrTl1);
                    GenerateRectangle("0.000000\\0.025000\\0.500000\\0.050000", out normalizedRectangle);
                    var mrTl2 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ManufacturerModelName));
                    list.Add(mrTl2);
                    GenerateRectangle("0.000000\\0.050000\\0.500000\\0.075000", out normalizedRectangle);
                    var mrTl3 = new AnnotationItem(normalizedRectangle, GetDescription(currentDataSet));
                    list.Add(mrTl3);
                    GenerateRectangle("0.000000\\0.075000\\0.500000\\0.100000", out normalizedRectangle);
                    var mrTl4 = new AnnotationItem(normalizedRectangle, GetSeriesNo(currentDataSet));
                    list.Add(mrTl4);
                    GenerateRectangle("0.000000\\0.100000\\0.500000\\0.125000", out normalizedRectangle);
                    var mrTl5 = new AnnotationItem(normalizedRectangle, GetInstanceNo(currentDataSet));
                    list.Add(mrTl5);
                    GenerateRectangle("0.000000\\0.125000\\0.500000\\0.150000", out normalizedRectangle);
                    var mrTl6 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.PositionReferenceIndicator));
                    list.Add(mrTl6);
                    GenerateRectangle("0.000000\\0.150000\\0.500000\\0.175000", out normalizedRectangle);
                    var mrTl7 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ImageComments));
                    list.Add(mrTl7);

                    #endregion //Top Left

                    #region Bottom Left

                    GenerateRectangle("0.000000\\0.850000\\0.500000\\0.875000", out normalizedRectangle);
                    var mrBl6 = new AnnotationItem(normalizedRectangle,
                                                   "ET:" + currentDataSet.Get<String>(DicomTag.EchoTrainLength));
                    list.Add(mrBl6);
                    GenerateRectangle("0.000000\\0.875000\\0.500000\\0.900000", out normalizedRectangle);
                    var mrBl5 = new AnnotationItem(normalizedRectangle,
                                                   "TR:" + currentDataSet.Get<String>(DicomTag.RepetitionTime));
                    list.Add(mrBl5);
                    GenerateRectangle("0.000000\\0.900000\\0.500000\\0.925000", out normalizedRectangle);
                    var mrBl4 = new AnnotationItem(normalizedRectangle,
                                                   "TE:" + currentDataSet.Get<String>(DicomTag.EchoTime));
                    list.Add(mrBl4);
                    GenerateRectangle("0.000000\\0.925000\\0.500000\\0.950000", out normalizedRectangle);
                    var mrBl3 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ReceiveCoilName));
                    list.Add(mrBl3);
                    GenerateRectangle("0.000000\\0.950000\\0.500000\\0.975000", out normalizedRectangle);
                    var mrBl2 = new AnnotationItem(normalizedRectangle, SliceThicknessAndSpacing(currentDataSet));
                    list.Add(mrBl2);
                    GenerateRectangle("0.000000\\0.975000\\0.400000\\1.000000", out normalizedRectangle);
                    var mrBl1 = new AnnotationItem(normalizedRectangle, GetWidthAndLevel(currentDataSet))
                    {
                        Bold = true
                    };
                    list.Add(mrBl1);

                    #endregion //Bottom Left

                    #region Top Right

                    GenerateRectangle("0.600000\\0.000000\\1.000000\\0.025000", out normalizedRectangle);
                    var mrtr1 = new AnnotationItem(normalizedRectangle, currentDataSet.Get<String>(DicomTag.InstitutionName))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mrtr1);
                    GenerateRectangle("0.500000\\0.025000\\1.000000\\0.050000", out normalizedRectangle);
                    var mrtr2 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 currentDataSet.Get<String>(DicomTag.PatientName).Replace("^", ","))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(mrtr2);
                    GenerateRectangle("0.500000\\0.050000\\1.000000\\0.075000", out normalizedRectangle);
                    var mrtr3 = new AnnotationItem(normalizedRectangle, anonymize ?
                                             "Anonymised" :
                                             GetDobAndSex(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mrtr3);
                    GenerateRectangle("0.500000\\0.075000\\1.000000\\0.100000", out normalizedRectangle);
                    var mrtr4 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "MRN:" + currentDataSet.Get<String>(DicomTag.PatientID))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true,
                        Italics = true
                    };
                    list.Add(mrtr4);
                    GenerateRectangle("0.500000\\0.100000\\1.000000\\0.125000", out normalizedRectangle);
                    var mrtr5 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "Acc:" + currentDataSet.Get<String>(DicomTag.AccessionNumber))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(mrtr5);
                    GenerateRectangle("0.500000\\0.125000\\1.000000\\0.150000", out normalizedRectangle);
                    var mrtr6 = new AnnotationItem(normalizedRectangle, GetAcquisitionTime(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mrtr6);
                    GenerateRectangle("0.500000\\0.150000\\1.000000\\0.175000", out normalizedRectangle);
                    var mrtr7 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.Columns) + "x" +
                                                 currentDataSet.Get<String>(DicomTag.Rows))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mrtr7);
                    GenerateRectangle("0.500000\\0.175000\\1.000000\\0.200000", out normalizedRectangle);
                    var mrtr8 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.ConvolutionKernel))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mrtr8);

                    #endregion //Top Right

                    #region Bottom Right

                    GenerateRectangle("0.600000\\0.975000\\1.000000\\1.000000", out normalizedRectangle);
                    var mrBr1 = new AnnotationItem(normalizedRectangle,
                                                   "PP: " + currentDataSet.Get<String>(DicomTag.PatientPosition))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(mrBr1);

                    #endregion //Bottom Right

                    break;
                // Marker for PX (Dential)
                case "PX":

                    #region Top Left

                    GenerateRectangle("0.000000\\0.000000\\0.400000\\0.025000", out normalizedRectangle);
                    var pxl1 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.Manufacturer));
                    list.Add(pxl1);
                    GenerateRectangle("0.000000\\0.025000\\0.500000\\0.050000", out normalizedRectangle);
                    var pxl2 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.ManufacturerModelName));
                    list.Add(pxl2);
                    GenerateRectangle("0.000000\\0.050000\\0.500000\\0.075000", out normalizedRectangle);
                    var pxl3 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.StationName));
                    list.Add(pxl3);
                    GenerateRectangle("0.000000\\0.075000\\0.500000\\0.100000", out normalizedRectangle);
                    var pxl4 = new AnnotationItem(normalizedRectangle, GetDescription(currentDataSet));
                    list.Add(pxl4);
                    GenerateRectangle("0.000000\\0.100000\\0.500000\\0.125000", out normalizedRectangle);
                    var pxl5 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.BodyPartExamined));
                    list.Add(pxl5);
                    GenerateRectangle("0.000000\\0.125000\\0.300000\\0.150000", out normalizedRectangle);
                    var pxl6 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.ViewPosition));
                    list.Add(pxl6);
                    GenerateRectangle("0.000000\\0.150000\\0.300000\\0.175000", out normalizedRectangle);
                    var pxl7 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.CassetteOrientation));
                    list.Add(pxl7);
                    GenerateRectangle("0.000000\\0.175000\\0.300000\\0.200000", out normalizedRectangle);
                    var pxl8 = new AnnotationItem(normalizedRectangle, GetSeriesNo(currentDataSet));
                    list.Add(pxl8);
                    GenerateRectangle("0.000000\\0.200000\\0.300000\\0.225500", out normalizedRectangle);
                    var pxl9 = new AnnotationItem(normalizedRectangle, GetInstanceNo(currentDataSet));
                    list.Add(pxl9);
                    GenerateRectangle("0.000000\\0.225500\\0.300000\\0.250000", out normalizedRectangle);
                    var pxl10 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.PositionReferenceIndicator));
                    list.Add(pxl10);
                    GenerateRectangle("0.000000\\0.250000\\0.300000\\0.275000", out normalizedRectangle);
                    var pxl11 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ImageComments));
                    list.Add(pxl11);

                    #endregion //Top Left

                    #region Bottom Left

                    GenerateRectangle("0.000000\\0.9750000\\0.300000\\1.000000", out normalizedRectangle);
                    var pxBl1 = new AnnotationItem(normalizedRectangle, GetWidthAndLevel(currentDataSet))
                    {
                        Bold = true
                    };
                    list.Add(pxBl1);

                    #endregion //Bottom Left

                    #region Top Right

                    GenerateRectangle("0.600000\\0.000000\\1.000000\\0.025000", out normalizedRectangle);
                    var pxr1 = new AnnotationItem(normalizedRectangle, currentDataSet.Get<String>(DicomTag.InstitutionName))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(pxr1);
                    GenerateRectangle("0.500000\\0.025000\\1.000000\\0.050000", out normalizedRectangle);
                    var pxr2 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 currentDataSet.Get<String>(DicomTag.PatientName).Replace("^", ","))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(pxr2);
                    GenerateRectangle("0.500000\\0.050000\\1.000000\\0.075000", out normalizedRectangle);
                    var pxr3 = new AnnotationItem(normalizedRectangle, anonymize ?
                                             "Anonymised" :
                                             GetDobAndSex(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(pxr3);
                    GenerateRectangle("0.500000\\0.075000\\1.000000\\0.100000", out normalizedRectangle);
                    var pxr4 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "MRN:" + currentDataSet.Get<String>(DicomTag.PatientID))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true,
                        Italics = true
                    };
                    list.Add(pxr4);
                    GenerateRectangle("0.500000\\0.100000\\1.000000\\0.125000", out normalizedRectangle);
                    var pxr5 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "Acc:" + currentDataSet.Get<String>(DicomTag.AccessionNumber))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(pxr5);
                    GenerateRectangle("0.500000\\0.125000\\1.000000\\0.150000", out normalizedRectangle);
                    var pxr6 = new AnnotationItem(normalizedRectangle, GetAcquisitionTime(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(pxr6);
                    GenerateRectangle("0.500000\\0.150000\\1.000000\\0.175000", out normalizedRectangle);
                    var pxr7 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.Columns) + "x" +
                                                 currentDataSet.Get<String>(DicomTag.Rows))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(pxr7);
                    GenerateRectangle("0.500000\\0.175000\\1.000000\\0.200000", out normalizedRectangle);
                    var pxr8 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.ConvolutionKernel))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(pxr8);

                    #endregion //Top Right

                    break;
                // Markeers for CR and the Default Marker Template.
                default:

                    #region Top Left

                    GenerateRectangle("0.000000\\0.000000\\0.300000\\0.012500", out normalizedRectangle);
                    var dTl1 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.Manufacturer));
                    list.Add(dTl1);
                    GenerateRectangle("0.000000\\0.012500\\0.300000\\0.025000", out normalizedRectangle);
                    var dTl2 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.ManufacturerModelName));
                    list.Add(dTl2);
                    GenerateRectangle("0.000000\\0.025000\\0.300000\\0.037500", out normalizedRectangle);
                    var dTl3 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.StationName));
                    list.Add(dTl3);
                    GenerateRectangle("0.000000\\0.037500\\0.300000\\0.050000", out normalizedRectangle);
                    var dTl4 = new AnnotationItem(normalizedRectangle, GetDescription(currentDataSet));
                    list.Add(dTl4);
                    GenerateRectangle("0.000000\\0.050000\\0.300000\\0.062500", out normalizedRectangle);
                    var dTl5 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.BodyPartExamined));
                    list.Add(dTl5);
                    GenerateRectangle("0.000000\\0.062500\\0.300000\\0.075000", out normalizedRectangle);
                    var dTl6 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.ViewPosition));
                    list.Add(dTl6);
                    GenerateRectangle("0.000000\\0.075000\\0.300000\\0.100000", out normalizedRectangle);
                    var dTl7 = new AnnotationItem(normalizedRectangle,
                                                  currentDataSet.Get<String>(DicomTag.CassetteOrientation));
                    list.Add(dTl7);
                    GenerateRectangle("0.000000\\0.100000\\0.300000\\0.112500", out normalizedRectangle);
                    var dTl8 = new AnnotationItem(normalizedRectangle, GetSeriesNo(currentDataSet));
                    list.Add(dTl8);
                    GenerateRectangle("0.000000\\0.112500\\0.300000\\0.125500", out normalizedRectangle);
                    var dTl9 = new AnnotationItem(normalizedRectangle, GetInstanceNo(currentDataSet));
                    list.Add(dTl9);
                    GenerateRectangle("0.000000\\0.125500\\0.300000\\0.137500", out normalizedRectangle);
                    var dTl10 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.PositionReferenceIndicator));
                    list.Add(dTl10);
                    GenerateRectangle("0.000000\\0.137500\\0.300000\\0.150000", out normalizedRectangle);
                    var dTl11 = new AnnotationItem(normalizedRectangle,
                                                   currentDataSet.Get<String>(DicomTag.ImageComments));
                    list.Add(dTl11);

                    #endregion //Top Left

                    #region Bottom Left

                    GenerateRectangle("0.000000\\0.9875000\\0.300000\\1.000000", out normalizedRectangle);
                    var dBl1 = new AnnotationItem(normalizedRectangle, GetWidthAndLevel(currentDataSet))
                    {
                        Bold = true
                    };
                    list.Add(dBl1);

                    #endregion //Bottom Left

                    #region Top Right

                    GenerateRectangle("0.600000\\0.000000\\1.000000\\0.012500", out normalizedRectangle);
                    var dtr1 = new AnnotationItem(normalizedRectangle, currentDataSet.Get<String>(DicomTag.InstitutionName))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(dtr1);
                    GenerateRectangle("0.500000\\0.012500\\1.000000\\0.025000", out normalizedRectangle);
                    var dtr2 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 currentDataSet.Get<String>(DicomTag.PatientName).Replace("^", ","))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(dtr2);
                    GenerateRectangle("0.500000\\0.025000\\1.000000\\0.037500", out normalizedRectangle);
                    var dtr3 = new AnnotationItem(normalizedRectangle, anonymize ?
                                             "Anonymised" :
                                             GetDobAndSex(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(dtr3);
                    GenerateRectangle("0.500000\\0.037500\\1.000000\\0.050000", out normalizedRectangle);
                    var dtr4 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "MRN:" + currentDataSet.Get<String>(DicomTag.PatientID))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true,
                        Italics = true
                    };
                    list.Add(dtr4);
                    GenerateRectangle("0.500000\\0.050000\\1.000000\\0.062500", out normalizedRectangle);
                    var dtr5 = new AnnotationItem(normalizedRectangle, anonymize ?
                                                 "Anonymised" :
                                                 "Acc:" + currentDataSet.Get<String>(DicomTag.AccessionNumber))
                    {
                        Justification = AnnotationItem.HAlignment.Right,
                        Bold = true
                    };
                    list.Add(dtr5);
                    GenerateRectangle("0.500000\\0.062500\\1.000000\\0.075000", out normalizedRectangle);
                    var dtr6 = new AnnotationItem(normalizedRectangle, GetAcquisitionTime(currentDataSet))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(dtr6);
                    GenerateRectangle("0.500000\\0.075000\\1.000000\\0.087500", out normalizedRectangle);
                    var dtr7 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.Columns) + "x" +
                                                 currentDataSet.Get<String>(DicomTag.Rows))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(dtr7);
                    GenerateRectangle("0.500000\\0.087500\\1.000000\\0.100000", out normalizedRectangle);
                    var dtr8 = new AnnotationItem(normalizedRectangle,
                                                 currentDataSet.Get<String>(DicomTag.ConvolutionKernel))
                    {
                        Justification = AnnotationItem.HAlignment.Right
                    };
                    list.Add(dtr8);

                    #endregion //Top Right

                    break;
            }

            return list;
        }

        #endregion

        #region Annotation helpers

        /// <summary>
        /// Gets the instance no.
        /// </summary>
        /// <param name="currentDataSet">The current data set.</param>
        /// <returns></returns>
        private static string GetInstanceNo(DicomDataset currentDataSet)
        {
            var str = currentDataSet.Get<String>(DicomTag.InstanceNumber);
            return string.IsNullOrEmpty(str) ? "" : "Img: " + str;
        }

        /// <summary>
        /// Gets the series no.
        /// </summary>
        /// <param name="currentDataSet">The current data set.</param>
        /// <returns></returns>
        private static string GetSeriesNo(DicomDataset currentDataSet)
        {
            var str = currentDataSet.Get<String>(DicomTag.SeriesNumber);
            return string.IsNullOrEmpty(str) ? "" : "Ser: " + str;
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <param name="currentDataSet">The current data set.</param>
        /// <returns></returns>
        private static string GetDescription(DicomDataset currentDataSet)
        {
            var str = currentDataSet.Get<String>(DicomTag.StudyDescription);
            return string.IsNullOrEmpty(str) ? "" : "Desc: " + str;
        }

        /// <summary>
        /// Gets the field of view.
        /// </summary>
        /// <param name="currentDataSet">The current Dicom data set.</param>
        /// <returns></returns>
        private static string GetFieldOfView(DicomDataset currentDataSet)
        {
            var ds = currentDataSet.Get<Double[]>(DicomTag.PixelSpacing);
            var x = ds[0];
            var y = ds[1];

            var rowsInt = currentDataSet.Get<Int32>(DicomTag.Rows);
            var colsInt = currentDataSet.Get<Int32>(DicomTag.Columns);

            // Field-of-View (FOV) is Rows/Columns * Pixel Spacing
            // Divide by 10 to put values in Centimeters instead of Millimeters
            var fovWidth = (colsInt * x) / 10;
            var fovHeight = (rowsInt * y) / 10;

            return "DFOV: " + fovWidth.ToString("0.0") + " x " + fovHeight.ToString("0.0") + "cm";
        }

        /// <summary>
        /// Gets the width and level.
        /// </summary>
        /// <param name="currentDataSet">The current Dicom data set.</param>
        /// <returns></returns>
        private static string GetWidthAndLevel(DicomDataset currentDataSet)
        {
            try
            {
                var width = currentDataSet.Get<double>(DicomTag.WindowWidth);
                var center = currentDataSet.Get<double>(DicomTag.WindowCenter);

                return "W:" + Math.Round(Convert.ToDouble(width), 3) + " C:" + Math.Round(Convert.ToDouble(center), 3);
            }
            catch
            {
                return "W: 100% C: 50%";
            }

        }

        /// <summary>
        /// Slices the thickness and spacing.
        /// </summary>
        /// <param name="currentDataSet">The current Dicom data set.</param>
        /// <returns></returns>
        private static string SliceThicknessAndSpacing(DicomDataset currentDataSet)
        {
            var sliceThickness = currentDataSet.Get<Decimal>(DicomTag.SliceThickness);
            var sliceSpacing = currentDataSet.Get<Decimal>(DicomTag.SpacingBetweenSlices);
            return sliceThickness == 0 && sliceThickness == 0
                       ? ""
                       : "thk:" + sliceThickness.ToString("0.00") + " mm spc:" + sliceSpacing.ToString("0.00") + "mm";
        }

        /// <summary>
        /// Gets the acquisition time.
        /// </summary>
        /// <param name="currentDataSet">The current Dicom data set.</param>
        /// <returns></returns>
        private static string GetAcquisitionTime(DicomDataset currentDataSet)
        {
            var acquisitionDate = currentDataSet.GetDateTime(DicomTag.AcquisitionDate, DicomTag.AcquisitionTime);
            var acquisitionTime = currentDataSet.GetDateTime(DicomTag.AcquisitionDate, DicomTag.AcquisitionTime);
            var date = acquisitionDate.ToString("dd-MMM-yyyy");
            var time = acquisitionTime.ToString("HH:mm:ss");

            return String.Format("{0} {1}", date, time);
        }

        /// <summary>
        /// Gets the dob and sex.
        /// </summary>
        /// <param name="currentDataSet">The current Dicom data set.</param>
        /// <returns></returns>
        private static string GetDobAndSex(DicomDataset currentDataSet)
        {
            var dob = currentDataSet.Get(DicomTag.PatientBirthDate, new DateTime());
            var dateString = dob.ToString("dd-MMM-yyyy");
            var age = currentDataSet.Get(DicomTag.PatientAge, String.Empty);
            var sex = currentDataSet.Get(DicomTag.PatientSex, String.Empty);
            return "DOB: " + dateString + " (" + age + ") " + sex;
        }

        #endregion //Annotation helpers

        #region Rectangle utills

        /// <summary>
        /// Generates the rectangle.
        /// </summary>
        /// <param name="rectangleString">The rectangle string.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns></returns>
        // ReSharper disable UnusedMethodReturnValue.Local
        private static bool GenerateRectangle(string rectangleString, out RectangleF rectangle)
        // ReSharper restore UnusedMethodReturnValue.Local
        {
            rectangle = new RectangleF();

            string[] rectangleComponents = rectangleString.Split('\\');
            if (rectangleComponents.Length != 4)
                return false;

            float left, right, top, bottom;
            if (!float.TryParse(rectangleComponents[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out left))
                return false;
            if (!float.TryParse(rectangleComponents[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out top))
                return false;
            if (!float.TryParse(rectangleComponents[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out right))
                return false;
            if (!float.TryParse(rectangleComponents[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out bottom))
                return false;

            if (left >= right)
                return false;
            if (top >= bottom)
                return false;
            if (left < 0F || left > 1.0F)
                return false;
            if (top < 0F || top > 1.0F)
                return false;
            if (right < 0F || right > 1.0F)
                return false;
            if (bottom < 0F || bottom > 1.0F)
                return false;

            rectangle = RectangleF.FromLTRB(left, top, right, bottom);
            return IsRectangleNormalized(rectangle);
        }

        /// <summary>
        /// Determines whether the specified rectangle is normalized.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>
        ///   <c>true</c> if [The rectangle] is normalized; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRectangleNormalized(RectangleF rectangle)
        {
            return (!(FloatComparer.IsLessThan(rectangle.Left, 0.0f) ||
                    FloatComparer.IsGreaterThan(rectangle.Left, 1.0f) ||
                    FloatComparer.IsLessThan(rectangle.Right, 0.0f) ||
                    FloatComparer.IsGreaterThan(rectangle.Right, 1.0f) ||
                    FloatComparer.IsLessThan(rectangle.Top, 0.0f) ||
                    FloatComparer.IsGreaterThan(rectangle.Top, 1.0f) ||
                    FloatComparer.IsLessThan(rectangle.Bottom, 0.0f) ||
                    FloatComparer.IsGreaterThan(rectangle.Bottom, 1.0f) ||
                    FloatComparer.IsGreaterThan(rectangle.Left, rectangle.Right) ||
                    FloatComparer.IsGreaterThan(rectangle.Top, rectangle.Bottom)));
        }

        /// <summary>
        /// Calculates the sub rectangle.
        /// </summary>
        /// <param name="parentRectangle">The parent rectangle.</param>
        /// <param name="childRectangle">The child rectangle.</param>
        /// <returns></returns>
        public static Rectangle CalculateSubRectangle(Rectangle parentRectangle, RectangleF childRectangle)
        {
            var left = parentRectangle.Left + (int)(childRectangle.Left * parentRectangle.Width);
            var right = parentRectangle.Left + (int)(childRectangle.Right * parentRectangle.Width);
            var top = parentRectangle.Top + (int)(childRectangle.Top * parentRectangle.Height);
            var bottom = parentRectangle.Top + (int)(childRectangle.Bottom * parentRectangle.Height);

            return new Rectangle(left, top, right - left, bottom - top);
        }

        #endregion //Rectangle utills
    }
}
