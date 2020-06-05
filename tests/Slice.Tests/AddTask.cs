using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.Infrastructure;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Slice.Tests
{
    public class AddTask : IClassFixture<TestFixture>
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture; 
        public AddTask(TestFixture factory)
        {
            _testFixture = factory;
            var httpClient = factory.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Fact]
        public async Task Valid_AddTask_Returns_Created_Response()
        {
            await _testFixture.ResetDatabase();
            var request = new AddTaskRequest("1", "First Task");
            var response = await _client.AddTask(request);
          
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            response.Headers.Location.ShouldNotBeNull();
        }

        [Fact]
        public async Task Invalid_Task_Returns_Bad_Request_Response()
        {
            await _testFixture.ResetDatabase();
            var request = new AddTaskRequest("2", " ");
            var response = await _client.AddTask(request);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Headers.Location.ShouldBeNull();
        }

        [Fact]
        public async Task Duplicate_Id_Returns_Bad_Request_Response()
        {
            await _testFixture.ResetDatabase();
            var request = new AddTaskRequest("1", "First Task");
            var response = await _client.AddTask(request);
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            response.Headers.Location.ShouldNotBeNull();
            response = await _client.AddTask(request);
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
