namespace TimeKeeper.App.Models
{
  class YearModel
  {
    private Dictionary<int, MonthModel> Months = new Dictionary<int, MonthModel>();
    public int Id { get; set; } = -1;
    public TimeSpan Deficit { get; set; }
    public TimeSpan WorkedHours { get; set; }
    public bool ContainMonthId(int id)
    {
      return Months.ContainsKey(id);
    }
    public List<MonthModel> GetMonths()
    {
      return Months.Values.ToList();
    }
    public MonthModel GetMonth(int id)
    {
      return Months[id];
    }
    public bool AddMonth(MonthModel month)
    {
      if (Months.ContainsKey(month.Id))
      {
        Months[month.Id] = month;
      }
      else
      {
        Months.Add(month.Id, month);
      }
      return true;
    }
    public void UpdateStatus()
    {
      if (Months.Count > 0)
      {
        TimeSpan deficit = TimeSpan.Zero;
        TimeSpan worked = TimeSpan.Zero;
        foreach (MonthModel month in Months.Values.ToArray())
        {
          month.UpdateStatus();
          deficit += month.Deficit;
          worked += month.WorkedHours;
        }
        Deficit = deficit;
        WorkedHours = worked;
      }
    }
  }
}
