namespace TimeKeeper
{
  class Settings
  {
    public string KeeperName { get; set; } = "Keeper";
    public Rounding Rounding { get; set; } = Rounding.None;
    public bool ShowDeficit { get; set; }
    public bool ShowTotalWork { get; set; }
  }
}
