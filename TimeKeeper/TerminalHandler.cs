namespace TimeKeeper
{
  class TerminalHandler
  {
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
      foreach(char c in fullstring)
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
      if(commands.Count == 1)
      {
        if (commands[0] == "")
        {
          return new string[0];
        }
      }
      for(int i = 0;i<commands.Count; i++)
      {
        var command = commands[i];
        command = command.ToLower();
        commands[i] = command;
      }
      return commands.ToArray();
    }
  }
}

