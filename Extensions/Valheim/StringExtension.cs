namespace Extensions.Valheim;

public static class StringExtension
{
    public static string Localize(this string str)
    {
        return Localization.instance.Localize(str);
    }
}