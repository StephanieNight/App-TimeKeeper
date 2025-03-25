// Command class: Stores flags and their corresponding actions
namespace TimeKeeper.App.Handlers
{
   class Command
{
    public string Name { get; }
    public Dictionary<string, Action<string[]>> Flags { get; }
    public Action? DefaultAction { get; private set; } // Default action if no flag is provided

    public Command(string name)
    {
        Name = name;
        Flags = new Dictionary<string, Action<string[]>>();
    }

    public void AddFlag(string flag, Action action)
    {
        Flags[flag] = _ => action();
    }

    public void AddFlag(string flag, Action<string[]> action)
    {
        Flags[flag] = action;
    }

    public void SetDefaultAction(Action action)
    {
        DefaultAction = action;
    }
}
}
