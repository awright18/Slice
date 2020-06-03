using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Paramore.Darker;
using Paramore.Darker.Policies;
using Slice.ServerApp.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Slice.ServerApp.Tasks
{
    public sealed class ListTask
    {
        public string TaskId { get; }
        public string Title { get; }
        public string Description { get; }
        public DateTime? DueDate { get; }
        public DateTime? CompletedDate { get; }
    }

    public sealed class ListTasksQueryResult
    {
        public ListTask[] Tasks { get; }
        public ListTasksQueryResult(IEnumerable<ListTask> tasks)
        {
            Tasks = tasks.ToArray();
        }
    }

    public sealed class ListTasksQuery : IQuery<ListTasksQueryResult>
    {
        public int PageNumber { get; }
        public int PageSize { get; }

        public ListTasksQuery(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }       
    }

   
    public sealed class ListTasksQueryHandlerAsync : QueryHandlerAsync<ListTasksQuery, ListTasksQueryResult>
    {
        private readonly string _connectionString;
        public ListTasksQueryHandlerAsync(IConnectionString connectionString)
        {
            _connectionString = connectionString.ConnectionString;
        }

        [RetryableQuery(1)]
        public override async Task<ListTasksQueryResult> ExecuteAsync(ListTasksQuery query, CancellationToken cancellationToken = default)
        {
            var sql = @"SELECT TaskId, Title, Description, DueDate 
                          FROM Tasks 
                          ORDER BY TaskId
                          OFFSET ((@PageNumber - 1) * @PageSize) ROWS 
                          FETCH FIRST @PageSize ROWS ONLY";

            await using (var sqlConnection = new SqlConnection(_connectionString))
            {
                var result = await sqlConnection.QueryAsync<ListTask>(new CommandDefinition(sql, query, cancellationToken: cancellationToken));

                return new ListTasksQueryResult(result);
            }
        }
    }

    public sealed class ListTasksResponse
    {
        public ListTask[]? Tasks { get;}

        public ListTasksResponse(IEnumerable<ListTask> tasks)
        {
            Tasks = tasks.ToArray();
        }
    }

    [ApiController]
    [Route("api/Tasks/List")]
    public sealed class ListTasksController : ControllerBase
    {
        private readonly IQueryProcessor _queryProcessor;

        public ListTasksController(IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        [HttpGet()]
        public async Task<ActionResult<ListTasksResponse>> ListTasks(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _queryProcessor.ExecuteAsync(new ListTasksQuery(pageNumber, pageSize));

            return new ListTasksResponse(result.Tasks);
        }
    }
}
