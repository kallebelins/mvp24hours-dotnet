//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Extensions;
using Mvp24Hours.Patterns.Test.Setup;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Patterns.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class HttpClientTest
    {
        [Fact, Priority(1)]
        public async Task GetPostsAsyncByNameClass()
        {
            // arrange
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("HttpClientTest");
            var result = await client.HttpGetAsync("users");
            // assert
            Assert.NotNull(result);
        }

        [Fact, Priority(1)]
        public async Task GetPostsAsync()
        {
            // arrange
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("jsonUrl");
            var result = await client.HttpGetAsync("users");
            // assert
            Assert.NotNull(result);
        }

        [Fact, Priority(1)]
        public async Task GetPostsWithNotFoundAsync()
        {
            await Assert.ThrowsAsync<HttpStatusCodeException>(async () =>
            {
                // arrange
                var serviceProvider = Startup.InitializeHttp();
                var factory = serviceProvider.GetService<IHttpClientFactory>();
                var client = factory.CreateClient("jsonUrl");
                var result = await client.HttpGetAsync("notFound");
                // assert
                Assert.NotNull(result);
            });
        }

        [Fact, Priority(2)]
        public async Task GetIdPostsAsync()
        {
            // arrange
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("jsonUrl");
            var result = await client.HttpGetAsync("posts/1");
            // assert
            Assert.NotNull(result);
        }

        [Fact, Priority(3)]
        public async Task PostPostsAsync()
        {
            // arrange
            var dto = new
            {
                title = "foo",
                body = "bar",
                userId = 1,
            };
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("jsonUrl");
            var result = await client.HttpPostAsync("posts", dto.ToSerialize());
            // assert
            Assert.NotNull(result);
        }

        [Fact, Priority(4)]
        public async Task PutPostsAsync()
        {
            // arrange
            var dto = new
            {
                id = 1,
                title = "foo1",
                body = "bar1",
                userId = 1,
            };
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("jsonUrl");
            var result = await client.HttpPutAsync("posts/1", dto.ToSerialize());
            // assert
            Assert.NotNull(result);
        }


        [Fact, Priority(5)]
        public async Task DeletePostsAsync()
        {
            // arrange
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("jsonUrl");
            var result = await client.HttpDeleteAsync("posts/1");
            // assert
            Assert.Equal("{}", result);
        }

        [Fact, Priority(6)]
        public async Task PatchPostsAsync()
        {
            // arrange
            var dto = new
            {
                title = "foo1"
            };
            var serviceProvider = Startup.InitializeHttp();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            var client = factory.CreateClient("jsonUrl");
            var result = await client.HttpPatchAsync("posts/1", dto.ToSerialize());
            // assert
            Assert.NotNull(result);
        }
    }
}
