#define USE_CUSTOM_LOGGER

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.ServiceEssentials;
using Microsoft.IdentityModel.S2S.Configuration;
using Microsoft.IdentityModel.S2S.Logging;

namespace JwtBearerHandlerApi
{
    public class TokenValidationUtilityMise
    {
        public static TokenValidationUtilityMise GetInstance(
             string instance = "https://login.microsoftonline.com/",
             string tenant = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
             string clientId = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc",
             string audience = null)
        {
            if (tokenValidationUtilityMiseInstance == null)
            {
                tokenValidationUtilityMiseInstance = new TokenValidationUtilityMise(instance, tenant, clientId, audience);
            }
            return tokenValidationUtilityMiseInstance;
        }

        public async Task<ClaimsPrincipal> GetClaimsAsync(IDictionary<string, StringValues> requestHeaders, CancellationToken cancellationToken = default)
        {
            ClaimsPrincipal claims = null;

            StringValues authorizationHeader;
            if (requestHeaders.TryGetValue("Authorization", out authorizationHeader))
            {
                string authorizationHeaderContent = authorizationHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(authorizationHeaderContent) && authorizationHeaderContent.Contains("Bearer"))
                {

                    // Obtain http request data from your stack
                    var httpRequestData = new HttpRequestData();
                    httpRequestData.Headers.Add("Authorization", authorizationHeaderContent);

                    /*** 1. create mise http context object (for each request) ***/
                    var context = new MiseHttpContext(httpRequestData)
                    {
                        // CorrelationId = // optional: let mise use the transaction/correlation id from you stack for a current request
                    };

                    /*** 2. execute mise (for each request) ***/
                    var miseResult = await miseHost.HandleAsync(context, cancellationToken).ConfigureAwait(false);

                    /*** 3. examine results (for each request) ***/
                    if (miseResult.Succeeded)
                    {
                        // IMPORTANT:
                        // If your application is a multi-tenant web API that accepts app-tokens, it could receive an app token
                        // from any app in any tenant if that app does not have a service principal in the tenant where your web API is running.
                        // Unless you have a scenario where you are dependent on authentication without client service principal (deprecated behavior),
                        // and provide another kind of authorization, you need to check that the `oid` claim is present. If it's not present, this indicates
                        // that there is no service principal for that client in the right tenant (described by the `tid` claim),
                        // and in that case, it should not be authorized.
                        // Lack of service principal for an app in a tenant indicates that the app was never explicitly installed or consented to use in that tenant.
                        var appIdentity = miseResult.AuthenticationTicket.ActorIdentity ?? miseResult.AuthenticationTicket.SubjectIdentity;

                        // Check that there is an oid claim  in the presented Access token
                        string oid = appIdentity.Claims.FirstOrDefault(x => x.Type == "oid"
                                          || x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                        if (oid == null)
                        {
                            string tid = appIdentity.Claims.FirstOrDefault(x => x.Type == "tid"
                                          || x.Type == "http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                            string sub = appIdentity.Claims.FirstOrDefault(x => x.Type == "sub"
                                        || x.Type == "http://schemas.microsoft.com/identity/claims/nameidentifier")?.Value;

                            logger.LogWarning($"The client '{sub}' calling the web API doesn't have a service principal in tenant '{tid}'.");
                            // and fail the authentication.
                            return null;
                        }

                        logger.LogInformation($"Request was validated successfully.");

                        /*** 3.1 examine additional headers and cookies produced by modules ***/
                        var additionalHttpResponseParams = miseResult.MiseContext.HttpSuccessfulResponseBuilder.BuildResponse(200);

                        foreach (var header in additionalHttpResponseParams.Headers)
                            logger.LogInformation($"Header - key:{header.Key} value:{string.Join(",", header.Value)}");

                        claims = new ClaimsPrincipal(miseResult.AuthenticationTicket.SubjectIdentity ?? miseResult.AuthenticationTicket.ActorIdentity);
                    }
                    else
                    {
                        logger.LogInformation($"Request validation failed.");

                        /*** 3.2 examine failure, and/or http response produced by a module that failed to handle the request ***/
                        logger.LogInformation($"Exception: {miseResult.Failure}");
                        var moduleCreatedFailureResponse = miseResult.MiseContext.ModuleFailureResponse;
                        if (moduleCreatedFailureResponse != null)
                        {
                            logger.LogInformation($"HTTP status code: {moduleCreatedFailureResponse.StatusCode}");

                            foreach (var header in moduleCreatedFailureResponse.Headers)
                                logger.LogInformation($"Header - key:{header.Key} value:{string.Join(",", header.Value)}");

                            if (moduleCreatedFailureResponse.Body != null)
                                logger.LogInformation($"HTTP Body: {Encoding.UTF8.GetString(moduleCreatedFailureResponse.Body, 0, moduleCreatedFailureResponse.Body.Length)}");
                        }
                    }
                }
            }

            return claims;
        }


        private TokenValidationUtilityMise(
             string instance = "https://login.microsoftonline.com/",
             string tenant = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
             string clientId = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc",
             string audience = null
             )
        {
            // Initialize SAL
            var aadAuthenticationOptions = new AadAuthenticationOptions
            {
                Instance = instance,
                TenantId = tenant,
                Audience = audience,
                ClientId = clientId
            };
            var s2sAuthenticationManager = S2SAuthenticationManagerFactory.Default.BuildS2SAuthenticationManager(aadAuthenticationOptions);

            logger = factory.CreateLogger<MiseHost<MiseHttpContext>>();

#if USE_CUSTOM_LOGGER
            var customLogger = new CustomLogger() { MinLogLevel = LogLevel.Information };
#endif

            // Initialize MISE
            miseHost = MiseBuilder.Create(new ApplicationInformationContainer(clientId))
                   .WithDefaultAuthentication(s2sAuthenticationManager)
                   .ConfigureDefaultModuleCollection(builder =>
                   {
                       builder.AddTrV2Module();
                   })
#if USE_CUSTOM_LOGGER
                   // SampleLoggerAdapter is a wrapper around customLogger and funnels .NET Identity logs (MISE*, S2S*, IDX*) to it
                   .WithLogger(new SampleLoggerAdapter(customLogger)) 
#endif
                   /*
                    // Add logging and telemetry if you need  
                    .WithTelemetryClient(new SampleTelemetryClient()) // optional
                    .WithLogger(logger) // optional
                    .WithLogScrubber(new SampleDataScrubber()) // optional
                   */
                   .Build();
        }

        private static MiseHost<MiseHttpContext> miseHost;

        private static ILogger<MiseHost<MiseHttpContext>> logger;

        private static readonly ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole(c =>
            {
                c.TimestampFormat = "[HH:mm:ss.ffff] ";
            });
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        static TokenValidationUtilityMise tokenValidationUtilityMiseInstance;
    }
}
