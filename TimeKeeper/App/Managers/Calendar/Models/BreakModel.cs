namespace TimeKeeper.App.Managers.Calendar.Models
{
  class BreakModel
  {
    public string Name { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsCompleted
    {
      get
      {
        if (StartTime.HasValue && EndTime.HasValue)
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
        if (IsCompleted)
        {
          return EndTime.Value - StartTime.Value;
        }
        return new TimeSpan();
      }
    }
  }
}
