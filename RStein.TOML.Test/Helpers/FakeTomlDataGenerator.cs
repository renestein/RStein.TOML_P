using System.Text;

namespace RStein.TOML.Test.Helpers;

public class FakeTomlDataGenerator
{
  public static void GenerateBigIniFile(string filePath, long numberOfKeys =  1 << 16)
  {
    using var streamWriter = File.CreateText(filePath);
    var hashSetKeys = new HashSet<string>();
    for (var i = 0; i < numberOfKeys; i++)
    {
      var valueSize = Random.Shared.Next(1, 100);
      string key;
      do
      {
        var keySize = Random.Shared.Next(1, 30);
        key = new string(Enumerable.Range(0, keySize).Select(_ => (char) Random.Shared.Next(97, 123)).ToArray());
      } while (!hashSetKeys.Add(key));

      var value = new string(Enumerable.Range(0, valueSize).Select(_ => (char) Random.Shared.Next(97, 123)).ToArray());
      streamWriter.WriteLine($"{key} = '{value}'");
    }
  }
}