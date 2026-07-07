public static class StartupBanner
{
    public static void Write(string storageRoot, int port)
    {
        var lanIp = NetworkAddressResolver.TryGetLanIPv4();
        var localUrl = $"http://localhost:{port}";

        Console.WriteLine();
        Console.WriteLine("Upload server is running.");
        Console.WriteLine($"Storage root: {storageRoot}");
        Console.WriteLine($"Local URL: {localUrl}");

        if (lanIp is not null)
        {
            Console.WriteLine($"Phone upload URL: http://{lanIp}:{port}");
        }
        else
        {
            Console.WriteLine("Phone upload URL: Could not determine LAN IP automatically.");
        }

        Console.WriteLine();
    }
}
