// Copyright (c) 2012-2015 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace Dicom.Network
{
    public class DicomCCancelRequest : DicomRequest
    {
        public DicomCCancelRequest(DicomDataset command)
            : base(command)
        {
        }

        public DicomCCancelRequest(DicomPriority priority = DicomPriority.Medium)
            : base(DicomCommandField.CCancelRequest, DicomUID.StudyRootQueryRetrieveInformationModelMOVE, priority)
        {
            Dataset = new DicomDataset();
            //Level = DicomQueryRetrieveLevel.Study;
        }

        public DicomCCancelRequest()
            : base(new DicomDataset())
        {
        }

        //public DicomCCancelRequest(
        //    string destinationAe,
        //    string studyInstanceUid,
        //    DicomPriority priority = DicomPriority.Medium)
        //    : base(DicomCommandField.CCancelRequest, DicomUID.StudyRootQueryRetrieveInformationModelMOVE, priority)
        //{
        //    DestinationAE = destinationAe;
        //    Dataset = new DicomDataset();
        //    Level = DicomQueryRetrieveLevel.Study;
        //    Dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid);
        //}

        //public DicomCCancelRequest(
        //    string destinationAe,
        //    string studyInstanceUid,
        //    string seriesInstanceUid,
        //    DicomPriority priority = DicomPriority.Medium)
        //    : base(DicomCommandField.CCancelRequest, DicomUID.StudyRootQueryRetrieveInformationModelMOVE, priority)
        //{
        //    DestinationAE = destinationAe;
        //    Dataset = new DicomDataset();
        //    Level = DicomQueryRetrieveLevel.Series;
        //    Dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid);
        //    Dataset.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid);
        //}

        //public DicomCCancelRequest(
        //    string destinationAe,
        //    string studyInstanceUid,
        //    string seriesInstanceUid,
        //    string sopInstanceUid,
        //    DicomPriority priority = DicomPriority.Medium)
        //    : base(DicomCommandField.CCancelRequest, DicomUID.StudyRootQueryRetrieveInformationModelMOVE, priority)
        //{
        //    DestinationAE = destinationAe;
        //    Dataset = new DicomDataset();
        //    Level = DicomQueryRetrieveLevel.Image;
        //    Dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid);
        //    Dataset.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid);
        //    Dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUid);
        //}

        //public DicomQueryRetrieveLevel Level
        //{
        //    get
        //    {
        //        return Dataset.Get<DicomQueryRetrieveLevel>(DicomTag.QueryRetrieveLevel);
        //    }
        //    set
        //    {
        //        Dataset.Remove(DicomTag.QueryRetrieveLevel);
        //        if (value != DicomQueryRetrieveLevel.Worklist) Dataset.Add(DicomTag.QueryRetrieveLevel, value.ToString().ToUpper());
        //    }
        //}

        //public string DestinationAE
        //{
        //    get
        //    {
        //        return Command.Get<string>(DicomTag.MoveDestination);
        //    }
        //    set
        //    {
        //        Command.Add(DicomTag.MoveDestination, value);
        //    }
        //}

        //public delegate void ResponseDelegate(DicomCCancelRequest request, DicomCCancelResponse response);

        //public ResponseDelegate OnResponseReceived;

        internal override void PostResponse(DicomService service, DicomResponse response)
        {
            try
            {
                //if (OnResponseReceived != null) OnResponseReceived(this, (DicomCCancelResponse)response);
            }
            catch
            {
            }
        }
    }
}

