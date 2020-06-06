using System;
using System.Net.Http;
using Refit;
using System.Threading.Tasks;

namespace Slice.Client
{
    public sealed class SetTaskDueDateRequest
    {
        public string TaskId { get; }
        public DateTime DueDate { get; }

        public SetTaskDueDateRequest(string taskId, DateTime dueDate)
        {
            TaskId = taskId;
            DueDate = dueDate;
        }
    }

    public interface ISetTaskDueDAte
    {
        [Post("/api/Tasks/SetTaskDueDate")]
        Task<HttpResponseMessage> SetTaskDueDate(SetTaskDueDateRequest request);
    }
}
