using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace Investigation.Infrastructure.Policies
{
    public static class PolicyFactory
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 3)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
}
