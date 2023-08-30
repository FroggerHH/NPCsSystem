using System;
using System.ComponentModel;
using System.Globalization;

namespace NPCsSystem;

[Description("Thanks to Azu ;)")]
public static class TimeUtils
{
    private static string GetCurrentTimeString()
    {
        return string.Format("<b>{0}</b>", GetCurrentTime().ToString("HH:mm"));
    }

    [Description("HH.mm")]
    public static float GetCurrentTimeValue()
    {
        var theTime = GetCurrentTime();
        if (theTime == DateTime.Now) return -1f;
        var parse = $"{theTime.Hour},{theTime.Minute}";
        var value = double.Parse(parse, new CultureInfo("nl-NL").NumberFormat);
        return (float)value;
    }

    private static DateTime GetCurrentTime()
    {
        var now = DateTime.Now;
        if (!EnvMan.instance) return now;

        var smoothDayFraction = EnvMan.instance.m_smoothDayFraction;
        var num = (int)(smoothDayFraction * 24f);
        var num2 = (int)((smoothDayFraction * 24f - num) * 60f);
        var second = (int)(((smoothDayFraction * 24f - num) * 60f - num2) * 60f);
        var theTime = new DateTime(now.Year, now.Month, now.Day, num, num2, second);
        //int currentDay = EnvMan.instance.GetCurrentDay();
        //return TimeUtils.GetCurrentTimeString(theTime);
        return theTime;
    }
}