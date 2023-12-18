namespace EPR.SubmissionMicroservice.Data.Enums;

public enum AntivirusScanResult
{
    /// <summary> File has not been virus scanned. </summary>
    AwaitingProcessing = 1,

    /// <summary> File has been virus scanned. </summary>
    Success = 2,

    /// <summary> File has not been re-virus scanned within 24 hours. </summary>
    FileInaccessible = 3,

    /// <summary> File has been quarantined. </summary>
    Quarantined = 4,

    /// <summary> File has not been able to be scanned. </summary>
    FailedToVirusScan = 5,
}
