using TimeKeeper.App.Managers.Terminal.Models;

namespace TimeKeeper.App.Managers.Terminal
{
  class TerminalManeger
  {
    Dictionary<string, CommandModel> commands = new Dictionary<string, CommandModel>();

    public TerminalManeger()
    {
      var command = new CommandModel("help");
      command.SetCommandDescription("Prints all The usage messages of every registered command");
      command.SetCommandDefaultAction(HelpCommand);
      AddCommand(command);
    }

    public void AddCommand(CommandModel command)
    {    
      commands.Add(command.Name,command);
    }
    public void ExecuteCommand(string[] parts)
    {
      string commandName = parts[0].ToLower();

      if (!commands.TryGetValue(commandName, out CommandModel command))
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
        WriteLine(command.GetHelp());
        WaitForKeypress();
        return;
      }

      string action = parts[1].ToLower();
      string[] flagArgs = parts.Length > 2 ? parts[2..] : new string[0];
      if(!command.Invoke(action, flagArgs)){
        WriteLine($"Cant Invoke action {action}, Incorect use of command {command.Name}");
      }
    }
    public void HelpCommand()
    {
      foreach(var command in commands.Values)
      {
        if(command.Name == "help")
        {
          continue;
        }
        WriteLine(command.GetHelp());
      }
      WaitForKeypress();
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
    public void WriteLine(string value="")
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