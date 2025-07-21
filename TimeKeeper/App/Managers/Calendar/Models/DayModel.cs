using System.Text.Json.Serialization;

namespace TimeKeeper.App.Managers.Calendar.Models
{
  class DayModel
  {
    public int Id { get; set; } = -1;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<TimedSegment> Breaks { get; set; } = new List<TimedSegment>();
    public bool IsComplete
    {
      get
      {
        return StartTime.HasValue && EndTime.HasValue;
      }
    }
    public TimeSpan Duration
    {
      get
      {
        if (IsComplete)
        {
          return EndTime.Value - StartTime.Value;
        }
        return DateTime.Now - StartTime.Value;
      }
    }
    public TimeSpan TotalBreaks
    {
      get
      {
        return GetTotalBreaksSpan();
      }
    }
    public TimeSpan Worked
    {
      get
      {
        return GetActualWorkDay();
      }
    }
    public TimeSpan ExpectedWorkDay { get; set; }
    public TimeSpan Deficit
    {
      get
      {
        return GetDeficit();
      }
    }
    // breaks
    public void AddBreak(TimedSegment breakModel)
    {
      Breaks.Add(breakModel);
    }
    public TimedSegment GetBreak(int Id)
    {
      return Breaks[Id];
    }
    // Time Spans
    private TimeSpan GetTotalBreaksSpan()
    {
      TimeSpan duration = new TimeSpan();
      foreach (var b in Breaks)
      {
        duration += b.Duration;
      }
      return duration;
    }
    private TimeSpan GetActualWorkDay()
    {
      TimeSpan work = Duration;
      if (IsComplete)
      {
        work = EndTime.Value - StartTime.Value;
      }
      work -= GetTotalBreaksSpan();
      return work;
    }
    private TimeSpan GetDeficit()
    {
      return GetActualWorkDay() - ExpectedWorkDay;
    }
  }
}
