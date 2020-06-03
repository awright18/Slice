using System.Net;
using System.Threading.Tasks;
using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.Infrastructure;
using Xunit;

namespace Slice.Tests
{
    public class CompleteTask : IClassFixture<TestFixture>
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture;
        public CompleteTask(TestFixture factory)
        {
            _testFixture = factory;
            var httpClient = factory.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Fact]
        public async Task When_Task_Not_Completed_Complete_Task_Succeeds()
        {
            await _testFixture.ResetDatabase();
            await _client.AddTask(new AddTaskRequest("1", "First Task"));
            
            var response = await _client.CompleteTask(new CompleteTaskRequest("1"));
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var task = await _client.GetTask(new GetTaskRequest("1"));
            task.CompletedDate.ShouldNotBeNull();
        }

        [Fact]
        public async Task When_Task_Is_Completed_Complete_Task_Returns_Bad_Request()
        {
            await _testFixture.ResetDatabase();

            await _client.AddTask(new AddTaskRequest("1", "First Task"));

            try
            {
                await _client.CompleteTask(new CompleteTaskRequest("1"));
            }
            catch (ValidationApiException e)
            {
                e.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
        }

        [Fact]
        public async Task When_Task_Does_Not_Exist_Returns_Bad_Request()
        {
            await _testFixture.ResetDatabase();

            try
            {
                await _client.CompleteTask(new CompleteTaskRequest("1"));
            }
            catch (ValidationApiException e)
            {
                e.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
        }
    }
}
