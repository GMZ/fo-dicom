namespace Dicom.PrintScp
{
    public enum PrintJobStatus : ushort
    {
        Pending = 1,
        Printing = 2,
        Done = 3,
        Failure = 4
    }
}