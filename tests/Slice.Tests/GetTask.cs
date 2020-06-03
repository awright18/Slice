using System.Net;
using System.Threading.Tasks;
using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.Infrastructure;
using Xunit;

namespace Slice.Tests
{
    public class GetTask : IClassFixture<TestFixture>
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture;

        public GetTask(TestFixture factory)
        {
            _testFixture = factory;
            var httpClient = factory.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Fact]
        public async Task When_Task_Exists_Returns_Task()
        {
            await _testFixture.ResetDatabase();
            var addTaskRequest = new AddTaskRequest("1", "First Task");
            await _client.AddTask(addTaskRequest);

            var response = await _client.GetTask(new GetTaskRequest("1"));
            response.TaskId.ShouldBe("1");
            response.Title.ShouldBe("First Task");
        }

        [Fact]
        public async Task When_Task_Does_Not_Exist_Returns_NotFound()
        {
            await _testFixture.ResetDatabase();

            try
            {
                await _client.GetTask(new GetTaskRequest("1"));
            }
            catch (ApiException e)
            {
                e.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }
        }
    }
}
