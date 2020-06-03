using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Policies.Attributes;
using Paramore.Darker;
using Slice.ServerApp.Infrastructure;

namespace Slice.ServerApp.Tasks
{
    public class CompleteTaskCommand : Command
    {
        public string TaskId { get; }

        public CompleteTaskCommand(string taskId):base(Guid.NewGuid())
        {
            TaskId = taskId;
        }
    }

    public sealed class CompleteTaskCommandHandlerAsync
        : RequestHandlerAsync<CompleteTaskCommand>
    {
        private readonly IConnectionString _connectionString;

        public CompleteTaskCommandHandlerAsync(IConnectionString connectionString)
        {
            _connectionString = connectionString;
        }
        
        [UsePolicyAsync(CommandProcessor.RETRYPOLICYASYNC, 1)]
        public override async Task<CompleteTaskCommand> HandleAsync(CompleteTaskCommand command, CancellationToken cancellationToken = new CancellationToken())
        {
            var sql = @"UPDATE TASKS 
                        SET CompletedDate = GetDate()
                        WHERE TaskId = @TaskId ";

            await using (var connection = new SqlConnection(_connectionString.ConnectionString))
            {
               await connection.ExecuteAsync(new CommandDefinition(sql, command, cancellationToken: cancellationToken));
            }

            return await base.HandleAsync(command, cancellationToken);
        }
    }

    public sealed class CompleteTaskRequest
    {
        public string? TaskId { get; set; }
    }

    public class CompleteTaskRequestValidator : AbstractValidator<CompleteTaskRequest>
    {
        public CompleteTaskRequestValidator()
        {
            RuleFor(req => req.TaskId)
                .MinimumLength(1)
                .NotEmpty();
        }
    }

    [ApiController]
    [Route("api/Tasks")]
    public sealed class CompleteTaskController : ControllerBase
    {
        private readonly IQueryProcessor _queryProcessor;
        private readonly IAmACommandProcessor _commandProcessor;
        private readonly ILogger<CompleteTaskController> _logger;

        public CompleteTaskController(
            IQueryProcessor queryProcessor,
            IAmACommandProcessor commandProcessor, 
            ILogger<CompleteTaskController> logger)
        {
            _queryProcessor = queryProcessor;
            _commandProcessor = commandProcessor;
            _logger = logger;
        }

        [HttpPost("Complete")]
        public async Task<ActionResult> CompleteTask(CompleteTaskRequest request)
        {
            var task = await _queryProcessor.ExecuteAsync(new GetTaskQuery(request.TaskId));

            if (task is null)
            {
                return BadRequest($"Task with TaskId {request.TaskId} does not exist");
            }

            await _commandProcessor.SendAsync(new CompleteTaskCommand(request.TaskId));

            return Ok();
        }
    }
}
