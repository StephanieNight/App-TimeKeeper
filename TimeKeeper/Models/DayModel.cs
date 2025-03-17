using System.Text.Json.Serialization;

namespace TimeKeeper.Models
{
  class DayModel
  {
    public int Id { get; set; } = -1;
    public DateTime? StartTime { get; set; } = DateTime.MinValue;
    public DateTime? EndTime { get; set; }
    public TimeSpan Lunch { get; set; } = new TimeSpan(0, 30, 0);
    public TimeOnly LunchTimeCompleted { get; set; } = new TimeOnly(11, 45, 00);
    [JsonIgnore]
    public bool IsComplete
    {
      get
      {
        return StartTime.HasValue && EndTime.HasValue;
      }
    }
    [JsonIgnore]
    public bool IsLunchComplete
    {
      get
      {
        if (EndTime.HasValue)
        {
          return TimeOnly.FromDateTime(EndTime.Value) > LunchTimeCompleted;
        }
        return TimeOnly.FromDateTime(DateTime.Now) > LunchTimeCompleted;
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
      TimeSpan work = DateTime.Now - StartTime.Value;
      if (IsComplete)
      {
        work = EndTime.Value - StartTime.Value;
      }
      if (IsLunchComplete)
      {
        work -= Lunch;
      }
      return work;
    }
    public TimeSpan GetDeficit()
    {
      return GetActualWorkDay() - GetExpectedWorkDay();    
    }
  }
}
