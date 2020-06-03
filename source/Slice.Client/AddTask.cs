using System;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;

namespace Slice.Client
{
    public sealed class AddTaskRequest
    {
        public string TaskId { get; }
        public string Title { get; }
        public string? Description { get; }
        public DateTime? DueDate { get; }

        public AddTaskRequest(string taskId, string title, 
            string? description = null, DateTime? dueDate = null )
        {
            TaskId = taskId;
            Title = title;
            Description = description;
            DueDate = dueDate;
        }
    }

    public interface IAddTask
    {
        [Post("/api/Tasks/Add")]
        [Headers("Accept:application/json")]
        Task<HttpResponseMessage> AddTask(AddTaskRequest request);
    }
}
