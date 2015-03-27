using System;
using System.Drawing;

namespace Dicom.Printing
{
    public class AnnotationItem
    {
        #region Enums
        /// <summary>
        /// Defines the available horizontal justifications.
        /// </summary>
        public enum HAlignment
        {
            /// <summary>
            /// Specifies that the string should be left-justified in the <see cref="AnnotationItem.AnnotationRect" />.
            /// </summary>
            Left,

            /// <summary>
            /// Specifies that the string should be centred horizontally in the <see cref="AnnotationItem.AnnotationRect"/>.
            /// </summary>
            Center,

            /// <summary>
            /// Specifies that the string should be right-justified in the <see cref="AnnotationItem.AnnotationRect"/>.
            /// </summary>
            Right
        };

        /// <summary>
        /// Defines the available vertical alignments.
        /// </summary>
        public enum VAlignment
        {
            /// <summary>
            /// Specifies that the string should be aligned along the top of the <see cref="AnnotationItem.AnnotationRect"/>.
            /// </summary>
            Top,

            /// <summary>
            /// Specifies that the string should be centered in the <see cref="AnnotationItem.AnnotationRect"/>.
            /// </summary>
            Center,

            /// <summary>
            /// Specifies that the string should be aligned along the bottom of the <see cref="AnnotationItem.AnnotationRect"/>.
            /// </summary>
            Bottom
        };
        #endregion //Enums

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationItem"/> class.
        /// </summary>
        public AnnotationItem()
        {
            VerticalAlignment = VAlignment.Center;
            Justification = HAlignment.Left;
            AnnotationRect = new RectangleF();
        }

        /// <summary>
        /// Constructor that initializes the <see cref="AnnotationRect"/>
        /// </summary>
        public AnnotationItem(RectangleF annotationRectangle, String annotationText)
        {
            VerticalAlignment = VAlignment.Center;
            Justification = HAlignment.Left;
            AnnotationRect = annotationRectangle;
            AnnotationText = annotationText;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the annotation rect.
        /// </summary>
        /// <value>
        /// The annotation rect.
        /// </value>
        public RectangleF AnnotationRect { get; set; }

        /// <summary>
        /// Gets or sets the annotation text.
        /// </summary>
        /// <value>
        /// The annotation text.
        /// </value>
        public String AnnotationText { get; set; }

        /// <summary>
        /// Gets or sets whether the text should be in italics.
        /// </summary>
        /// <remarks>
        /// The default value is false.
        /// </remarks>
        public bool Italics { get; set; }

        /// <summary>
        /// Gets or sets whether the text should be in bold.
        /// </summary>
        /// <remarks>
        /// The default value is false.
        /// </remarks>
        public bool Bold { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HAlignment"/>.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="HAlignment.Left"/>.
        /// </remarks>
        public HAlignment Justification { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="VAlignment"/>.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="VAlignment.Center"/>.
        /// </remarks>
        public VAlignment VerticalAlignment { get; set; }

        #endregion //Public Properties
    }
}
