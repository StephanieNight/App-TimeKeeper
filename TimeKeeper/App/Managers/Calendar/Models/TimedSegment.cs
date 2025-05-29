using System.Xml;

namespace TimeKeeper.App.Managers.Calendar.Models
{
  class TimedSegment
  {
    public string Name { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsCompleted
    {
      get
      {
        if ((StartTime.HasValue && EndTime.HasValue))
        {
          return true;
        }
        return false;
      }
    }
    public TimeSpan Duration
    {
      get
      {
        if(EndTime.HasValue)          
        {
          return EndTime.Value - StartTime.Value;
        }
        else
        {
          return DateTime.Now - StartTime.Value;
        }        
      }
    }
    public TimeSpan Elapsed
    {
      get
      {
        if (IsCompleted)
        {
          return Duration;
        }
        return DateTime.Now - StartTime.Value;
      }
    }
  }
}
