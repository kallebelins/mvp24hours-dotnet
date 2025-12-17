//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Patterns.Test.Setup;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Patterns.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class WebRequestTest
    {
        private static string GetBaseUrl() => Startup.GetMockServer().Url;

        [Fact, Priority(1)]
        public async Task GetPostsAsync()
        {
            // arrange
            var result = await WebRequestHelper.GetAsync($"{GetBaseUrl()}/posts");
            // assert
            Assert.NotNull(result);
        }

        [Fact, Priority(2)]
        public async Task GetIdPostsAsync()
        {
            // arrange
            var result = await WebRequestHelper.GetAsync($"{GetBaseUrl()}/posts/1");
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
            var result = await WebRequestHelper.PostAsync($"{GetBaseUrl()}/posts", dto.ToSerialize());
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
            var result = await WebRequestHelper.PutAsync($"{GetBaseUrl()}/posts/1", dto.ToSerialize());
            // assert
            Assert.NotNull(result);
        }


        [Fact, Priority(5)]
        public async Task DeletePostsAsync()
        {
            // arrange
            var result = await WebRequestHelper.DeleteAsync($"{GetBaseUrl()}/posts/1");
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
            var result = await WebRequestHelper.PatchAsync($"{GetBaseUrl()}/posts/1", dto.ToSerialize());
            // assert
            Assert.NotNull(result);
        }
    }
}
