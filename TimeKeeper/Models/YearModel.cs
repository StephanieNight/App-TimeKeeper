namespace TimeKeeper.Models
{
  class YearModel
  {
    public List<MonthModel> Months { get; set; } = new List<MonthModel>();
    private DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public int GetYear { get { return Date.Year; } }
    public double TotalWorkedHours()
    {
      double total = 0.0;
      foreach (MonthModel month in Months)
      {
        total += month.WorkedHours();
      }
      return total;
    }
    public double TotalDecifict()
    {
      double total = 0.0;
      foreach (MonthModel month in Months)
      {
        total += month.Deficite();
      }
      return total;
    }
  }
}
