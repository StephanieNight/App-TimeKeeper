namespace TimeKeeper.App.Managers.Calendar.Models
{
  class MonthModel
  {
    private Dictionary<int, DayModel> Days = new Dictionary<int, DayModel>();    
    public int Id { get; set; } = -1;
    public TimeSpan Deficit { get; set; }
    public TimeSpan Worked { get; set; }
    public TimeSpan AverageWorkDay { get; set; } 
    public List<DayModel> GetDays()
    {      
      return Days.Values.OrderBy(x => x.Id).ToList();
    }
    public DayModel GetDay(int id)
    {
      return Days[id];
    }
    public bool AddDay(DayModel day)
    {
      if(Days.ContainsKey(day.Id))
      {
        Days[day.Id] =  day;
      }
      else
      {
        Days.Add(day.Id, day);
      }      
      UpdateStatus();
      return true;
    }
    public void UpdateStatus()
    {
      if(Days.Count > 0)
      {
        TimeSpan deficit = TimeSpan.Zero;
        TimeSpan worked = TimeSpan.Zero;
        var completedDays = 0;
        foreach (DayModel day in Days.Values.ToArray())
        {
          if (day.IsComplete)
          {
            deficit += day.Deficit;
            worked += day.Worked;
            completedDays++;
          }
        }
        Deficit = deficit;
        Worked = worked;
        if(completedDays > 0)
        {
          AverageWorkDay = worked / completedDays;
        }        
      }
    }
    public bool ContainDayId(int id)
    {
      return Days.ContainsKey(id);
    }

  }
}
