using System.Globalization;

namespace TimeKeeper.App.Common.Extensions
{
  static class DateTimeExtensions
  {
    public static DateTime RoundUp(this DateTime dt, TimeSpan d)
    {
      var modTicks = dt.Ticks % d.Ticks;
      var delta = modTicks != 0 ? d.Ticks - modTicks : 0;
      return new DateTime(dt.Ticks + delta, dt.Kind);
    }

    public static DateTime RoundDown(this DateTime dt, TimeSpan d)
    {
      var delta = dt.Ticks % d.Ticks;
      return new DateTime(dt.Ticks - delta, dt.Kind);
    }

    public static DateTime RoundToNearest(this DateTime dt, TimeSpan d)
    {
      var delta = dt.Ticks % d.Ticks;
      bool roundUp = delta > d.Ticks / 2;
      var offset = roundUp ? d.Ticks : 0;

      return new DateTime(dt.Ticks + offset - delta, dt.Kind);
    }
    // Get ISO 8601 week number (international standard)
    public static int GetIsoWeekNumber(this DateTime dt)
    {
      return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
          dt,
          CalendarWeekRule.FirstFourDayWeek,
          DayOfWeek.Monday);
    }

    // Get US week number (Sunday as first day of week)
    public static int GetUsWeekNumber(this DateTime dt)
    {
      return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
          dt,
          CalendarWeekRule.FirstDay,
          DayOfWeek.Sunday);
    }
  }
}
