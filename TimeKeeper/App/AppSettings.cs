using TimeKeeper.App.Managers.Calendar;

namespace TimeKeeper.App
{
  class AppSettings
  {
    public string KeeperName { get; set; } = "Keeper";
    public bool ShowDeficit { get; set; }
    public bool ShowTotalWork { get; set; }
    public CalendarSettings Calendar { get; set; } = new CalendarSettings();
  }
}
