# Sample Workflow - Add Task to the task list

## Components in Layers

### Application Layer

The AddTaskRequestValidator is responsible for making sure the api request in this case is valid. If it is not in this case it will trigger a http response with status code 400 and an error message indicating which part of the input was invalid. 

```C#
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

            //Due Date Can't Be in the Past
            RuleFor(task => task.DueDate)
                .GreaterThan(DateTime.Today.AddDays(-1));
        }
    }

```

The AddTaskController in this example is at the application layer it is the public facing interface to the application. Note:*Controllers usually only contain a single action*

```C#
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
        public async Task<IActionResult> AddTask(AddTaskRequest request)
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

```

The application layer as in most n-tier architecture depends on a domain or business layer. 

### Business Layer

In the Slice application there I'm using the AddTaskCommand as the domain layer action in this work flow.  It's job is to accept the appropriate inputs needed to create a task. 

```C#
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

```

### Data Layer
   
In this example the AddTaskCommandHandler plays a part in the business layer and the data layer. It is responsible for validating the command (not done here.) and storing the new task in the database.
If there is a failure in either part it will throw an exception back to the caller.

```C#
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
```