namespace Resto.Infrastructure.Identity;

public static class AuthPolicies
{
    public const string Authenticated = "Authenticated";
    public const string WaiterOrManager = "WaiterOrManager";
    public const string ManagerOnly = "ManagerOnly";
    public const string KitchenOrManager = "KitchenOrManager";
    public const string StaffManagement = "StaffManagement";
}
