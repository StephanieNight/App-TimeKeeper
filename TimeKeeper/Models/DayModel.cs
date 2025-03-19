using System.Text.Json.Serialization;

namespace TimeKeeper.Models
{
  class DayModel
  {
    public int Id { get; set; } = -1;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ExpectedWorkDay { get; set; }
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
        if (StartTime.HasValue && TimeOnly.FromDateTime(StartTime.Value) > LunchTimeCompleted)
        {
          return false;
        }
        else if (EndTime.HasValue)
        {
          return TimeOnly.FromDateTime(EndTime.Value) > LunchTimeCompleted;
        }
        return TimeOnly.FromDateTime(DateTime.Now) > LunchTimeCompleted;
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
      return GetActualWorkDay() - ExpectedWorkDay;
    }
  }
}
