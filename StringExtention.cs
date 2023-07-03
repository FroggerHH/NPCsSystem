namespace NPCsSystem;

public static class StringExtention
{
    public static string Localize(this string str)
    {
        return Localization.instance.Localize(str);
    }
}