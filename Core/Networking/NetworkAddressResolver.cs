using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class NetworkAddressResolver
{
    public static object GetNetworkStatus(int port)
    {
        var details = TryGetPrimaryNetworkDetails();

        if (details is null)
        {
            return new
            {
                connected = false,
                connectionType = "offline",
                localIp = (string?)null,
                phoneUrl = (string?)null
            };
        }

        return new
        {
            connected = true,
            connectionType = details.Value.ConnectionType,
            localIp = details.Value.IpAddress,
            phoneUrl = $"http://{details.Value.IpAddress}:{port}"
        };
    }

    public static string? TryGetLanIPv4()
    {
        var details = TryGetPrimaryNetworkDetails();
        return details?.IpAddress;
    }

    private static (string IpAddress, string ConnectionType)? TryGetPrimaryNetworkDetails()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            var props = nic.GetIPProperties();

            foreach (var unicast in props.UnicastAddresses)
            {
                var address = unicast.Address;

                if (address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                if (IPAddress.IsLoopback(address))
                {
                    continue;
                }

                return (address.ToString(), GetConnectionType(nic));
            }
        }

        return null;
    }

    private static string GetConnectionType(NetworkInterface nic)
    {
        if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
        {
            return "wifi";
        }

        var name = (nic.Name ?? string.Empty).ToLowerInvariant();
        var description = (nic.Description ?? string.Empty).ToLowerInvariant();

        if (name.StartsWith("wl") || description.Contains("wifi") || description.Contains("wireless"))
        {
            return "wifi";
        }

        if (name.StartsWith("eth") || name.StartsWith("en") || description.Contains("ethernet"))
        {
            return "ethernet";
        }

        return "unknown";
    }
}
