// Copyright (c) 2012-2015 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dicom
{
    public static class DicomDatasetExtensions
    {
        public static DicomDataset Clone(this DicomDataset dataset)
        {
            return new DicomDataset(dataset)
            {
                InternalTransferSyntax = dataset.InternalTransferSyntax
            };
        }

        public static DateTime GetDateTime(this DicomDataset dataset, DicomTag date, DicomTag time)
        {
            var dd = dataset.Get<DicomDate>(date);
            var dt = dataset.Get<DicomTime>(time);

            var da = dd != null && dd.Count > 0 ? dd.Get<DateTime>(0) : DateTime.MinValue;
            var tm = dt != null && dt.Count > 0 ? dt.Get<DateTime>(0) : DateTime.MinValue;

            return new DateTime(da.Year, da.Month, da.Day, tm.Hour, tm.Minute, tm.Second);
        }

        /// <summary>
        /// Enumerates DICOM items matching mask.
        /// </summary>
        /// <param name="mask">Mask</param>
        /// <returns>Enumeration of DICOM items</returns>
        public static IEnumerable<DicomItem> EnumerateMasked(this DicomDataset dataset, DicomMaskedTag mask)
        {
            return dataset.Where(x => mask.IsMatch(x.Tag));
        }

        /// <summary>
        /// Enumerates DICOM items for specified group.
        /// </summary>
        /// <param name="group">Group</param>
        /// <returns>Enumeration of DICOM items</returns>
        public static IEnumerable<DicomItem> EnumerateGroup(this DicomDataset dataset, ushort group)
        {
            return dataset.Where(x => x.Tag.Group == group && x.Tag.Element != 0x0000);
        }
    }
}
