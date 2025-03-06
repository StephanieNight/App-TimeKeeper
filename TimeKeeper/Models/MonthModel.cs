namespace TimeKeeper.Models
{
  class MonthModel
  {
    private Dictionary<int, DayModel> Days = new Dictionary<int, DayModel>();    
    public int Id { get; set; } = -1;
    public TimeSpan Deficit { get; set; }
    public TimeSpan WorkedHours { get; set; }
    public List<DayModel> GetDays()
    {
      return Days.Values.ToList();
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
       
      UpdateDeficit();
      return true;
    }
    public void UpdateDeficit()
    {
      TimeSpan deficit = TimeSpan.Zero;
      foreach (DayModel day in Days.Values.ToArray())
      {
        if (day.IsComplete)
        {
          deficit += day.GetDeficit();
        }
      }
      Deficit = deficit;
    }
    public bool ContainDayId(int id)
    {
      return Days.ContainsKey(id);
    }

  }
}
