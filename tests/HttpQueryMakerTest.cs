using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class HttpQueryMakerTest
    {
        [TestMethod]
        public async Task Query()
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = handler.CreateClient();

            const string url = "https://google2.com";
            const string data = "def";

            handler.SetupRequest(HttpMethod.Get, url)
                                .ReturnsResponse(data, "application/json");

            var obj = new HttpQueryMaker(httpClient);
            using var stream = await obj.GetUtf8JsonResponse(url, CancellationToken.None);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            Assert.AreEqual(data, reader.ReadToEnd());

            handler.Verify();
        }

        [TestMethod]
        public async Task QueryThrowsOnFailure()
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = handler.CreateClient();

            const string url = "https://google2.com";

            handler.SetupRequest(HttpMethod.Get, url)
                                .ReturnsResponse(HttpStatusCode.Forbidden);

            var obj = new HttpQueryMaker(httpClient);
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => obj.GetUtf8JsonResponse(url, CancellationToken.None));

            handler.Verify();
        }

        [TestMethod]
        public async Task QueryThrowsOnNotJson()
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = handler.CreateClient();

            const string url = "https://google2.com";

            handler.SetupRequest(HttpMethod.Get, url)
                                .ReturnsResponse("{}", "application/xml");

            var obj = new HttpQueryMaker(httpClient);

            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => obj.GetUtf8JsonResponse(url, CancellationToken.None));

            handler.Verify();
        }

        [TestMethod]
        public async Task QueryThrowsOnNotUTF8()
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = handler.CreateClient();

            const string url = "https://google2.com";

            handler.SetupRequest(HttpMethod.Get, url)
                                .ReturnsResponse(new byte[100],
                                                 "application/json", x => x.Content.Headers.ContentType.CharSet = "utf-7");

            var obj = new HttpQueryMaker(httpClient);

            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => obj.GetUtf8JsonResponse(url, CancellationToken.None));

            handler.Verify();
        }
    }
}