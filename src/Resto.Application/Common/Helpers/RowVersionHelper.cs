namespace Resto.Application.Common.Helpers;

public static class RowVersionHelper
{
    public static string ToBase64(byte[] rowVersion) =>
        Convert.ToBase64String(rowVersion);

    public static byte[] FromBase64(string rowVersion) =>
        Convert.FromBase64String(rowVersion);
}
