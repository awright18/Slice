using Polly;
using Polly.Registry;
using System;
using Paramore.Brighter;
using Polly.Retry;

namespace Slice.ServerApp.Infrastructure
{
    public static class Policies
    {
        public const string ExceptionPolicy = "ExceptionPolicy";
    }

    public static class PolicyRegistryFactory
    {
        public static PolicyRegistry CreatePolicyRegistry()
        {
            var retryPolicyAsync = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
                });
            var circuitBreakerPolicyAsync = 
                Policy.Handle<Exception>()
                    .CircuitBreakerAsync(
                        1, 
                        TimeSpan.FromMilliseconds(500));

            var policyRegistry = new PolicyRegistry()
            {
                { CommandProcessor.RETRYPOLICYASYNC, retryPolicyAsync }, 
                { CommandProcessor.CIRCUITBREAKERASYNC, circuitBreakerPolicyAsync }
            };

            return policyRegistry;
        }
    }
}
