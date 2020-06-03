using Refit;
using System;
using System.Threading.Tasks;

namespace Slice.Client
{
   

    public sealed class GetTaskResponse
    {
        public string? TaskId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
    public sealed class GetTaskRequest
    {
        public string TaskId { get; }

        public GetTaskRequest(string taskId)
        {
            TaskId = taskId;
        }
    }

    public interface IGetTask
    {
        [Get("/api/Tasks/{request.TaskId}")]
        public Task<GetTaskResponse> GetTask(GetTaskRequest request);
    }
}
