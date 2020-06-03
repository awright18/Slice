using NUnit.Framework;
using Refit;
using Shouldly;
using Slice.Client;
using System.Net;
using System.Threading.Tasks;
using Slice.Tests.NUnit.Infrastructure;

namespace Slice.Tests.NUnit
{
    class AddTask
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture = new TestFixture();

        public AddTask()
        {
            var httpClient = _testFixture.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Test]
        public async Task Valid_AddTask_Returns_Created_Response()
        {
            await _testFixture.ResetDatabase();
            var request = new AddTaskRequest("1", "First Task");
            var response = await _client.AddTask(request);
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            response.Headers.Location.ShouldNotBeNull();
        }

        [Test]
        public async Task Invalid_Task_Returns_Bad_Request_Response()
        {
            await _testFixture.ResetDatabase();
            var request = new AddTaskRequest("1", " ");
            var response = await _client.AddTask(request);
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Headers.Location.ShouldBeNull();
        }

        [Test]
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
