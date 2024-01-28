# RequestsThrottling
This ejercicio is from WorkForFood.

---

Propose solution how we can restrict the number of requests for our web APIs.

---

### Technology stack

- C#
- .NET 6.0

## Implementation description
Yes, there is [RateLimiting]https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
But as for it should be custom and .net 6.0 base idea comes from [here](https://www.tpeczek.com/2017/08/implementing-concurrent-requests-limit.html) and [there](https://medium.com/@kamransadiq111/restrict-the-number-of-requests-in-net-core-without-any-library-4b73ec187774) 
Integration Tests from [here](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0) and [there](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample/tests/RazorPagesProject.Tests/IntegrationTests)
so...

### Application 1 - API
with RequestThrottlingMiddleware 
- limits the number of requests handled in parallel
- use queue for processing boost
- queue is limited by length and has item timeout

### Application 2 - Integration Tests 



## How To Run It
Just run tests


## Tags
`.net core` `c#` `asp.net core` `web api` `minimal api` `xUnit` `integration test` `mvc testing` `webapplicationfactory` `testhost` `middleware` `limited concurrent queue` `slim semaphore` `interlocked`

## Go Ahead
