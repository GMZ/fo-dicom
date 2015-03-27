using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.Network;
using Dicom.Printing;

namespace Dicom_Print_SCP
{
    public class PrintService : DicomService, IDicomServiceProvider, IDicomNServiceProvider, IDicomCEchoProvider
    {
        #region Properties and Attributes

        public static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes =
        {
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        };
        public static readonly DicomTransferSyntax[] AcceptedImageTransferSyntaxes =
        {
            //Uncmpressed
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        };

        private static DicomServer<PrintService> _server;
        public static Printer Printer { get; private set; }

        public string CallingAE { get; protected set; }
        public string CalledAE { get; protected set; }
        public System.Net.IPAddress RemoteIP { get; private set; }

        private FilmSession _filmSession;

        private readonly Dictionary<string, PrintJob> _printJobList = new Dictionary<string, PrintJob>();

        private bool _sendEventReports;
        private readonly object _synchRoot = new object();

        #endregion

        #region Constructors and Initialization

        public PrintService(System.IO.Stream stream, Dicom.Log.Logger log)
            : base(stream, log)
        {
            var pi = stream.GetType().GetProperty("Socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pi != null)
            {
                var endPoint = ((System.Net.Sockets.Socket)pi.GetValue(stream, null)).RemoteEndPoint as System.Net.IPEndPoint;
                if (endPoint != null) RemoteIP = endPoint.Address;
            }
            else
            {
                RemoteIP = new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 });
            }
        }

        public static void Start(int port, string aet)
        {
            Printer = new Printer(aet);
            _server = new DicomServer<PrintService>(port);
        }

        public static void Stop()
        {
            _server.Dispose();
        }

        #endregion

        #region IDicomServiceProvider Members

