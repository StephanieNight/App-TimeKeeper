// Command class: Stores flags and their corresponding actions
using System.Text;

namespace TimeKeeper.App.Handlers
{
  class Command
  {
    public string Name { get; }
    public string Description { get; private set; }
    public Dictionary<string, Action<string[]>> Flags { get; }
    public Dictionary<string, string> FlagDescriptions { get; }
    public Action? DefaultAction { get; private set; } // Default action if no flag is provided

    public Command(string name)
    {
      Name = name;
      Flags = new Dictionary<string, Action<string[]>>();
      FlagDescriptions = new Dictionary<string, string>();
    }

    public void AddFlag(string flag, Action action)
    {
      Flags[flag] = _ => action();
    }
    public void AddFlag(string flag, Action action, string description)
    {
      AddFlag(flag, action);
      FlagDescriptions[flag] = description;
    }

    public void AddFlag(string flag, Action<string[]> action)
    {
      Flags[flag] = action;
    }
    public void AddFlag(string flag, Action<string[]> action, string description)
    {
      AddFlag(flag, action);
      FlagDescriptions[flag] = description;
    }

    public void SetDefaultAction(Action action)
    {
      DefaultAction = action;
    }
    public void SetDescription(string description)
    {
      Description = description;
    }

    public string GetHelp()
    {
      StringBuilder helpText = new StringBuilder();
      helpText.AppendLine($"{Name} : ");
      helpText.AppendLine($"Decription : {(string.IsNullOrEmpty(Description)? "N/A": Description)}");
      helpText.AppendLine($"Available flags:");
      foreach (var pair in Flags)
      {
        helpText.Append(pair.Key);
        if(FlagDescriptions.TryGetValue(pair.Key, out string desc))
        {
          helpText.Append($" {desc}");
        }
        helpText.AppendLine("");
      }
      return helpText.ToString();
    }
  }
}
