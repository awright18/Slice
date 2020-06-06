using NUnit.Framework;
using Refit;
using Shouldly;
using Slice.Client;
using System;
using System.Net;
using System.Threading.Tasks;
using Slice.Tests.NUnit.Infrastructure;

namespace Slice.Tests.NUnit
{
    public class SetTaskDueDate
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture = new TestFixture();

        public SetTaskDueDate()
        {
            var httpClient = _testFixture.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Test]
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

        [Test]
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
                e.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
        }

        [Test]
        public async Task When_Due_Date_Is_In_The_Past_Return_Bad_Request()
        {
            await _testFixture.ResetDatabase();

            try
            {
                await _client.AddTask(new AddTaskRequest("1", "First Task"));

                var dueDate = DateTime.UtcNow.AddDays(-1);

                await _client.SetTaskDueDate(
                    new SetTaskDueDateRequest("1", dueDate));
            }
            catch (ValidationApiException e)
            {
                e.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
        }
    }
}