        public void OnReceiveAssociationRequest(DicomAssociation association)
        {
            Logger.Info("Received association request from AE: {0} with IP: {1} ", association.CallingAE, RemoteIP);

            if (Printer.PrinterAet != association.CalledAE)
            {
                Logger.Error("Association with {0} rejected since requested printer {1} not found",
                    association.CallingAE, association.CalledAE);
                SendAssociationReject(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
                return;
            }

            CallingAE = association.CallingAE;
            CalledAE = Printer.PrinterAet;

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification ||
                    pc.AbstractSyntax == DicomUID.BasicGrayscalePrintManagementMetaSOPClass ||
                    pc.AbstractSyntax == DicomUID.BasicColorPrintManagementMetaSOPClass ||
                    pc.AbstractSyntax == DicomUID.PrinterSOPClass ||
                    pc.AbstractSyntax == DicomUID.BasicFilmSessionSOPClass ||
                    pc.AbstractSyntax == DicomUID.BasicFilmBoxSOPClass ||
                    pc.AbstractSyntax == DicomUID.BasicGrayscaleImageBoxSOPClass ||
                    pc.AbstractSyntax == DicomUID.BasicColorImageBoxSOPClass)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PrintJobSOPClass)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                    _sendEventReports = true;
                }
                else
                {
                    Logger.Warn("Requested abstract syntax {0} from {1} not supported", pc.AbstractSyntax, association.CallingAE);
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Logger.Info("Accepted association request from {0}", association.CallingAE);
            SendAssociationAccept(association);
        }

        public void OnReceiveAssociationReleaseRequest()
        {
            Clean();
            SendAssociationReleaseResponse();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            //stop printing operation
            //log the abort reason
            Logger.Error("Received abort from {0}, reason is {1}", source, reason);
        }

        public void OnConnectionClosed(int errorCode)
        {
            Clean();
        }

        #endregion

        #region IDicomCEchoProvider Members

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            Logger.Info("Received verification request from AE {0} with IP: {1}", CallingAE, RemoteIP);
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        #endregion

        #region N-CREATE requests handlers

        public DicomNCreateResponse OnNCreateRequest(DicomNCreateRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSessionSOPClass)
                {
                    return CreateFilmSession(request);
                }
                if (request.SOPClassUID == DicomUID.BasicFilmBoxSOPClass)
                {
                    return CreateFilmBox(request);
                }
                if (request.SOPClassUID == DicomUID.PresentationLUTSOPClass)
                {
                    return CreatePresentationLut(request);

                }
                return new DicomNCreateResponse(request, DicomStatus.SOPClassNotSupported);
            }
        }

        private DicomNCreateResponse CreateFilmSession(DicomNCreateRequest request)
        {
            if (_filmSession != null)
            {
                Logger.Error("Attemted to create new basic film session on association with {0}", CallingAE);
                SendAbort(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified);
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            var pc = request.PresentationContext;

            bool isColor = pc != null && pc.AbstractSyntax == DicomUID.BasicColorPrintManagementMetaSOPClass;


            _filmSession = new FilmSession(request.SOPClassUID, request.SOPInstanceUID, request.Dataset, isColor);


            Logger.Info("Create new film session {0}", _filmSession.SOPInstanceUID.UID);

            var response = new DicomNCreateResponse(request, DicomStatus.Success);
            response.Command.Add(DicomTag.AffectedSOPInstanceUID, _filmSession.SOPInstanceUID);
            return response;
        }

        private DicomNCreateResponse CreateFilmBox(DicomNCreateRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                SendAbort(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified);
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);

            }


            var filmBox = _filmSession.CreateFilmBox(request.SOPInstanceUID, request.Dataset);

            if (!filmBox.Initialize())
            {
                Logger.Error("Failed to initialize requested film box {0}", filmBox.SOPInstanceUID.UID);
                SendAbort(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified);
                return new DicomNCreateResponse(request, DicomStatus.ProcessingFailure);
            }

            Logger.Info("Created new film box {0}", filmBox.SOPInstanceUID.UID);

            var response = new DicomNCreateResponse(request, DicomStatus.Success);
            response.Command.Add(DicomTag.AffectedSOPInstanceUID, filmBox.SOPInstanceUID);
            response.Dataset = filmBox;
            return response;
        }

        private DicomNCreateResponse CreatePresentationLut(DicomNCreateRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("Film Session on association with {0} does not exist", CallingAE);
                SendAbort(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified);
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            _filmSession.CreatePresentationLut(request.SOPInstanceUID, request.Dataset);
            var response = new DicomNCreateResponse(request, DicomStatus.Success);
            response.Command.Add(DicomTag.AffectedSOPInstanceUID, _filmSession.SOPInstanceUID);
            return response;
        }

        #endregion

        #region N-DELETE request handler

        public DicomNDeleteResponse OnNDeleteRequest(DicomNDeleteRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSessionSOPClass)
                {
                    return DeleteFilmSession(request);
                }
                if (request.SOPClassUID == DicomUID.BasicFilmBoxSOPClass)
                {
                    return DeleteFilmBox(request);
                }
                if (request.SOPClassUID == DicomUID.PresentationLUTSOPClass)
                {
                    return DeletePresentationLut(request);
                }
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchSOPClass);
            }
        }

        private DicomNDeleteResponse DeleteFilmBox(DicomNDeleteRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("Can't delete a basic film session doesnot exist for this association {0}", CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            var status = _filmSession.DeleteFilmBox(request.SOPInstanceUID) 
                ? DicomStatus.Success 
                : DicomStatus.NoSuchObjectInstance;
            var response = new DicomNDeleteResponse(request, status);

            response.Command.Add(DicomTag.AffectedSOPInstanceUID, request.SOPInstanceUID);
            return response;
        }

        private DicomNDeleteResponse DeleteFilmSession(DicomNDeleteRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("Can't delete a basic film session doesnot exist for this association {0}", CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            if (!request.SOPInstanceUID.Equals(_filmSession.SOPInstanceUID))
            {
                Logger.Error("Can't delete a basic film session with instace UID {0} doesnot exist for this association {1}",
                    request.SOPInstanceUID.UID, CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }
            _filmSession = null;

            return new DicomNDeleteResponse(request, DicomStatus.Success);
        }

        private DicomNDeleteResponse DeletePresentationLut(DicomNDeleteRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("Can't delete a basic film session doesnot exist for this association {0}", CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            _filmSession.DeletePresentationLut(request.SOPInstanceUID);

            return new DicomNDeleteResponse(request, DicomStatus.Success);
        }

        #endregion

        #region N-SET request handler

        public DicomNSetResponse OnNSetRequest(DicomNSetRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSessionSOPClass)
                {
                    return SetFilmSession(request);
                }
                if (request.SOPClassUID == DicomUID.BasicFilmBoxSOPClass)
                {
                    return SetFilmBox(request);
                }
                if (request.SOPClassUID == DicomUID.BasicColorImageBoxSOPClass ||
                    request.SOPClassUID == DicomUID.BasicGrayscaleImageBoxSOPClass)
                {
                    return SetImageBox(request);
                }
                return new DicomNSetResponse(request, DicomStatus.SOPClassNotSupported);
            }
        }

        private DicomNSetResponse SetImageBox(DicomNSetRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            Logger.Info("Set image box {0}", request.SOPInstanceUID.UID);

            var imageBox = _filmSession.FindImageBox(request.SOPInstanceUID);
            if (imageBox == null)
            {
                Logger.Error("Received N-SET request for invalid image box instance {0} for this association {1}", request.SOPInstanceUID.UID, CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            request.Dataset.CopyTo(imageBox);

            return new DicomNSetResponse(request, DicomStatus.Success);
        }

        private DicomNSetResponse SetFilmBox(DicomNSetRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            Logger.Info("Set film box {0}", request.SOPInstanceUID.UID);
            var filmBox = _filmSession.FindFilmBox(request.SOPInstanceUID);

            if (filmBox == null)
            {
                Logger.Error("Received N-SET request for invalid film box {0} from {1}", request.SOPInstanceUID.UID, CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            request.Dataset.CopyTo(filmBox);

            filmBox.Initialize();

            var response = new DicomNSetResponse(request, DicomStatus.Success);
            response.Command.Add(DicomTag.AffectedSOPInstanceUID, filmBox.SOPInstanceUID);
            response.Command.Add(DicomTag.CommandDataSetType, (ushort)0x0202);
            response.Dataset = filmBox;
            return response;
        }

        private DicomNSetResponse SetFilmSession(DicomNSetRequest request)
        {
            if (_filmSession == null || _filmSession.SOPInstanceUID.UID != request.SOPInstanceUID.UID)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            Logger.Info("Set film session {0}", request.SOPInstanceUID.UID);
            request.Dataset.CopyTo(_filmSession);

            return new DicomNSetResponse(request, DicomStatus.Success);
        }

        #endregion

        #region N-GET request handler

        public DicomNGetResponse OnNGetRequest(DicomNGetRequest request)
        {
            lock (_synchRoot)
            {
                Logger.Info(request.ToString(true));

                if (request.SOPClassUID == DicomUID.PrinterSOPClass && request.SOPInstanceUID == DicomUID.PrinterSOPInstance)
                {
                    return GetPrinter(request);
                }
                if (request.SOPClassUID == DicomUID.PrintJobSOPClass)
                {
                    return GetPrintJob(request);
                }
                if (request.SOPClassUID == DicomUID.PrinterConfigurationRetrievalSOPClass && request.SOPInstanceUID == DicomUID.PrinterConfigurationRetrievalSOPInstance)
                {
                    return GetPrinterConfiguration(request);
                }
                return new DicomNGetResponse(request, DicomStatus.NoSuchSOPClass);
            }
        }

        private DicomNGetResponse GetPrinter(DicomNGetRequest request)
        {

            var ds = new DicomDataset();

            var sb = new System.Text.StringBuilder();
            if (request.Attributes != null && request.Attributes.Length > 0)
            {
                foreach (var item in request.Attributes)
                {
                    sb.AppendFormat("GetPrinter attribute {0} requested", item);
                    sb.AppendLine();
                    var value = Printer.Get(item, "");
                    ds.Add(item, value);
                }

                Logger.Info(sb.ToString());
            }
            if (ds.Count() == 0)
            {

                ds.Add(DicomTag.PrinterStatus, Printer.PrinterStatus);
                ds.Add(DicomTag.PrinterStatusInfo, Printer.PrinterStatusInfo);
                ds.Add(DicomTag.PrinterName, Printer.PrinterName);
                ds.Add(DicomTag.Manufacturer, Printer.Manufacturer);
                ds.Add(DicomTag.DateOfLastCalibration, Printer.DateTimeOfLastCalibration.Date);
                ds.Add(DicomTag.TimeOfLastCalibration, Printer.DateTimeOfLastCalibration);
                ds.Add(DicomTag.ManufacturerModelName, Printer.ManufacturerModelName);
                ds.Add(DicomTag.DeviceSerialNumber, Printer.DeviceSerialNumber);
                ds.Add(DicomTag.SoftwareVersions, Printer.SoftwareVersions);
            }

            var response = new DicomNGetResponse(request, DicomStatus.Success) {Dataset = ds};

            Logger.Info(response.ToString(true));
            return response;
        }

        private static DicomNGetResponse GetPrinterConfiguration(DicomNGetRequest request)
        {
            var dataset = new DicomDataset();
            var config = new DicomDataset();

            var sequence = new DicomSequence(DicomTag.PrinterConfigurationSequence, config);

            dataset.Add(sequence);

            var response = new DicomNGetResponse(request, DicomStatus.Success);
            response.Command.Add(DicomTag.AffectedSOPInstanceUID, request.SOPInstanceUID);
            response.Dataset = dataset;
            return response;

        }

        private DicomNGetResponse GetPrintJob(DicomNGetRequest request)
        {
            if (_printJobList.ContainsKey(request.SOPInstanceUID.UID))
            {
                var printJob = _printJobList[request.SOPInstanceUID.UID];

                var sb = new System.Text.StringBuilder();

                var dataset = new DicomDataset();

                if (request.Attributes != null && request.Attributes.Length > 0)
                {
                    foreach (var item in request.Attributes)
                    {
                        sb.AppendFormat("GetPrintJob attribute {0} requested", item);
                        sb.AppendLine();
                        var value = printJob.Get(item, "");
                        dataset.Add(item, value);
                    }

                    Logger.Info(sb.ToString());
                }

                var response = new DicomNGetResponse(request, DicomStatus.Success) {Dataset = dataset};
                return response;
            }
            else
            {
                var response = new DicomNGetResponse(request, DicomStatus.NoSuchObjectInstance);
                return response;
            }
        }

        #endregion

        #region N-ACTION request handler

        public DicomNActionResponse OnNActionRequest(DicomNActionRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNActionResponse(request, DicomStatus.InvalidObjectInstance);
            }

            lock (_synchRoot)
            {
                try
                {

                    var filmBoxList = new List<FilmBox>();
                    if (request.SOPClassUID == DicomUID.BasicFilmSessionSOPClass && request.ActionTypeID == 0x0001)
                    {
                        Logger.Info("Creating new print job for film session {0}", _filmSession.SOPInstanceUID.UID);
                        filmBoxList.AddRange(_filmSession.BasicFilmBoxes);
                    }
                    else if (request.SOPClassUID == DicomUID.BasicFilmBoxSOPClass && request.ActionTypeID == 0x0001)
                    {
                        Logger.Info("Creating new print job for film box {0}", request.SOPInstanceUID.UID);

                        var filmBox = _filmSession.FindFilmBox(request.SOPInstanceUID);
                        if (filmBox != null)
                        {
                            filmBoxList.Add(filmBox);
                        }
                        else
                        {
                            Logger.Error("Received N-ACTION request for invalid film box {0} from {1}", request.SOPInstanceUID.UID, CallingAE);
                            return new DicomNActionResponse(request, DicomStatus.NoSuchObjectInstance);
                        }
                    }
                    else
                    {
                        if (request.ActionTypeID != 0x0001)
                        {
                            Logger.Error("Received N-ACTION request for invalid action type {0} from {1}", request.ActionTypeID, CallingAE);
                            return new DicomNActionResponse(request, DicomStatus.NoSuchActionType);
                        }
                        Logger.Error("Received N-ACTION request for invalid SOP class {0} from {1}", request.SOPClassUID, CallingAE);
                        return new DicomNActionResponse(request, DicomStatus.NoSuchSOPClass);
                    }

                    var printJob = new PrintJob2(null, Printer, CallingAE, Logger, _filmSession)
                    {
                        SendNEventReport = _sendEventReports
                    };
                    printJob.StatusUpdate += OnPrintJobStatusUpdate;

                    printJob.Print(filmBoxList);

                    if (printJob.Error == null)
                    {

                        var result = new DicomDataset
                        {
                            new DicomSequence(new DicomTag(0x2100, 0x0500),
                                new DicomDataset(new DicomUniqueIdentifier(DicomTag.ReferencedSOPClassUID,
                                    DicomUID.PrintJobSOPClass)),
                                new DicomDataset(new DicomUniqueIdentifier(DicomTag.ReferencedSOPInstanceUID,
                                    printJob.SOPInstanceUID)))
                        };

                        var response = new DicomNActionResponse(request, DicomStatus.Success);
                        response.Command.Add(DicomTag.AffectedSOPInstanceUID, printJob.SOPInstanceUID);
                        response.Dataset = result;

                        return response;
                    }
                    throw printJob.Error;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error occured during N-ACTION {0} for SOP class {1} and instance {2}",
                        request.ActionTypeID, request.SOPClassUID.UID, request.SOPInstanceUID.UID);
                    Logger.Error(ex.Message);
                    return new DicomNActionResponse(request, DicomStatus.ProcessingFailure);
                }
            }
        }

        void OnPrintJobStatusUpdate(object sender, StatusUpdateEventArgs e)
        {
            var printJob = sender as PrintJob;
            if (printJob != null && printJob.SendNEventReport)
            {
                var reportRequest = new DicomNEventReportRequest(printJob.SOPClassUID, printJob.SOPInstanceUID, e.EventTypeId);
                var ds = new DicomDataset
                {
                    {DicomTag.ExecutionStatusInfo, e.ExecutionStatusInfo},
                    {DicomTag.FilmSessionLabel, e.FilmSessionLabel},
                    {DicomTag.PrinterName, e.PrinterName}
                };

                reportRequest.Dataset = ds;
                SendRequest(reportRequest);
            }
        }

        #endregion

        public void Clean()
        {
            //delete the current active print job and film sessions
            lock (_synchRoot)
            {
                _filmSession = null;
                _printJobList.Clear();
            }
        }

        #region IDicomNServiceProvider Members

        public DicomNEventReportResponse OnNEventReportRequest(DicomNEventReportRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
