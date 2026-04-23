namespace BusBooking.Api.Models;

public static class AuditAction
{
    public const string OperatorRequestApproved = "OPERATOR_REQUEST_APPROVED";
    public const string OperatorRequestRejected = "OPERATOR_REQUEST_REJECTED";
    public const string OperatorOfficeCreated = "OFFICE_CREATED";
    public const string OperatorOfficeDeleted = "OFFICE_DELETED";
    public const string BusCreated = "BUS_CREATED";
    public const string BusApproved = "BUS_APPROVED";
    public const string BusRejected = "BUS_REJECTED";
    public const string BusStatusChanged = "BUS_STATUS_CHANGED";
}
