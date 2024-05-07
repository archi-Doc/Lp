// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Http;

namespace Netsphere.Stats;

public static class NetStatsHelper
{
    private const string IcanhazipUriIPv4 = "http://ipv4.icanhazip.com";
    private const string IcanhazipUriIPv6 = "http://ipv6.icanhazip.com";
    private const string DynDnsUri = "http://checkip.dyndns.org";
    private static readonly TimeSpan GetTimeout = TimeSpan.FromSeconds(2);

    public static async Task<AddressQueryResult> GetIcanhazipIPv4(CancellationToken cancellationToken = default)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(IcanhazipUriIPv4, cancellationToken).WaitAsync(GetTimeout).ConfigureAwait(false);
                var ipString = result.Replace("\\r\\n", string.Empty).Replace("\\n", string.Empty).Trim();
                IPAddress.TryParse(ipString, out var ipAddress);
                return new(false, IcanhazipUriIPv4, ipAddress);
            }
        }
        catch
        {
            return new(false, IcanhazipUriIPv4, default);
        }
    }

    public static async Task<AddressQueryResult> GetIcanhazipIPv6(CancellationToken cancellationToken = default)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                // httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                var result = await httpClient.GetStringAsync(IcanhazipUriIPv6, cancellationToken).WaitAsync(GetTimeout).ConfigureAwait(false);
                var ipString = result.Replace("\\r\\n", string.Empty).Replace("\\n", string.Empty).Trim();
                IPAddress.TryParse(ipString, out var ipAddress);
                return new(true, IcanhazipUriIPv6, ipAddress);
            }
        }
        catch
        {
            return new(true, IcanhazipUriIPv6, default);
        }
    }

    public static async Task<AddressQueryResult> GetDynDns(CancellationToken cancellationToken = default)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(DynDnsUri, cancellationToken).WaitAsync(GetTimeout).ConfigureAwait(false);

                var start = result.IndexOf(':');
                if (start < 0)
                {
                    return default;
                }

                var end = result.IndexOf('<', start + 1);
                if (end < 0)
                {
                    return default;
                }

                var ipString = result.Substring(start + 1, end - start - 1).Trim();
                IPAddress.TryParse(ipString, out var ipAddress);
                return new(false, DynDnsUri, ipAddress);
            }
        }
        catch
        {
            return default;
        }
    }
}
