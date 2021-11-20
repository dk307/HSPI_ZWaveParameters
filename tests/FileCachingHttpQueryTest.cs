using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class FileCachingHttpQueryTest
    {
        [TestMethod]
        public void DefaultConstructorWithPath()
        {
            // checking not throws exception
            var _ = new FileCachingHttpQuery();
        }

        //[TestMethod]
        //public async Task CacheIsUpdated()
        //{
        //    var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        //    var httpClient = handler.CreateClient();

        //    string url = "http://google2.com";
        //    string data = "def";

        //    handler.SetupRequest(HttpMethod.Get, url)
        //                        .ReturnsResponse(data, "application/json");

        //    var obj = new FileCachingHttpQuery(httpClient,
        //                                       Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        //    Assert.AreEqual(await obj.GetResponseAsString(url, CancellationToken.None), data);

        //    handler.Verify();
        //    handler.Reset();

        //    handler.SetupRequest(HttpMethod.Get, url)
        //                        .ReturnsResponse(HttpStatusCode.Forbidden);

        //    Assert.AreEqual(await obj.GetResponseAsString(url, CancellationToken.None), data);

        //    handler.Verify();
        //}

        //[TestMethod]
        //public async Task CacheThrowsOnFailure()
        //{
        //    var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        //    var httpClient = handler.CreateClient();

        //    string url = "http://google2.com";

        //    handler.SetupRequest(HttpMethod.Get, url)
        //                        .ReturnsResponse(HttpStatusCode.Forbidden);

        //    var obj = new FileCachingHttpQuery(httpClient,
        //                                       Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        //    await Assert.ThrowsExceptionAsync<HttpRequestException>(() => obj.GetResponseAsString(url, CancellationToken.None));

        //    handler.Verify();
        //}
    }
}