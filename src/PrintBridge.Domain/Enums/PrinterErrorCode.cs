namespace PrintBridge.Domain.Enums;

public enum PrinterErrorCode
{
    None = 0,
    PAPER_OUT,
    PAPER_JAM,
    COVER_OPEN,
    OVERHEAT,
    COMM_ERROR,
    UNKNOWN_COMMAND
}
