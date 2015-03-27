using System;

namespace Dicom.PrintScp
{
    /// <summary>
    /// StatusUpdateEventArgs
    /// </summary>
    public class StatusUpdateEventArgs : EventArgs
    {
        public ushort EventTypeId { get; private set; }
        public string ExecutionStatusInfo { get; private set; }
        public string FilmSessionLabel { get; private set; }
        public string PrinterName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusUpdateEventArgs"/> class.
        /// </summary>
        /// <param name="eventTypeId">The event type identifier.</param>
        /// <param name="executionStatusInfo">The execution status information.</param>
        /// <param name="filmSessionLabel">The film session label.</param>
        /// <param name="printerName">Name of the printer.</param>
        public StatusUpdateEventArgs(ushort eventTypeId, string executionStatusInfo, string filmSessionLabel, string printerName)
        {
            EventTypeId = eventTypeId;
            ExecutionStatusInfo = executionStatusInfo;
            FilmSessionLabel = filmSessionLabel;
            PrinterName = printerName;
        }
    }
}