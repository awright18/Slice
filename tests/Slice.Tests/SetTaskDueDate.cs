using System;
using System.Net;
using System.Threading.Tasks;
using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.Infrastructure;
using Xunit;

namespace Slice.Tests
{
    public class SetTaskDueDate : IClassFixture<TestFixture>
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture;
        public SetTaskDueDate(TestFixture factory)
        {
            _testFixture = factory;
            var httpClient = factory.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Fact]
        public async Task When_Task_Exists_Set_Tasks_Due_Date()
        {
            await _testFixture.ResetDatabase();

            await _client.AddTask(new AddTaskRequest("1", "First Task"));

            var dueDate = DateTime.UtcNow.AddDays(1);

            var response = await _client.SetTaskDueDate(
                new SetTaskDueDateRequest("1", dueDate));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var task = await _client.GetTask(new GetTaskRequest("1"));

            task.DueDate.Value.Date.ShouldBe(dueDate.Date);

        }

        [Fact]
        public async Task When_Task_Does_Not_Exist_Returns_Bad_Request()
        {
            await _testFixture.ResetDatabase();

            try
            {
                var dueDate = DateTime.UtcNow.AddDays(1);

                await _client.SetTaskDueDate(
                    new SetTaskDueDateRequest("1", dueDate));
            }
            catch (ValidationApiException e)
            {
                e.Message.ShouldContain("DueDate");
                e.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
        }
    }
}
