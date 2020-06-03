using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Refit;

namespace Slice.Client
{
    public sealed class CompleteTaskRequest
    {
        public string TaskId { get; }

        public CompleteTaskRequest(string taskId)
        {
            TaskId = taskId;
        }
    }

    public interface ICompleteTask
    {
        [Post("/api/Tasks/Complete")]
        Task<HttpResponseMessage> CompleteTask(CompleteTaskRequest request);
    }
}
