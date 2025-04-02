namespace TimeKeeper.App.Managers.Terminal.Models
{
  class CommandActionModel
  {
    public string Flag { get; }
    public string Tag { get; }
    public string Description { get; }
    public Action<string[]> Action { get;  }
    public CommandActionModel(string flag, string tag, string description, Action<string[]> action)
    {
      Flag = flag;
      Tag = tag;
      Description = description;
      Action = action;
    }
  }
}
