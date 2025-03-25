namespace TimeKeeper.App.Models
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
    public TimeSpan Duration()
    {
      if (IsCompleted)
      {
        return EndTime.Value - StartTime.Value;
      }
      return new TimeSpan();
    }
  }
}
