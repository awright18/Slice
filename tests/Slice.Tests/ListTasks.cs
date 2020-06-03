using Refit;
using Slice.Client;
using Slice.Tests.Infrastructure;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Slice.Tests
{
    public class ListTasks : IClassFixture<TestFixture>
    {
        private readonly ISliceClient _client;
        private readonly TestFixture _testFixture;
        public ListTasks(TestFixture factory)
        {
            _testFixture = factory;
            var httpClient = factory.CreateClient();
            _client = RestService.For<ISliceClient>(httpClient);
        }

        [Fact]
        public async Task When_Tasks_Exist_Tasks_Are_Returned()
        {
            await _testFixture.ResetDatabase();
            await _client.AddTask(new AddTaskRequest("1", "First Task"));

            var response = await _client.ListTasks(new ListTasksRequest());

            response.Tasks.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task When_No_Tasks_Exist_An_Empty_Array_Is_Returned()
        {
            await _testFixture.ResetDatabase();

            var response = await _client.ListTasks(new ListTasksRequest());
            response.Tasks.ShouldBeEmpty();
        }
    }
}
