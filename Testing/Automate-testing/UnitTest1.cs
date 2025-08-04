using Moq;
using Moq.Protected;
using MyWorldBankApi.Services;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Automate_testing
{
    public class WorldBankServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHandler;
        private readonly HttpClient _httpClient;
        private readonly WorldBankService _service;

        public WorldBankServiceTests()
        {
            _mockHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHandler.Object);
            _service = new WorldBankService(_httpClient);
        }

        [Fact]
        public async Task GetIndicatorDataAsync_ValidId_ReturnsJsonData()
        {
            // Arrange
            var indicatorId = "SI.POV.DDAY";
            var mockResponse = @"[{""indicator"":{""id"":""SI.POV.DDAY"",""value"":""Poverty headcount ratio""},""data"":[{""year"":2020,""value"":9.2}]}]";
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(mockResponse)
                });

            // Act
            var result = await _service.GetIndicatorDataAsync(indicatorId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Poverty headcount ratio", result);
        }

        [Fact]
        public async Task GetIndicatorDataAsync_InvalidId_ThrowsHttpRequestException()
        {
            // Arrange
            var indicatorId = "INVALID";
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetIndicatorDataAsync(indicatorId));
        }

        [Fact]
        public async Task GetIndicatorDataAsync_LargeResponse_ProcessesQuickly()
        {
            // Arrange
            var indicatorId = "SI.POV.DDAY";
            var largeResponse = new string('{"data":"value"}'[0], 10000); // Simulate large JSON
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(largeResponse)
                });

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await _service.GetIndicatorDataAsync(indicatorId);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(stopwatch.ElapsedMilliseconds < 50, $"Processing took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}