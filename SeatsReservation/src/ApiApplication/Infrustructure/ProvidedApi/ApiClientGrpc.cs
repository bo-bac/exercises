using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ProtoDefinitions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Infrustructure.ProvidedApi
{
    //todo: separate. all-in-one-file here is just to improve readability for challenge reviewver

    public class ProvidedApiOptions
    {
        public string Host { get; set; }
        public string ApiKey { get; set; }
        public short CacheTimeoutMinutes { get; set; }
        public short RetryCount { get; set; }
    }

    public interface IProvidedApi
    {
        Task<ICollection<showResponse>> GetAll(CancellationToken cancellationToken = default);
    }

    public class ApiClientGrpcCached : IProvidedApi
    {
        private readonly IProvidedApi _api;
        private readonly IDistributedCache _cache;
        private readonly IOptions<ProvidedApiOptions> _options;

        public ApiClientGrpcCached(
            IProvidedApi api,
            IDistributedCache cache,
            IOptions<ProvidedApiOptions> options
            )
        {
            _api = api;
            _cache = cache;
            _options = options;
        }

        public async Task<ICollection<showResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var key = nameof(GetAll);
            
            // try read from cache
            var cacheData = await _cache.GetAsync(key, cancellationToken);
            if (cacheData != null)
            {
                return JsonSerializer.Deserialize<ICollection<showResponse>>(cacheData);
            }

            // if not - read from API
            var movies = await _api.GetAll();

            // save to cache
            var expirationTime = TimeSpan.FromMinutes(_options.Value.CacheTimeoutMinutes);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            };

            cacheData = JsonSerializer.SerializeToUtf8Bytes<ICollection<showResponse>>(movies);
            await _cache.SetAsync(key, cacheData, options, cancellationToken);

            return movies;
        }
    }    

    public class ApiClientGrpc : IProvidedApi
    {
        private readonly IOptions<ProvidedApiOptions> _options;

        public ApiClientGrpc(IOptions<ProvidedApiOptions> options) => _options = options;

        public async Task<ICollection<showResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var channel =
                GrpcChannel.ForAddress($"https://{_options.Value.Host}", new GrpcChannelOptions()
                {
                    HttpHandler = httpHandler
                });
            var client = new MoviesApi.MoviesApiClient(channel);

            var headers = new Metadata
            {
                { "X-Apikey", $"{_options.Value.ApiKey}" }
            };

            //todo: here could be 'retry' implemented
            var all = await client.GetAllAsync(new Empty(), headers, cancellationToken: cancellationToken);
            all.Data.TryUnpack<showListResponse>(out var data);

            return data.Shows;
        }
    }
}