using System.Text.Json.Serialization;

namespace TimeKeeper.App.Models
{
  class DayModel
  {
    public int Id { get; set; } = -1;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ExpectedWorkDay { get; set; }
    public List<BreakModel> Breaks { get; set; } = new List<BreakModel>();
    public bool IsComplete
    {
      get
      {
        return StartTime.HasValue && EndTime.HasValue;
      }
    }
    public bool IsOnBreak
    {
      get
      {
        foreach (var b in Breaks)
        {
          if (b.IsCompleted == false)
          {
            return true;
          }
        }
        return false;
      }
    }
    public TimeSpan Deficit
    {
      get
      {
        return GetDeficit();
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
        return new TimeSpan();
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
    // breaks
    public void AddBreak(BreakModel breakModel)
    {
      Breaks.Add(breakModel);
    }
    public BreakModel GetBreak(int Id)
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
      TimeSpan work = DateTime.Now - StartTime.Value;
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
