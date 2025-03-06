using System.Text.Json.Serialization;
using TimeKeeper.Enums;

namespace TimeKeeper.Models
{
  class DayModel
  {
    public DateTime? StartTime { get; set; } = DateTime.MinValue;
    public DateTime? EndTime { get; set; }
    public TimeSpan Lunch { get; set; } = new TimeSpan(0, 30, 0);
    [JsonIgnore]
    public bool IsComplete
    {
      get
      {
        return StartTime.HasValue && EndTime.HasValue;
      }
    }
    [JsonIgnore]
    public int Id
    {
      get
      {
        return StartTime.HasValue ? StartTime.Value.Day : -1;
      }
    }
    public TimeSpan GetExpectedWorkDay()
    {
      if (StartTime.HasValue)
      {
        switch (StartTime.Value.DayOfWeek)
        {
          case DayOfWeek.Monday:
          case DayOfWeek.Tuesday:
          case DayOfWeek.Wednesday:
          case DayOfWeek.Thursday:
            return new TimeSpan(7, 30, 0);
          case DayOfWeek.Friday:
            return new TimeSpan(7, 0, 0);
          case DayOfWeek.Saturday:
          case DayOfWeek.Sunday:
            return new TimeSpan(0, 0, 0);
          default:
            return new TimeSpan(0, 0, 0);
        }
      }
      else
      {
        return TimeSpan.Zero;
      }
    }
    public TimeSpan GetActualWorkDay()
    {
      if (IsComplete)
      {
        return EndTime.Value - StartTime.Value - Lunch;
      }
      return DateTime.Now - StartTime.Value - Lunch;
    }
    public TimeSpan GetDayWorkDeficit()
    {
      return GetActualWorkDay() - GetExpectedWorkDay();
    }

  }
}
