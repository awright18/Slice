using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.NUnit.Infrastructure;

namespace Slice.Tests.NUnit
{
    public class GetTask 
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture = new TestFixture();

        public GetTask()
        {
            var httpClient = _testFixture.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Test]
        public async Task When_Task_Exists_Returns_Task()
        {
            await _testFixture.ResetDatabase();
            var addTaskRequest = new AddTaskRequest("1", "First Task");
            await _client.AddTask(addTaskRequest);

            var response = await _client.GetTask(new GetTaskRequest("1"));
            response.TaskId.ShouldBe("1");
            response.Title.ShouldBe("First Task");
        }

        [Test]
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
