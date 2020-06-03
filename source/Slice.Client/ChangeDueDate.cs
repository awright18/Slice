using System;
using System.Net.Http;
using Refit;
using System.Threading.Tasks;

namespace Slice.Client
{
    public sealed class ChangeDueDateRequest
    {
        public string TaskId { get; }
        public DateTime DueDate { get; }

        public ChangeDueDateRequest(string taskId, DateTime dueDate)
        {
            TaskId = taskId;
            DueDate = dueDate;
        }
    }

    public interface IChangeDueDate
    {
        [Post("/api/Tasks/ChangeDueDate")]
        Task<HttpResponseMessage> ChangeDueDate(ChangeDueDateRequest request);
    }
}
