using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JwtBearerHandlerApi;
using Microsoft.Extensions.Primitives;

namespace MISENetFrameworkTests
{
    internal class Program
    {
        const string TestTokenFile = @"e:\c_temp\testtoken.txt";

        static async Task Main(string[] args)
        {
            try
            {
                await Test(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static async Task Test(string[] args)
        {
            var jwt = File.ReadAllText(TestTokenFile);

            var validator = TokenValidationUtilityMise.GetInstance(
                instance: "https://login.windows-ppe.net/",
                tenant: "83abe5cd-bcc3-441a-bd86-e6a75360cecc",
                clientId: "1950a258-227b-4e31-a9cf-717495945fc2",
                audience: "https://management.core.windows.net/");

            var requestHeaders = new Dictionary<string, StringValues>
            {
                { "Authorization", new StringValues($"Bearer {jwt}") }
            };

            var claims = await validator.GetClaimsAsync(requestHeaders);

            Console.WriteLine(claims);
        }
    }
}
