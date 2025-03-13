using System.Text.Json;

namespace TimeKeeper
{
  class FileHandler
  {
    public string AppName { get; set; }
    public string BasePath { get; set; }
    public FileHandler(string appName)
    {
      AppName = appName;
      this.BasePath = $"C://{AppName}";
      if (Directory.Exists(BasePath) == false)
      {
        Directory.CreateDirectory(BasePath);
      }
    }
    public bool FileExists(string fileName)
    {
      return File.Exists($"{BasePath}/{fileName}");
    }
    public void Serialize<T>(string path, T obj)
    {
      var options = new JsonSerializerOptions
      {
        IgnoreReadOnlyProperties = true,
        WriteIndented = true
      };
      string jsonSerial = JsonSerializer.Serialize<T>(obj,options);
      string fullpath = $"{BasePath}/{path}";

      if (Directory.Exists(Path.GetDirectoryName(fullpath)) == false)
      {
        Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
      }

      File.WriteAllText(fullpath, jsonSerial);
    }
    public T Deserialize<T>(string path)
    {
      // Open the text file using a stream reader.
      using StreamReader reader = new(path);

      // Read the stream as a string.
      string jsonSerial = reader.ReadToEnd();
      return JsonSerializer.Deserialize<T>(jsonSerial);
    }
    public string[] GetFilesInFolder(string folder)
    {
      var fullpath = $"{BasePath}/{folder}";
      return Directory.GetFiles(fullpath);
    }
  }
}
