using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Paramore.Darker;
using Slice.ServerApp.Infrastructure;
using Paramore.Darker.Policies;

namespace Slice.ServerApp.Tasks
{
    public sealed class GetTaskQuery: IQuery<GetTaskQueryResult>
    {
        public string TaskId { get; }

        public GetTaskQuery(string taskId) 
        {
            TaskId = taskId;        
        }
    }

    public sealed class GetTaskQueryResult 
    {
        public string TaskId { get; }
        public string Title { get; }
        public string Description { get; }
        public DateTime? DueDate { get; }     
        public DateTime? CompletedDate { get; set; }
    }


    public sealed class GetTaskQueryHandlerAsync : QueryHandlerAsync<GetTaskQuery,GetTaskQueryResult>
    {
        private readonly string _connectionString;
        public GetTaskQueryHandlerAsync(IConnectionString connectionString)
        {
            _connectionString = connectionString.ConnectionString;
        }
        
        [RetryableQuery(1)]
        public override async Task<GetTaskQueryResult> ExecuteAsync(GetTaskQuery command, CancellationToken cancellationToken = default)
        {
            var query = @"SELECT 
                               TaskId, 
                               Title, 
                               Description, 
                               DueDate, 
                               CompletedDate
                          FROM Tasks WHERE TaskId = @TaskId";

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
               var result =  await sqlConnection.QueryFirstOrDefaultAsync<GetTaskQueryResult>(new CommandDefinition(query, command, cancellationToken: cancellationToken));

                return result;
            }
        }
    }

    public sealed class GetTaskRequest
    {
        [FromRoute]
        public string TaskId { get; set; }     
    }

    public sealed class GetTaskResponse
    {
        public string TaskId { get; }
        public string Title { get; }
        public string? Description { get; }
        public DateTime? DueDate { get; }
        public DateTime? CompletedDate { get; }

        public GetTaskResponse(
            string taskId, 
            string title, 
            string? description, 
            DateTime? dueDate,
            DateTime? completedDate)
        {
            TaskId = taskId;
            Title = title;
            Description = description;
            DueDate = dueDate;
            CompletedDate = completedDate;
        }
    }

    public sealed class GetTaskRequestValidator : AbstractValidator<GetTaskRequest>
    {
        public GetTaskRequestValidator()
        {
            RuleFor(task => task.TaskId)
                .MinimumLength(1)
                .NotEmpty();           
        }
    }

    [ApiController]
    [Route("api/Tasks")]
    public sealed class GetTaskController : ControllerBase
    {
        private readonly IQueryProcessor _queryProcessor;

        public GetTaskController(IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }
       
        [HttpGet("{taskId}")]
        public async Task<ActionResult<GetTaskResponse>> GetTask(string taskId)
        {
            var result = await _queryProcessor.ExecuteAsync(new GetTaskQuery(taskId));

            if (result is null)
            {
                return NotFound($"A task with TaskId {taskId} was not found.");
            }

            return new GetTaskResponse(
                result.TaskId, 
                result.Title, 
                result.Description, 
                result.DueDate,
                result.CompletedDate);           
        }
    }
}