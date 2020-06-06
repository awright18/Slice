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
    public sealed class AddTaskCommand : Command
    {
        public string TaskId { get; }
        public string Title { get; }
        public string Description { get; }
        public DateTime? DueDate { get; }

        public AddTaskCommand(string taskId, string title, string description, DateTime? dueDate) : base(new Guid())
        {
            TaskId = taskId;
            Title = title;
            Description = description;
            DueDate = dueDate;
        }
    }

    public sealed class AddTaskCommandHandlerAsync : RequestHandlerAsync<AddTaskCommand>
    {
        private readonly string _connectionString;
        public AddTaskCommandHandlerAsync(IConnectionString connectionString)
        {
            _connectionString = connectionString.ConnectionString;
        }

        [UsePolicyAsync(CommandProcessor.RETRYPOLICYASYNC, 1)]
        public override async Task<AddTaskCommand> HandleAsync(AddTaskCommand command, CancellationToken cancellationToken = default)
        {
            var addTaskQuery = @"INSERT INTO TASKS (TaskId,Title, Description,DueDate)VALUES(@TaskId, @Title,@Description, @DueDate);";

            await using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.ExecuteAsync(new CommandDefinition(addTaskQuery, command,
                    cancellationToken: cancellationToken));
            }

            return await base.HandleAsync(command, cancellationToken);
        }
    }

    public sealed class AddTaskRequest
    {
        public string? TaskId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public sealed class AddTaskResponse
    {
        public string TaskId { get; }
        public string Title { get; }
        public string Description { get; }
        public DateTime? DueDate { get; }

        public AddTaskResponse(string id, string title, string description, DateTime? dueDate)
        {
            TaskId = id;
            Title = title;
            Description = description;
            DueDate = dueDate;
        }
    }

    public sealed class AddTaskRequestValidator : AbstractValidator<AddTaskRequest>
    {
        public AddTaskRequestValidator()
        {
            RuleFor(task => task.TaskId)
                .MinimumLength(1)
                .NotEmpty();

            RuleFor(task => task.Title)
                .MinimumLength(3)
                .MaximumLength(50)
                .NotEmpty();

            RuleFor(task => task.Description)
                .MaximumLength(150);

            RuleFor(task => task.DueDate)
                .GreaterThan(DateTime.Today.AddDays(-1));

        }
    }

    [ApiController]
    [Route("api/Tasks/Add")]
    public sealed class AddTaskController : ControllerBase
    {
        private readonly IQueryProcessor _queryProcessor;
        private readonly IAmACommandProcessor _commandProcessor;
        private readonly ILogger<AddTaskController> _logger;

        public AddTaskController(
            IQueryProcessor queryProcessor,
            IAmACommandProcessor commandProcessor,
            ILogger<AddTaskController> logger)
        {
            _queryProcessor = queryProcessor;
            _commandProcessor = commandProcessor;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<AddTaskResponse>> AddTask(AddTaskRequest request)
        {
            var task = await _queryProcessor.ExecuteAsync(new GetTaskQuery(request.TaskId));
            
            if(task != null)
            {
                return BadRequest($"Task with TaskId {request.TaskId} already exists");
            }

            await _commandProcessor.SendAsync(new AddTaskCommand(request.TaskId, request.Title, request.Description,
                request.DueDate));

            var newUrl = $@"{Request.Host}/api/tasks/{request.TaskId}";
           
            return Created(newUrl,
                new AddTaskResponse(request.TaskId, request.Title, request.Description, request.DueDate));
        }
    }
}