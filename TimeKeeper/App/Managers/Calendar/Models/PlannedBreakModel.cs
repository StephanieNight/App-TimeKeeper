namespace TimeKeeper.App.Managers.Calendar.Models
{
  class PlannedBreakModel
  {
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Name { get; set; }
    public List<DayOfWeek> ActiveOnDays { get; set; } = new List<DayOfWeek>();
  }
}
