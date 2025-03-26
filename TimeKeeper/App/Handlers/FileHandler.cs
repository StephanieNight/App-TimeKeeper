using System.Text.Json;

namespace TimeKeeper.App.Handlers
{
  class FileHandler
  {
    public string BasePath { get; set; }
    public FileHandler(string basePath)
    {
      BasePath = basePath;
      InitializeFolder(basePath);
    }
    public void InitializeFolder(string directory)
    {
      if (Directory.Exists(directory) == false)
      {
        Directory.CreateDirectory(directory);
      }
    }
    public bool FileExists(string fileName, bool includeBasePath = true)
    {
      if (includeBasePath)
        return File.Exists($"{BasePath}/{fileName}");
      return File.Exists($"{fileName}");
    }
    public bool DirectoryExists(string directory, bool includeBasePath = true)
    {
      if (includeBasePath)
        return Directory.Exists($"{BasePath}/{directory}");
      return Directory.Exists($"{directory}");
    }
    public void Serialize<T>(string path, T obj, bool includeBasePath = true)
    {
      var options = new JsonSerializerOptions
      {       
        WriteIndented = true
      };
      string jsonSerial = JsonSerializer.Serialize<T>(obj, options);

      if (includeBasePath)
      {
        path = $"{BasePath}/{path}";
      }

      if (Directory.Exists(Path.GetDirectoryName(path)) == false)
      {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
      }

      File.WriteAllText(path, jsonSerial);
    }
    public T Deserialize<T>(string path, bool includeBasePath = false)
    {
      if (includeBasePath)
      {
        path = $"{BasePath}/{path}";
      }

      // Open the text file using a stream reader.
      using StreamReader reader = new(path);

      // Read the stream as a string.
      string jsonSerial = reader.ReadToEnd();
      return JsonSerializer.Deserialize<T>(jsonSerial);
    }
    public string[] GetFilesInFolder(string folder)
    {
      var fullpath = $"{BasePath}/{folder}";
      if (DirectoryExists(fullpath, false))
      {
        return Directory.GetFiles(fullpath);
      }
      return new string[0];
    }
  }
}
