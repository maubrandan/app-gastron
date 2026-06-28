namespace Resto.Infrastructure.Identity;

public static class AppRoles
{
    public const string Waiter = "Waiter";
    public const string Manager = "Manager";
    public const string Kitchen = "Kitchen";
    public const string Admin = "Admin";

    public static readonly string[] All = [Waiter, Manager, Kitchen, Admin];
}
