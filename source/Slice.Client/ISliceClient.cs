using Refit;
using System.Net.Http;
using System.Threading.Tasks;

namespace Slice.Client
{
    public sealed class PingResponse
    {
        public string Pong { get; } = "Pong";
    }

    public interface ISliceClient :
        IListTasks,
        IAddTask,
        IGetTask,
        IChangeDueDate,
        ICompleteTask
    {
        [Get("/api/tasks/ping")]
        Task<PingResponse> Ping();
    }
}
