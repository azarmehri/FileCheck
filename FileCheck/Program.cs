using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using System;
using System.Security.Cryptography;

IConfigurationRoot config = new ConfigurationBuilder()
           .AddUserSecrets<Program>()
           .AddJsonFile("conf.json")
           .Build();

using var client = new SshClient(config["SSH:Host"], int.Parse(config["SSH:Port"]), config["SSH:User"], config["SSH:Password"]);
client.Connect();

Console.WriteLine(client.ConnectionInfo.ServerVersion);

var baseFolder = "MarmosetData/";
var result = client.RunCommand($"tree -ifF --noreport {baseFolder}");
var files = result.Result.Split("\n").Where(name => !name.EndsWith('/') && !string.IsNullOrWhiteSpace(name)).ToList();


for (var i = 0; i<files.Count; i++)
{
    var file = files[i];

    Console.Write($"({i * 100.0 / (files.Count-1):F2}%) File: {file}");

    var local_file = file.Replace(baseFolder, "");

    try
    {
        var local_file_sha = SHA256CheckSum(local_file);
        var remote_file_sha = client.RunCommand($"sha256sum {file}").Result[0..64];

        if (local_file_sha != remote_file_sha) throw new Exception("checksum error.");

        Console.WriteLine(" " + client.RunCommand($"rm {file}").Result);
    }
    catch(Exception e)
    {
        Console.WriteLine($" {e.Message}");
    }

    
}


string SHA256CheckSum(string filePath)
{
    if (!File.Exists(filePath)) throw new Exception("file not exist.");

    using (SHA256 sha256 = SHA256.Create())
    {
        using (FileStream fileStream = File.OpenRead(filePath))
        {
            return BitConverter.ToString(sha256.ComputeHash(fileStream)).ToLower().Replace("-", "");
        }
    }
}