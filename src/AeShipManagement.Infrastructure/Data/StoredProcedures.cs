namespace AeShipManagement.Infrastructure.Data;

internal static class StoredProcedures
{
    public const string UserCreate = "dbo.usp_User_Create";
    public const string UserGetById = "dbo.usp_User_GetById";
    public const string UserList = "dbo.usp_User_List";
    public const string UserRoleList = "dbo.usp_UserRole_List";
    public const string ShipCreate = "dbo.usp_Ship_Create";
    public const string ShipGetByCode = "dbo.usp_Ship_GetByCode";
    public const string ShipList = "dbo.usp_Ship_List";
    public const string UserShipAssign = "dbo.usp_UserShip_Assign";
    public const string UserShipUnassign = "dbo.usp_UserShip_Unassign";
    public const string UserShipListShipsByUser = "dbo.usp_UserShip_ListShipsByUser";
    public const string CrewListByShip = "dbo.usp_Crew_ListByShip";
    public const string FinancialReportDetail = "dbo.usp_FinancialReport_Detail";
    public const string FinancialReportSummary = "dbo.usp_FinancialReport_Summary";
}
