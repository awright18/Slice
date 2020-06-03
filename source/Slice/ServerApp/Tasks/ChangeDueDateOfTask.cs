using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Paramore.Brighter;
using Paramore.Brighter.Policies.Attributes;
using Paramore.Darker;
using Slice.ServerApp.Infrastructure;

namespace Slice.ServerApp.Tasks
{
    public sealed class ChangeDueDateCommand : Command
    {
        public string TaskId { get; }
        public DateTime DueDate { get; }

        public ChangeDueDateCommand(string taskId, DateTime dueDate) 
            : base(Guid.NewGuid())
        {
            TaskId = taskId;
            DueDate = dueDate;
        }
    }

    public sealed class ChangeDueDateCommandHandlerAsync 
        : RequestHandlerAsync<ChangeDueDateCommand>
    {
        private readonly string _connectionString;
        public ChangeDueDateCommandHandlerAsync(IConnectionString connectionString)
        {
            _connectionString = connectionString.ConnectionString;
        }

        [UsePolicyAsync(CommandProcessor.RETRYPOLICYASYNC, 1)]
        public override async Task<ChangeDueDateCommand> HandleAsync(ChangeDueDateCommand command, CancellationToken cancellationToken = new CancellationToken())
        {
            await using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TASKS SET DueDate = @DueDate WHERE TaskId = @TaskId";
                await connection.ExecuteAsync(new CommandDefinition(sql, command,
                    cancellationToken: cancellationToken));
            }

            return await base.HandleAsync(command, cancellationToken);
        }
    }

    public sealed class ChangeDueDateRequest
    {
        public string? TaskId { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public sealed class ChangeDueDateRequestValidator :
        AbstractValidator<ChangeDueDateRequest>
    {
        public ChangeDueDateRequestValidator()
        {
            RuleFor(req => req.TaskId)
                .MinimumLength(1)
                .NotEmpty();
            RuleFor(req => req.DueDate)
                .GreaterThan(d => DateTime.Today);
        }
    }

    [ApiController]
    [Route("api/Tasks")]
    public sealed class ChangeDueDateController: ControllerBase
    {
        private readonly IQueryProcessor _queryProcessor;
        private readonly IAmACommandProcessor _commandProcessor;

        public ChangeDueDateController(
            IQueryProcessor queryProcessor,
            IAmACommandProcessor commandProcessor)
        {
            _queryProcessor = queryProcessor;
            _commandProcessor = commandProcessor;
        }

        [HttpPost("ChangeDueDate")]
        public async Task<ActionResult> ChangeDueDate(ChangeDueDateRequest request)
        {
            var task = await _queryProcessor.ExecuteAsync(new GetTaskQuery(request.TaskId));

            if (task is null)
            {
                return BadRequest($"Task with TaskId {request.TaskId} does not exist");
            }

            await _commandProcessor.SendAsync(new ChangeDueDateCommand(request.TaskId, request.DueDate.Value));

            return Ok();
        }
    }
}
