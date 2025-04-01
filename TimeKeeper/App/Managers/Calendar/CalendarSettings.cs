using TimeKeeper.App.Managers.Calendar.Enums;
using TimeKeeper.App.Managers.Calendar.Models;

namespace TimeKeeper.App.Managers.Calendar
{
    class CalendarSettings
    {
    public Rounding Rounding { get; set; } = Rounding.None;
    public Dictionary<DayOfWeek, TimeSpan> ExpectedWorkWeek { get; set; } = new Dictionary<DayOfWeek, TimeSpan>();
    public List<PlannedBreakModel> PlannedBreaks { get; set; } = new List<PlannedBreakModel>();
  }
}
