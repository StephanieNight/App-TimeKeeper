using System.Text.Json.Serialization;

namespace TimeKeeper.Models
{
  class DayModel
  {
    public int Id { get; set; } = -1;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ExpectedWorkDay { get; set; }
    public List<BreakModel> Breaks { get; set; } = new List<BreakModel>();

    [JsonIgnore]
    public bool IsComplete
    {
      get
      {
        return StartTime.HasValue && EndTime.HasValue;
      }
    }
    [JsonIgnore]
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

    // breaks
    public void StartBreak(DateTime startTime, string name = "break")
    {
      foreach (var b in Breaks)
      {
        if (b.IsCompleted == false)
        {
          return;
        }
      }
      var newBreak = new BreakModel();
      newBreak.Name = name;
      newBreak.StartTime = startTime;
      Breaks.Add(newBreak);
    }
    public void EndBreak(DateTime endTime)
    {
      foreach (var b in Breaks)
      {
        if (b.IsCompleted == false)
        {
          b.EndTime = endTime;
          return;
        }
      }
    }

    // Time Spans
    public TimeSpan GetTotalBreaksSpan()
    {
      TimeSpan duration = new TimeSpan();
      foreach (var b in Breaks)
      {
        if (b.IsCompleted)
        {
          duration += b.Duration();
        }
      }
      return duration;
    }
    public TimeSpan GetActualWorkDay()
    {
      TimeSpan work = DateTime.Now - StartTime.Value;
      if (IsComplete)
      {
        work = EndTime.Value - StartTime.Value;
      }
      work -= GetTotalBreaksSpan();
      return work;
    }
    public TimeSpan GetDeficit()
    {
      return GetActualWorkDay() - ExpectedWorkDay;
    }
  }
}
