using TimeKeeper.Enums;

namespace TimeKeeper.Models
{
  class MonthModel
  {
    private List<DayModel> Days { get; set; } = new List<DayModel>();
    public Month Month { get; set; } = Month.January;
    public double WorkedHours()
    {
      TimeSpan totalWork = new TimeSpan();
      foreach (var day in Days)
      {
        totalWork += day.GetActualWorkDay();
      }
      return totalWork.TotalHours;
    }
    public double Deficite()
    {
      double deficit = 0.0;
      foreach (var day in Days)
      {
        if (day.IsComplete)
        {
          deficit += (day.GetActualWorkDay() - day.GetExpectedWorkDay()).TotalHours;
        }
      }
      return deficit;
    }
    public int[] GetNotCompletedDaysIndexes()
    {
      List<int> indexes = new List<int>();
      for (int i = 0; i < Days.Count; i++)
      {
        if (Days[i].IsComplete == false)
        {
          indexes.Add(i);
        }
      }
      return indexes.ToArray();
    }
    public int StartDay()
    {
      DayModel day = new DayModel();
      day.StartTime = DateTime.Now;
      Days.Add(day);
      return Days.IndexOf(day);
    }
    public bool EndDay(int index)
    {
      if (index > 0 && index < Days.Count)
      {
        Days[index].EndTime = DateTime.Now;
        return true;
      }
      return false;
    }
  }
}
