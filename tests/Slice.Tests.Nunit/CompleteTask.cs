using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.NUnit.Infrastructure;

namespace Slice.Tests.NUnit
{
    public class CompleteTask 
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture = new TestFixture();

        public CompleteTask()
        {
            var httpClient = _testFixture.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Test]
        public async Task When_Task_Not_Completed_Complete_Task_Succeeds()
        {
            await _testFixture.ResetDatabase();
            await _client.AddTask(new AddTaskRequest("1", "First Task"));
            
            var response = await _client.CompleteTask(new CompleteTaskRequest("1"));
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var task = await _client.GetTask(new GetTaskRequest("1"));
            task.CompletedDate.ShouldNotBeNull();
        }

        [Test]
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

        [Test]
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
