using System;
using System.Threading.Tasks;
using Refit;

namespace Slice.Client
{
    public sealed class ListTask
    {
        public string? TaskId { get; set; }
        public string? Title { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public sealed class ListTasksRequest
    {
        [Query]
        public int Page { get; set; } = 1;
        [Query]
        public int Size { get; set; } = 10;
    }

    public sealed class ListTasksResponse
    {
        public ListTask[] Tasks { get; set; }
    }

    public interface IListTasks
    {
        [Get("/api/Tasks/List")]
        Task<ListTasksResponse> ListTasks(ListTasksRequest request);
    }
}
