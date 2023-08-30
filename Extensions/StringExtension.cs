namespace Extensions;

public static class StringExtension
{
    public static bool IsGood(this string str)
    {
        return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
    }
}