namespace Dicom_Print_SCP
{
    public enum PrintJobStatus : ushort
    {
        Pending = 1,
        Printing = 2,
        Done = 3,
        Failure = 4
    }
}