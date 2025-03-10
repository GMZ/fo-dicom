﻿// Copyright (c) 2012-2015 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Dicom.Imaging.Codec;
using Dicom.IO;
using Dicom.IO.Reader;
using Dicom.IO.Writer;
using Dicom.Log;
using Dicom.Threading;

namespace Dicom.Network
{
    /// <summary>
    /// Base class for DICOM network services.
    /// </summary>
    public abstract class DicomService
    {
        private Stream _network;

        private object _lock;

        private volatile bool _writing;

        private volatile bool _sending;

        private Queue<PDU> _pduQueue;

        private Queue<DicomMessage> _msgQueue;

        private List<DicomRequest> _pending;

        private DicomMessage _dimse;

        private Stream _dimseStream;

        private string _dimseStreamFile;

        private bool _isTempFile;

        private int _readLength;

        private ThreadPoolQueue<int> _processQueue;

        private DicomServiceOptions _options;

        private readonly Encoding _fallbackEncoding;

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomService"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="log">The log.</param>
        protected DicomService(Stream stream, Logger log)
            : this(stream, DicomEncoding.Default, log)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomService"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fallbackEncoding">The fallback encoding.</param>
        /// <param name="log">The log.</param>
        /// <exception cref="System.ArgumentNullException">fallbackEncoding</exception>
        protected DicomService(Stream stream, Encoding fallbackEncoding, Logger log)
        {
            if (fallbackEncoding == null)
            {
                throw new ArgumentNullException("fallbackEncoding");
            }
            _network = stream;
            _lock = new object();
            _pduQueue = new Queue<PDU>();
            MaximumPDUsInQueue = 16;
            _msgQueue = new Queue<DicomMessage>();
            _pending = new List<DicomRequest>();
            _processQueue = new ThreadPoolQueue<int>();
            _processQueue.DefaultGroup = Int32.MinValue;
            IsConnected = true;
            _fallbackEncoding = fallbackEncoding;
            Logger = log ?? LogManager.Default.GetLogger("Dicom.Network");
            BeginReadPDUHeader();
            Options = DicomServiceOptions.Default;
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public Logger Logger { get; set; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        public DicomServiceOptions Options
        {
            get { return _options; }
            set
            {
                _options = value;
                _processQueue.Linger = _options.ThreadPoolLinger;
            }
        }

        private string LogID { get; set; }

        /// <summary>
        /// Gets or sets the userstate.
        /// </summary>
        public object UserState { get; set; }

        /// <summary>
        /// Gets the association.
        /// </summary>
        public DicomAssociation Association { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is send queue empty.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is send queue empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsSendQueueEmpty
        {
            get
            {
                lock (_lock) return _pending.Count == 0;
            }
        }

        /// <summary>
        /// Gets or sets the maximum PDUs in queue.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int MaximumPDUsInQueue { get; set; }

        private void CloseConnection(Exception exception)
        {
            if (!IsConnected) return;

            IsConnected = false;
            try
            {
                _network.Close();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }

            if (exception != null) Logger.Error("Connection closed with error: {@error}", exception);
            else Logger.Info("Connection closed");

            // ReSharper disable PossibleNullReferenceException
            if (this is IDicomServiceProvider) (this as IDicomServiceProvider).OnConnectionClosed(exception);
            else if (this is IDicomServiceUser) (this as IDicomServiceUser).OnConnectionClosed(exception);
            // ReSharper restore PossibleNullReferenceException
        }

        private void BeginReadPDUHeader()
        {
            try
            {
                _readLength = 6;

                var buffer = new byte[6];
                _network.BeginRead(buffer, 0, 6, EndReadPDUHeader, buffer);
            }
            catch (ObjectDisposedException)
            {
                // silently ignore
                CloseConnection(null);
            }
            catch (NullReferenceException)
            {
                // connection already closed; silently ignore
                CloseConnection(null);
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                {
                    Logger.Error(
                        "Socket error while reading PDU: {socketErrorCode} [{errorCode}]",
                        (e.InnerException as SocketException).SocketErrorCode,
                        (e.InnerException as SocketException).ErrorCode);
                }
                else if (!(e.InnerException is ObjectDisposedException)) Logger.Error("IO exception while reading PDU: {@error}", e);

                CloseConnection(e);
            }
        }

        private void EndReadPDUHeader(IAsyncResult result)
        {
            try
            {
                var buffer = (byte[])result.AsyncState;

                var count = _network.EndRead(result);
                if (count == 0)
                {
                    // disconnected
                    CloseConnection(null);
                    return;
                }

                _readLength -= count;

                if (_readLength > 0)
                {
                    _network.BeginRead(buffer, 6 - _readLength, _readLength, EndReadPDUHeader, buffer);
                    return;
                }

                var length = BitConverter.ToInt32(buffer, 2);
                length = Endian.Swap(length);

                _readLength = length;

                Array.Resize(ref buffer, length + 6);

                _network.BeginRead(buffer, 6, length, EndReadPDU, buffer);
            }
            catch (ObjectDisposedException)
            {
                // silently ignore
                CloseConnection(null);
            }
            catch (NullReferenceException)
            {
                // connection already closed; silently ignore
                CloseConnection(null);
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                {
                    Logger.Error(
                        "Socket error while reading PDU: {socketErrorCode} [{errorCode}]",
                        (e.InnerException as SocketException).SocketErrorCode,
                        (e.InnerException as SocketException).ErrorCode);
                }
                else if (!(e.InnerException is ObjectDisposedException)) Logger.Error("IO exception while reading PDU: {@error}", e);

                CloseConnection(e);
            }
            catch (Exception e)
            {
                Logger.Error("Exception processing PDU header: {@error}", e);
            }
        }

        private void EndReadPDU(IAsyncResult result)
        {
            try
            {
                var buffer = (byte[])result.AsyncState;

                var count = _network.EndRead(result);
                if (count == 0)
                {
                    // disconnected
                    CloseConnection(null);
                    return;
                }

                _readLength -= count;

                if (_readLength > 0)
                {
                    _network.BeginRead(buffer, buffer.Length - _readLength, _readLength, EndReadPDU, buffer);
                    return;
                }

                var raw = new RawPDU(buffer);

                switch (raw.Type)
                {
                    case 0x01:
                        {
                            Association = new DicomAssociation();
                            var pdu = new AAssociateRQ(Association);
                            pdu.Read(raw);
                            LogID = Association.CallingAE;
                            if (Options.UseRemoteAEForLogName) Logger = LogManager.Default.GetLogger(LogID);
                            Logger.Info(
                                "{callingAE} <- Association request:\n{association}",
                                LogID,
                                Association.ToString());
                            // ReSharper disable once PossibleNullReferenceException
                            if (this is IDicomServiceProvider) (this as IDicomServiceProvider).OnReceiveAssociationRequest(Association);
                            break;
                        }
                    case 0x02:
                        {
                            var pdu = new AAssociateAC(Association);
                            pdu.Read(raw);
                            LogID = Association.CalledAE;
                            Logger.Info(
                                "{calledAE} <- Association accept:\n{assocation}",
                                LogID,
                                Association.ToString());
                            // ReSharper disable once PossibleNullReferenceException
                            if (this is IDicomServiceUser) (this as IDicomServiceUser).OnReceiveAssociationAccept(Association);
                            break;
                        }
                    case 0x03:
                        {
                            var pdu = new AAssociateRJ();
                            pdu.Read(raw);
                            Logger.Info(
                                "{logId} <- Association reject [result: {pduResult}; source: {pduSource}; reason: {pduReason}]",
                                LogID,
                                pdu.Result,
                                pdu.Source,
                                pdu.Reason);
                            if (this is IDicomServiceUser)
                                // ReSharper disable once PossibleNullReferenceException
                                (this as IDicomServiceUser).OnReceiveAssociationReject(
                                    pdu.Result,
                                    pdu.Source,
                                    pdu.Reason);
                            break;
                        }
                    case 0x04:
                        {
                            var pdu = new PDataTF();
                            pdu.Read(raw);
                            if (Options.LogDataPDUs) Logger.Info("{logId} <- {@pdu}", LogID, pdu);
                            _processQueue.Queue(ProcessPDataTF, pdu);
                            break;
                        }
                    case 0x05:
                        {
                            var pdu = new AReleaseRQ();
                            pdu.Read(raw);
                            Logger.Info("{logId} <- Association release request", LogID);
                            // ReSharper disable once PossibleNullReferenceException
                            if (this is IDicomServiceProvider) (this as IDicomServiceProvider).OnReceiveAssociationReleaseRequest();
                            break;
                        }
                    case 0x06:
                        {
                            var pdu = new AReleaseRP();
                            pdu.Read(raw);
                            Logger.Info("{logId} <- Association release response", LogID);
                            // ReSharper disable once PossibleNullReferenceException
                            if (this is IDicomServiceUser) (this as IDicomServiceUser).OnReceiveAssociationReleaseResponse();
                            CloseConnection(null);
                            break;
                        }
                    case 0x07:
                        {
                            var pdu = new AAbort();
                            pdu.Read(raw);
                            Logger.Info("{logId} <- Abort: {pduSource} - {pduReason}", LogID, pdu.Source, pdu.Reason);
                            // ReSharper disable PossibleNullReferenceException
                            if (this is IDicomServiceProvider) (this as IDicomServiceProvider).OnReceiveAbort(pdu.Source, pdu.Reason);
                            else if (this is IDicomServiceUser) (this as IDicomServiceUser).OnReceiveAbort(pdu.Source, pdu.Reason);
                            // ReSharper restore PossibleNullReferenceException
                            CloseConnection(null);
                            break;
                        }
                    case 0xFF:
                        {
                            break;
                        }
                    default:
                        throw new DicomNetworkException("Unknown PDU type");
                }

                BeginReadPDUHeader();
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                {
                    Logger.Error(
                        "Socket error while reading PDU: {socketErrorCode} [{errorCode}]",
                        (e.InnerException as SocketException).SocketErrorCode,
                        (e.InnerException as SocketException).ErrorCode);
                }
                else if (!(e.InnerException is ObjectDisposedException)) Logger.Error("IO exception while reading PDU: {@error}", e);

                CloseConnection(e);
            }
            catch (NullReferenceException)
            {
                // connection already closed; silently ignore
                CloseConnection(null);
            }
            catch (Exception e)
            {
                Logger.Error("Exception processing PDU: {@error}", e);
                CloseConnection(e);
            }
        }

        /// <summary>
        /// Process P-DATA-TF PDUs.
        /// </summary>
        /// <param name="state">PDU to process.</param>
        private void ProcessPDataTF(object state)
        {
            var pdu = (PDataTF)state;
            try
            {
                foreach (var pdv in pdu.PDVs)
                {
                    if (_dimse == null)
                    {
                        // create stream for receiving command
                        if (_dimseStream == null)
                        {
                            _dimseStream = new MemoryStream();
                            _dimseStreamFile = null;
                        }
                    }
                    else
                    {
                        // create stream for receiving dataset
                        if (_dimseStream == null)
                        {
                            if (_dimse.Type == DicomCommandField.CStoreRequest)
                            {
                                var pc = Association.PresentationContexts.FirstOrDefault(x => x.ID == pdv.PCID);

                                var file = new DicomFile
                                {
                                    FileMetaInfo =
                                    {
                                        MediaStorageSOPClassUID = pc.AbstractSyntax,
                                        MediaStorageSOPInstanceUID = _dimse.Command.Get<DicomUID>(DicomTag.AffectedSOPInstanceUID),
                                        TransferSyntax = pc.AcceptedTransferSyntax,
                                        ImplementationClassUID = Association.RemoteImplemetationClassUID,
                                        ImplementationVersionName = Association.RemoteImplementationVersion,
                                        SourceApplicationEntityTitle = Association.CallingAE
                                    }
                                };

                                _dimseStream = CreateCStoreReceiveStream(file);
                                _dimseStreamFile = file.File == null ? null : file.File.Name;
                            }
                            else
                            {
                                _dimseStream = new MemoryStream();
                                _dimseStreamFile = null;
                            }
                        }
                    }

                    _dimseStream.Write(pdv.Value, 0, pdv.Value.Length);

                    if (pdv.IsLastFragment)
                    {
                        if (pdv.IsCommand)
                        {
                            _dimseStream.Seek(0, SeekOrigin.Begin);

                            var command = new DicomDataset();

                            var reader = new DicomReader {IsExplicitVR = false};
                            reader.Read(new StreamByteSource(_dimseStream), new DicomDatasetReaderObserver(command));

                            _dimseStream = null;
                            _dimseStreamFile = null;

                            var type = command.Get<DicomCommandField>(DicomTag.CommandField);
                            switch (type)
                            {
                                case DicomCommandField.CStoreRequest:
                                    _dimse = new DicomCStoreRequest(command);
                                    break;
                                case DicomCommandField.CStoreResponse:
                                    _dimse = new DicomCStoreResponse(command);
                                    break;
                                case DicomCommandField.CFindRequest:
                                    _dimse = new DicomCFindRequest(command);
                                    break;
                                case DicomCommandField.CFindResponse:
                                    _dimse = new DicomCFindResponse(command);
                                    break;
                                case DicomCommandField.CMoveRequest:
                                    _dimse = new DicomCMoveRequest(command);
                                    break;
                                case DicomCommandField.CMoveResponse:
                                    _dimse = new DicomCMoveResponse(command);
                                    break;
                                case DicomCommandField.CEchoRequest:
                                    _dimse = new DicomCEchoRequest(command);
                                    break;
                                case DicomCommandField.CEchoResponse:
                                    _dimse = new DicomCEchoResponse(command);
                                    break;
                                case DicomCommandField.NActionRequest:
                                    _dimse = new DicomNActionRequest(command);
                                    break;
                                case DicomCommandField.NActionResponse:
                                    _dimse = new DicomNActionResponse(command);
                                    break;
                                case DicomCommandField.NCreateRequest:
                                    _dimse = new DicomNCreateRequest(command);
                                    break;
                                case DicomCommandField.NCreateResponse:
                                    _dimse = new DicomNCreateResponse(command);
                                    break;
                                case DicomCommandField.NDeleteRequest:
                                    _dimse = new DicomNDeleteRequest(command);
                                    break;
                                case DicomCommandField.NDeleteResponse:
                                    _dimse = new DicomNDeleteResponse(command);
                                    break;
                                case DicomCommandField.NEventReportRequest:
                                    _dimse = new DicomNEventReportRequest(command);
                                    break;
                                case DicomCommandField.NEventReportResponse:
                                    _dimse = new DicomNEventReportResponse(command);
                                    break;
                                case DicomCommandField.NGetRequest:
                                    _dimse = new DicomNGetRequest(command);
                                    break;
                                case DicomCommandField.NGetResponse:
                                    _dimse = new DicomNGetResponse(command);
                                    break;
                                case DicomCommandField.NSetRequest:
                                    _dimse = new DicomNSetRequest(command);
                                    break;
                                case DicomCommandField.NSetResponse:
                                    _dimse = new DicomNSetResponse(command);
                                    break;
                                case DicomCommandField.CCancelRequest:
                                    _dimse = new DicomCCancelRequest(command);
                                    break;
                                case DicomCommandField.CCancelResponse:
                                    _dimse = new DicomCCancelResponse(command);
                                    break;
                                default:
                                    _dimse = new DicomMessage(command);
                                    break;
                            }
                            _dimse.PresentationContext =
                                Association.PresentationContexts.FirstOrDefault(x => x.ID == pdv.PCID);
                            if (!_dimse.HasDataset)
                            {
                                if (DicomMessage.IsRequest(_dimse.Type)) ThreadPool.QueueUserWorkItem(PerformDimseCallback, _dimse);
                                else
                                    _processQueue.Queue(
                                        (_dimse as DicomResponse).RequestMessageID,
                                        PerformDimseCallback,
                                        _dimse);
                                _dimse = null;
                                return;
                            }
                        }
                        else
                        {
                            if (_dimse.Type != DicomCommandField.CStoreRequest)
                            {
                                _dimseStream.Seek(0, SeekOrigin.Begin);

                                var pc = Association.PresentationContexts.FirstOrDefault(x => x.ID == pdv.PCID);

                                _dimse.Dataset = new DicomDataset {InternalTransferSyntax = pc.AcceptedTransferSyntax};

                                var source = new StreamByteSource(_dimseStream)
                                {
                                    Endian = pc.AcceptedTransferSyntax.Endian
                                };

                                var reader = new DicomReader {IsExplicitVR = pc.AcceptedTransferSyntax.IsExplicitVR};
                                reader.Read(source, new DicomDatasetReaderObserver(_dimse.Dataset));

                                _dimseStream = null;
                                _dimseStreamFile = null;
                            }
                            else
                            {
                                var request = _dimse as DicomCStoreRequest;

                                try
                                {
                                    var dicomFile = GetCStoreDicomFile();

                                    _dimseStream = null;
                                    _dimseStreamFile = null;

                                    _isTempFile = false;

                                    // NOTE: dicomFile will be valid with the default implementation of CreateCStoreReceiveStream() and
                                    // GetCStoreDicomFile(), but can be null if a child class overrides either method and changes behavior.
                                    // See documentation on CreateCStoreReceiveStream() and GetCStoreDicomFile() for information about why
                                    // this might be desired.
                                    request.File = dicomFile;
                                    if (request.File != null)
                                    {
                                        request.Dataset = request.File.Dataset;
                                    }
                                }
                                catch (Exception e)
                                {
                                    // failed to parse received DICOM file; send error response instead of aborting connection
                                    SendResponse(
                                        new DicomCStoreResponse(
                                            request,
                                            new DicomStatus(DicomStatus.ProcessingFailure, e.Message)));
                                    Logger.Error("Error parsing C-Store dataset: {@error}", e);
                                    (this as IDicomCStoreProvider).OnCStoreRequestException(_dimseStreamFile, e);
                                    return;
                                }
                            }

                            if (DicomMessage.IsRequest(_dimse.Type)) 
                                ThreadPool.QueueUserWorkItem(PerformDimseCallback, _dimse);
                            else
                                _processQueue.Queue(
                                    (_dimse as DicomResponse).RequestMessageID,
                                    PerformDimseCallback,
                                    _dimse);
                            _dimse = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SendAbort(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
                Logger.Error("Exception processing P-Data-TF PDU: {@error}", e);
            }
            finally
            {
                SendNextMessage();
            }
        }

        /// <summary>
        /// The purpose of this method is to return the Stream that a SopInstance received
        /// via CStoreSCP will be written to.  This default implementation creates a temporary
        /// file and returns a FileStream on top of it.  Child classes can override this to write
        /// to another stream and avoid the I/O associated with the temporary file if so desired.
        /// Beware that some SopInstances can be very large so using a MemoryStream() could cause
        /// out of memory situations.
        /// </summary>
        /// <param name="file">A DicomFile with FileMetaInfo populated</param>
        /// <returns>The stream to write the SopInstance to</returns>
        protected virtual Stream CreateCStoreReceiveStream(DicomFile file)
        {
            var temp = TemporaryFile.Create();

            var dimseStream = temp.Open();
            file.Save(dimseStream);
            dimseStream.Seek(0, SeekOrigin.Begin);

            _isTempFile = true;
            dimseStream.Seek(0, SeekOrigin.End);
            return dimseStream;
        }

        /// <summary>
        /// The purpose of this method is to create a DicomFile for the SopInstance received via
        /// CStoreSCP to pass to the IDicomCStoreProvider.OnCStoreRequest method for processing.
        /// This default implementation will return a DicomFile if the stream created by
        /// CreateCStoreReceiveStream() is seekable or null if it is not.  Child classes that 
        /// override CreateCStoreReceiveStream may also want override this to return a DicomFile 
        /// for unseekable streams or to do cleanup related to receiving that specific instance.  
        /// </summary>
        /// <returns>The DicomFile or null if the stream is not seekable</returns>
        protected virtual DicomFile GetCStoreDicomFile()
        {
            if (!string.IsNullOrWhiteSpace(_dimseStreamFile))
            {
                _dimseStream.Close();
                _dimseStream = null;

                var file = DicomFile.Open(_dimseStreamFile, _fallbackEncoding);
                file.File.IsTempFile = _isTempFile;
                _dimseStreamFile = null;

                return file;
            }

            if (_dimseStream.CanSeek)
            {
                _dimseStream.Seek(0, SeekOrigin.Begin);
                var file = DicomFile.Open(_dimseStream, _fallbackEncoding);
                return file;
            }

            return null;
        }

        private void PerformDimseCallback(object state)
        {
            // ReSharper disable PossibleNullReferenceException
            var dimse = state as DicomMessage;

            try
            {
                Logger.Info("{logId} <- {dicomMessage}", LogID, dimse.ToString(Options.LogDimseDatasets));

                if (!DicomMessage.IsRequest(dimse.Type))
                {
                    var rsp = dimse as DicomResponse;
                    lock (_lock)
                    {
                        var req = _pending.FirstOrDefault(x => x.MessageID == rsp.RequestMessageID);
                        if (req != null)
                        {
                            rsp.UserState = req.UserState;
                            req.PostResponse(this, rsp);
                            if (rsp.Status.State != DicomState.Pending) _pending.Remove(req);
                        }
                    }
                    return;
                }

                if (dimse.Type == DicomCommandField.CStoreRequest)
                {
                    if (this is IDicomCStoreProvider)
                    {
                        var response = (this as IDicomCStoreProvider).OnCStoreRequest(dimse as DicomCStoreRequest);
                        SendResponse(response);
                        return;
                    }
                    else throw new DicomNetworkException("C-Store SCP not implemented");
                }

                // ReSharper disable SuspiciousTypeConversion.Global
                if (dimse.Type == DicomCommandField.CFindRequest)
                {
                    if (this is IDicomCFindProvider)
                    {
                        var responses = (this as IDicomCFindProvider).OnCFindRequest(dimse as DicomCFindRequest);
                        foreach (var response in responses) SendResponse(response);
                        return;
                    }
                    else throw new DicomNetworkException("C-Find SCP not implemented");
                }

                if (dimse.Type == DicomCommandField.CMoveRequest)
                {
                    if (this is IDicomCMoveProvider)
                    {
                        var responses = (this as IDicomCMoveProvider).OnCMoveRequest(dimse as DicomCMoveRequest);
                        foreach (var response in responses) SendResponse(response);
                        return;
                    }
                    else throw new DicomNetworkException("C-Move SCP not implemented");
                }
                // ReSharper restore SuspiciousTypeConversion.Global

                if (dimse.Type == DicomCommandField.CEchoRequest)
                {
                    if (this is IDicomCEchoProvider)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        var response = (this as IDicomCEchoProvider).OnCEchoRequest(dimse as DicomCEchoRequest);
                        SendResponse(response);
                        return;
                    }
                    else throw new DicomNetworkException("C-Echo SCP not implemented");
                }

                if (dimse.Type == DicomCommandField.CCancelRequest)
                {
                    if (this is IDicomCCancelProvider)
                    {
                        var responses = (this as IDicomCCancelProvider).OnCCancelRequest(dimse as DicomCCancelRequest);
                        foreach (var response in responses) SendResponse(response);
                        return;
                    }
                    else throw new DicomNetworkException("C-Cancel SCP not implemented");
                }

                if (dimse.Type == DicomCommandField.NActionRequest || dimse.Type == DicomCommandField.NCreateRequest || 
                    dimse.Type == DicomCommandField.NDeleteRequest || dimse.Type == DicomCommandField.NEventReportRequest || 
                    dimse.Type == DicomCommandField.NGetRequest || dimse.Type == DicomCommandField.NSetRequest)
                {
                    if (!(this is IDicomNServiceProvider)) throw new DicomNetworkException("N-Service SCP not implemented");

                    DicomResponse response = null;

                    if (dimse.Type == DicomCommandField.NActionRequest) response = (this as IDicomNServiceProvider).OnNActionRequest(dimse as DicomNActionRequest);
                    else if (dimse.Type == DicomCommandField.NCreateRequest) response = (this as IDicomNServiceProvider).OnNCreateRequest(dimse as DicomNCreateRequest);
                    else if (dimse.Type == DicomCommandField.NDeleteRequest)
                        response =
                            (this as IDicomNServiceProvider).OnNDeleteRequest(dimse as DicomNDeleteRequest);
                    else if (dimse.Type == DicomCommandField.NEventReportRequest)
                        response =
                            (this as IDicomNServiceProvider).OnNEventReportRequest(
                                dimse as DicomNEventReportRequest);
                    else if (dimse.Type == DicomCommandField.NGetRequest)
                        response =
                            (this as IDicomNServiceProvider).OnNGetRequest(dimse as DicomNGetRequest);
                    else if (dimse.Type == DicomCommandField.NSetRequest)
                        response =
                            (this as IDicomNServiceProvider).OnNSetRequest(
                                dimse as DicomNSetRequest);

                    SendResponse(response);
                    return;
                }
                // ReSharper restore PossibleNullReferenceException

                throw new DicomNetworkException("Operation not implemented");
            }
            finally
            {
                SendNextMessage();
            }
        }

        /// <summary>
        /// Sends the pdu.
        /// </summary>
        /// <param name="pdu">The pdu.</param>
        protected void SendPDU(PDU pdu)
        {
            // throttle queueing of PDUs to prevent out of memory errors for very large datasets
            do
            {
                if (_pduQueue.Count >= MaximumPDUsInQueue)
                {
                    Thread.Sleep(10);
                    continue;
                }

                lock (_lock) 
                    _pduQueue.Enqueue(pdu);

                break;
            }
            while (true);

            SendNextPDU();
        }

        private void SendNextPDU()
        {
            if (!IsConnected) 
                return;

            PDU pdu;

            lock (_lock)
            {
                if (_writing) 
                    return;

                if (_pduQueue.Count == 0) 
                    return;

                _writing = true;

                pdu = _pduQueue.Dequeue();
            }

            if (Options.LogDataPDUs && pdu is PDataTF) Logger.Info("{logId} -> {pdu}", LogID, pdu);

            MemoryStream ms = new MemoryStream();
            pdu.Write().WritePDU(ms);

            byte[] buffer = ms.ToArray();

            try
            {
                _network.BeginWrite(buffer, 0, (int)ms.Length, OnEndSendPDU, buffer);
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                {
                    Logger.Error(
                        "Socket error while writing PDU: {socketErrorCode} [{errorCode}]",
                        (e.InnerException as SocketException).SocketErrorCode,
                        (e.InnerException as SocketException).ErrorCode);
                }
                else if (!(e.InnerException is ObjectDisposedException)) Logger.Error("IO exception while writing PDU: {@error}", e);

                CloseConnection(e);
            }
        }

        private void OnEndSendPDU(IAsyncResult ar)
        {
            // ReSharper disable once UnusedVariable
            var buffer = (byte[])ar.AsyncState;

            try
            {
                _network.EndWrite(ar);
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                {
                    Logger.Error(
                        "Socket error while writing PDU: {socketErrorCode} [{errorCode}]",
                        (e.InnerException as SocketException).SocketErrorCode,
                        (e.InnerException as SocketException).ErrorCode);
                }
                else if (!(e.InnerException is ObjectDisposedException)) Logger.Error("IO exception while writing PDU: {@error}", e);

                CloseConnection(e);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
            finally
            {
                lock (_lock) _writing = false;
                SendNextPDU();
            }
        }

        private void SendMessage(DicomMessage message)
        {
            lock (_lock) 
                _msgQueue.Enqueue(message);
            SendNextMessage();
        }

        private class Dimse
        {
            public DicomMessage Message;

            public PDataTFStream Stream;

            public DicomDatasetWalker Walker;

            public DicomPresentationContext PresentationContext;
        }

        private void SendNextMessage()
        {
            DicomMessage msg;

            lock (_lock)
            {
                if (_msgQueue.Count == 0)
                {
                    if (_pending.Count == 0) 
                        OnSendQueueEmpty();
                    return;
                }

                if (_sending) 
                    return;

                if (Association.MaxAsyncOpsInvoked > 0 && _pending.Count >= Association.MaxAsyncOpsInvoked) 
                    return;

                _sending = true;

                msg = _msgQueue.Dequeue();
            }

            if (msg is DicomRequest) 
                _pending.Add(msg as DicomRequest);

            DicomPresentationContext pc;
            if (msg is DicomCStoreRequest)
            {
                //pc = Association.PresentationContexts.FirstOrDefault(x => x.Result == DicomPresentationContextResult.Accept && x.AbstractSyntax == msg.SOPClassUID && x.AcceptedTransferSyntax == (msg as DicomCStoreRequest).TransferSyntax);
                //if (pc == null)
                //    pc = Association.PresentationContexts.FirstOrDefault(x => x.Result == DicomPresentationContextResult.Accept && x.AbstractSyntax == msg.SOPClassUID);
                // ReSharper disable once PossibleNullReferenceException
                pc = Association.PresentationContexts.FirstOrDefault(x => x.Result == DicomPresentationContextResult.Accept && x.AbstractSyntax == msg.SOPClassUID && x.AcceptedTransferSyntax == (msg as DicomCStoreRequest).TransferSyntax) ??
                     Association.PresentationContexts.FirstOrDefault(x => x.Result == DicomPresentationContextResult.Accept && x.AbstractSyntax == msg.SOPClassUID);
            }
            else
            {
                pc = Association.PresentationContexts.FirstOrDefault(x => x.Result == DicomPresentationContextResult.Accept && x.AbstractSyntax == msg.SOPClassUID);
            }

            if (pc == null)
            {
                pc = msg.PresentationContext;
            }

            if (pc == null)
            {
                _pending.Remove(msg as DicomRequest);

                try
                {
                    if (msg is DicomCStoreRequest)
                        (msg as DicomCStoreRequest).PostResponse(
                            this,
                            new DicomCStoreResponse(msg as DicomCStoreRequest, DicomStatus.SOPClassNotSupported));
                    else if (msg is DicomCEchoRequest)
                        (msg as DicomCEchoRequest).PostResponse(
                            this,
                            new DicomCEchoResponse(msg as DicomCEchoRequest, DicomStatus.SOPClassNotSupported));
                    else if (msg is DicomCFindRequest)
                        (msg as DicomCFindRequest).PostResponse(
                            this,
                            new DicomCFindResponse(msg as DicomCFindRequest, DicomStatus.SOPClassNotSupported));
                    else if (msg is DicomCMoveRequest)
                        (msg as DicomCMoveRequest).PostResponse(
                            this,
                            new DicomCMoveResponse(
                                msg as DicomCMoveRequest,
                                DicomStatus.SOPClassNotSupported));
                    else if (msg is DicomCCancelRequest)
                        (msg as DicomCCancelRequest).PostResponse(
                            this,
                            new DicomCCancelResponse(
                                msg as DicomCCancelRequest,
                                DicomStatus.SOPClassNotSupported));

                    //TODO: add N services
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }

                Logger.Error("No accepted presentation context found for abstract syntax: {sopClassUid}", msg.SOPClassUID);
                lock (_lock) 
                    _sending = false;
                SendNextMessage();
                return;
            }

            var dimse = new Dimse
            {
                Message = msg, 
                PresentationContext = pc
            };

            // force calculation of command group length as required by standard
            msg.Command.RecalculateGroupLengths();

            if (msg.HasDataset)
            {
                // remove group lengths as recommended in PS 3.5 7.2
                //
                //	2. It is recommended that Group Length elements be removed during storage or transfer 
                //	   in order to avoid the risk of inconsistencies arising during coercion of data 
                //	   element values and changes in transfer syntax.
                msg.Dataset.RemoveGroupLengths();

                if (msg.Dataset.InternalTransferSyntax != dimse.PresentationContext.AcceptedTransferSyntax) 
                    msg.Dataset = msg.Dataset.ChangeTransferSyntax(dimse.PresentationContext.AcceptedTransferSyntax);
            }

            Logger.Info("{logId} -> {dicomMessage}", LogID, msg.ToString(Options.LogDimseDatasets));

            dimse.Stream = new PDataTFStream(this, pc.ID, Association.MaximumPDULength);

            var writer = new DicomWriter(
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomWriteOptions.Default,
                new StreamByteTarget(dimse.Stream));

            dimse.Walker = new DicomDatasetWalker(msg.Command);

            if (dimse.Message.HasDataset) dimse.Walker.BeginWalk(writer, OnEndSendCommand, dimse);
            else dimse.Walker.BeginWalk(writer, OnEndSendMessage, dimse);
        }

        private void OnEndSendCommand(IAsyncResult result)
        {
            var dimse = result.AsyncState as Dimse;
            // ReSharper disable PossibleNullReferenceException
            try
            {
                dimse.Walker.EndWalk(result);

                dimse.Stream.IsCommand = false;

                var writer = new DicomWriter(
                    dimse.PresentationContext.AcceptedTransferSyntax,
                    DicomWriteOptions.Default,
                    new StreamByteTarget(dimse.Stream));

                dimse.Walker = new DicomDatasetWalker(dimse.Message.Dataset);
                dimse.Walker.BeginWalk(writer, OnEndSendMessage, dimse);
            }
            catch (Exception e)
            {
                Logger.Error("Exception sending DIMSE: {@error}", e);
            }
            finally
            {
                if (!dimse.Message.HasDataset)
                {
                    lock (_lock) _sending = false;
                    SendNextMessage();
                }
            }
            // ReSharper restore PossibleNullReferenceException
        }

        private void OnEndSendMessage(IAsyncResult result)
        {
            var dimse = result.AsyncState as Dimse;
            // ReSharper disable PossibleNullReferenceException
            try
            {
                dimse.Walker.EndWalk(result);
            }
            catch (Exception e)
            {
                Logger.Error("Exception sending DIMSE: {@error}", e);
            }
            finally
            {
                dimse.Stream.Flush(true);
                dimse.Stream.Close();

                lock (_lock) _sending = false;
                SendNextMessage();
            }
            // ReSharper restore PossibleNullReferenceException
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void SendRequest(DicomRequest request)
        {
            SendMessage(request);
        }

        /// <summary>
        /// Sends the response.
        /// </summary>
        /// <param name="response">The response.</param>
        protected void SendResponse(DicomResponse response)
        {
            SendMessage(response);
        }

        private class PDataTFStream : Stream
        {
            #region Private Members

            private DicomService _service;

            private bool _command;

            private uint _pduMax;

            private uint _max;

            private byte _pcid;

            private PDataTF _pdu;

            private byte[] _bytes;

            private int _length;

            #endregion

            #region Public Constructors

            public PDataTFStream(DicomService service, byte pcid, uint max)
            {
                _service = service;
                _command = true;
                _pcid = pcid;
                _pduMax = Math.Min(max, Int32.MaxValue);
                _max = (_pduMax == 0)
                           ? _service.Options.MaxCommandBuffer
                           : Math.Min(_pduMax, _service.Options.MaxCommandBuffer);

                _pdu = new PDataTF();

                // Max PDU Size - Current Size - Size of PDV header
                _bytes = new byte[_max - CurrentPduSize() - 6];
            }

            #endregion

            #region Public Properties

            public bool IsCommand
            {
                // ReSharper disable once UnusedMember.Local
                get
                {
                    return _command;
                }
                set
                {
                    // recalculate maximum PDU buffer size
                    if (_command != value)
                    {
                        if (value)
                            _max = (_pduMax == 0)
                                       ? _service.Options.MaxCommandBuffer
                                       : Math.Min(_pduMax, _service.Options.MaxCommandBuffer);
                        else
                            _max = (_pduMax == 0)
                                       ? _service.Options.MaxDataBuffer
                                       : Math.Min(_pduMax, _service.Options.MaxDataBuffer);

                        CreatePDV(true);
                        _command = value;
                    }
                }
            }

            #endregion

            #region Public Members

            public void Flush(bool last)
            {
                CreatePDV(last);
                WritePDU(last);
            }

            #endregion

            #region Private Members

            private uint CurrentPduSize()
            {
                // PDU header + PDV header + PDV data
                return 6 + _pdu.GetLengthOfPDVs();
            }

            // ReSharper disable once InconsistentNaming
            private void CreatePDV(bool last)
            {
                try
                {
                    if (_bytes == null) _bytes = new byte[0];

                    if (_length < _bytes.Length) Array.Resize(ref _bytes, _length);

                    PDV pdv = new PDV(_pcid, _bytes, _command, last);
                    _pdu.PDVs.Add(pdv);

                    //_service.Logger.Info(pdv);

                    // reset length in case we recurse into WritePDU()
                    _length = 0;
                    // is the current PDU at its maximum size or do we have room for another PDV?
                    if ((CurrentPduSize() + 6) >= _max || (!_command && last)) WritePDU(last);

                    // Max PDU Size - Current Size - Size of PDV header
                    uint max = _max - CurrentPduSize() - 6;
                    _bytes = last ? null : new byte[max];
                }
                catch (Exception e)
                {
                    _service.Logger.Error("Exception creating PDV: {@error}", e);
                    throw;
                }
            }

            private void WritePDU(bool last)
            {
                if (_length > 0) CreatePDV(last);

                if (_pdu.PDVs.Count > 0)
                {
                    if (last) _pdu.PDVs[_pdu.PDVs.Count - 1].IsLastFragment = true;

                    _service.SendPDU(_pdu);

                    _pdu = new PDataTF();
                }
            }

            #endregion

            #region Stream Members

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            public override void Flush()
            {
            }

            public override long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                try
                {
                    if (_bytes == null || _bytes.Length == 0)
                    {
                        // Max PDU Size - Current Size - Size of PDV header
                        uint max = _max - CurrentPduSize() - 6;
                        _bytes = new byte[max];
                    }

                    while (count >= (_bytes.Length - _length))
                    {
                        int c = Math.Min(count, _bytes.Length - _length);

                        Array.Copy(buffer, offset, _bytes, _length, c);

                        _length += c;
                        offset += c;
                        count -= c;

                        CreatePDV(false);
                    }

                    if (count > 0)
                    {
                        Array.Copy(buffer, offset, _bytes, _length, count);
                        _length += count;

                        if (_bytes.Length == _length) CreatePDV(false);
                    }
                }
                catch (Exception e)
                {
                    _service.Logger.Error("Exception writing data to PDV: {@error}", e);
                    throw;
                }
            }

            #endregion
        }

        #region Send Methods

        /// <summary>
        /// Sends the association request.
        /// </summary>
        /// <param name="association">The association.</param>
        protected void SendAssociationRequest(DicomAssociation association)
        {
            LogID = association.CalledAE;
            if (Options.UseRemoteAEForLogName) Logger = LogManager.Default.GetLogger(LogID);
            Logger.Info("{calledAE} -> Association request:\n{association}", LogID, association.ToString());
            Association = association;
            SendPDU(new AAssociateRQ(Association));
        }

        /// <summary>
        /// Sends the association accept.
        /// </summary>
        /// <param name="association">The association.</param>
        protected void SendAssociationAccept(DicomAssociation association)
        {
            Association = association;

            // reject all presentation contexts that have not already been accepted or rejected
            foreach (var pc in Association.PresentationContexts.Where(pc => pc.Result == DicomPresentationContextResult.Proposed))
            {
                pc.SetResult(DicomPresentationContextResult.RejectNoReason);
            }

            Logger.Info("{logId} -> Association accept:\n{association}", LogID, association.ToString());
            SendPDU(new AAssociateAC(Association));
        }

        /// <summary>
        /// Sends the association reject.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="source">The source.</param>
        /// <param name="reason">The reason.</param>
        protected void SendAssociationReject(
            DicomRejectResult result,
            DicomRejectSource source,
            DicomRejectReason reason)
        {
            Logger.Info(
                "{logId} -> Association reject [result: {result}; source: {source}; reason: {reason}]",
                LogID,
                result,
                source,
                reason);
            SendPDU(new AAssociateRJ(result, source, reason));
        }

        /// <summary>
        /// Sends the association release request.
        /// </summary>
        protected void SendAssociationReleaseRequest()
        {
            Logger.Info("{logId} -> Association release request", LogID);
            SendPDU(new AReleaseRQ());
        }

        /// <summary>
        /// Sends the association release response.
        /// </summary>
        protected void SendAssociationReleaseResponse()
        {
            Logger.Info("{logId} -> Association release response", LogID);
            SendPDU(new AReleaseRP());
        }

        /// <summary>
        /// Sends the abort.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="reason">The reason.</param>
        protected void SendAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            Logger.Info("{logId} -> Abort [source: {source}; reason: {reason}]", LogID, source, reason);
            SendPDU(new AAbort(source, reason));
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// Called when [send queue empty].
        /// </summary>
        protected virtual void OnSendQueueEmpty()
        {
        }

        #endregion
    }
}
