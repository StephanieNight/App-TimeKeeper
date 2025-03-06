namespace TimeKeeper.Models
{
  class YearModel
  {
    private Dictionary<int, MonthModel> Months = new Dictionary<int, MonthModel>();
    public int Id { get; set; } = -1;
    public TimeSpan Deficit { get; set; }
    public TimeSpan WorkedHours { get; set; }
  }
}
