﻿using Newtonsoft.Json;
using Nexar.Client.Token;
using System.Net.Http.Headers;
using System.Text;

namespace SupplyQueryDemo
{
    internal static class SupplyClient
    {
        // access tokens expire after one day
        private static readonly TimeSpan tokenLifetime = TimeSpan.FromDays(1);

        // assume Nexar client ID and secret are set as environment variables
        private static readonly string clientId = Environment.GetEnvironmentVariable("NEXAR_CLIENT_ID") ?? throw new InvalidOperationException("Please set environment variable 'NEXAR_CLIENT_ID'");
        private static readonly string clientSecret = Environment.GetEnvironmentVariable("NEXAR_CLIENT_SECRET") ?? throw new InvalidOperationException("Please set environment variable 'NEXAR_CLIENT_SECRET'");

        // keep track of token and expiry time
        private static string? token = null;
        private static DateTime tokenExpiresAt = DateTime.MinValue;

        internal static HttpClient CreateClient()
        {
            // create and configure the supply client
            HttpClient supplyClient = new()
            {
                BaseAddress = new Uri("https://api.nexar.com/graphql")
            };

            return supplyClient;
        }

        internal static async Task PopulateTokenAsync(this HttpClient supplyClient)
        {
            // get an access token, replacing the existing one if it has expired
            if (token == null || DateTime.UtcNow >= tokenExpiresAt)
            {
                tokenExpiresAt = DateTime.UtcNow + tokenLifetime;
                using HttpClient authClient = new();
                token = await authClient.GetNexarTokenAsync(clientId, clientSecret);
            }

            // set the default Authorization header so it includes the token
            supplyClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        internal static async Task<Response> RunQueryAsync(this HttpClient supplyClient, Request request)
        {
            // for another way of running GraphQL queries, see the related demo at:
            // https://github.com/NexarDeveloper/nexar-templates/tree/main/nexar-console-supply
            string requestString = JsonConvert.SerializeObject(request);
            HttpResponseMessage httResponse = await supplyClient.PostAsync(supplyClient.BaseAddress, new StringContent(requestString, Encoding.UTF8, "application/json"));
            httResponse.EnsureSuccessStatusCode();
            string responseString = await httResponse.Content.ReadAsStringAsync();
            Response response = JsonConvert.DeserializeObject<Response>(responseString);
            return response;
        }
    }
}