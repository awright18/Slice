using System.Threading.Tasks;
using NUnit.Framework;
using Refit;
using Shouldly;
using Slice.Client;
using Slice.Tests.NUnit.Infrastructure;

namespace Slice.Tests.NUnit
{
    public class ListTasks 
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture = new TestFixture();

        public ListTasks()
        {
            var httpClient = _testFixture.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Test]
        public async Task When_Tasks_Exist_Tasks_Are_Returned()
        {
            await _testFixture.ResetDatabase();
            await _client.AddTask(new AddTaskRequest("1", "First Task"));

            var response = await _client.ListTasks(new ListTasksRequest());

            response.Tasks.ShouldNotBeEmpty();
        }

        [Test]
        public async Task When_No_Tasks_Exist_An_Empty_Array_Is_Returned()
        {
            await _testFixture.ResetDatabase();

            var response = await _client.ListTasks(new ListTasksRequest());
            response.Tasks.ShouldBeEmpty();
        }
    }
}
