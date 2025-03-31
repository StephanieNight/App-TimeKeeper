using TimeKeeper.App.Managers.Calendar.Enums;

namespace TimeKeeper.App
{
  class Settings
  {
    public string KeeperName { get; set; } = "Keeper";
    public Rounding Rounding { get; set; } = Rounding.None;
    public bool ShowDeficit { get; set; }
    public bool ShowTotalWork { get; set; }
    public Dictionary<DayOfWeek,TimeSpan> ExpectedWorkWeek { get; set; } = new Dictionary<DayOfWeek, TimeSpan>();
  }
}
