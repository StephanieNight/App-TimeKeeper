
namespace TimeKeeper.App.Handlers
{
  class TerminalHandler
  {
    Dictionary<string, Command> commands = new Dictionary<string, Command>();

    public void AddCommand(Command command)
    {
      commands.Add(command.Name,command);
    }
    public void ExecuteCommand(string[] parts)
    {
      string commandName = parts[0].ToLower();

      if (!commands.TryGetValue(commandName, out Command command))
      {
        Console.WriteLine($"Unknown command: {commandName}");
        return;
      }

      if (parts.Length == 1)
      {
        if(command.DefaultAction != null){
          command.DefaultAction?.Invoke(); // Run default action if available
          return;
        }
        Console.WriteLine($"Available flags for {commandName}: {string.Join(", ", command.Flags.Keys)}");
        return;
      }

      string flag = parts[1].ToLower();
      string[] flagArgs = parts.Length > 2 ? parts[2..] : new string[0];

      if (!command.Flags.TryGetValue(flag, out Action<string[]> action))
      {
        Console.WriteLine($"Unknown flag: {flag}");
        return;
      }
      action.Invoke(flagArgs);
    }

    private void ClearInputBuffer()
    {
      while (Console.KeyAvailable)
        Console.ReadKey(false); // skips previous input chars
    }
    public void WaitForKeypress()
    {
      Console.ReadKey();
    }

    public string GetInput()
    {
      return Console.ReadLine();
    }
    public void Write(string value)
    {
      Console.Write(value);
    }
    public void WriteLine(string value)
    {
      Console.WriteLine(value);
    }
    public void Seperator()
    {
      Console.WriteLine("------------- *** -------------");
    }
    public void Clear()
    {
      Console.Clear();
    }
    public string[] ParseCommand(string fullstring)
    {
      List<string> commands = new List<string>();
      string current = "";
      bool isParameter = false;
      foreach (char c in fullstring)
      {
        // Check for a split and add the command to the new 
        if (c == ' ' && isParameter == false)
        {
          commands.Add(current);
          current = "";
          continue;
        }
        if (c == '"')
        {
          // toggle parameter
          isParameter = !isParameter;
          continue;
        }
        current += c;
      }
      commands.Add(current);
      if (commands.Count == 1)
      {
        if (commands[0] == "")
        {
          return new string[0];
        }
      }
      for (int i = 0; i < commands.Count; i++)
      {
        var command = commands[i];
        command = command.ToLower();
        commands[i] = command;
      }
      return commands.ToArray();
    }
  }

}