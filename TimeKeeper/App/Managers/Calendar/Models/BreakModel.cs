namespace TimeKeeper.App.Managers.Calendar.Models
{
  class BreakModel
  {
    public string Name { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsBreakPassed
    {
      get
      {
        if (IsCompleted && EndTime.Value < DateTime.Now)
        {
          return true;

        }
        return false;
      }
    }
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
        if (IsBreakPassed)
        {
          return EndTime.Value - StartTime.Value;
        }
        return new TimeSpan();
      }
    }
  }
}
