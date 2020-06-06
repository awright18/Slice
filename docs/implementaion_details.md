# Implmenation Details

## Project Structure Decisions

### One Application Project

In this application no matter what UI it has I chose a single web host to make deployment simple. An Angular front end is planned, that will live next to the apis. There are benefits to calling apis on the same host as well.

There are no separate projects for Domain or Data Access Layers. This is a big benefit compared to N-Tier architectures besides the decoupling its less stuff to have to search through and it saves time.

### Feature Folders In The Project

The Tasks feature folder is a single folder in the application it contains all the task related workflows. 

### One File Per Workflow

Each workflow is in a single file. This is the ultimate expression of the benefits of "Vertical Slices". *An alternative would be to have seperate project per Feature set, that could be hosted in any .NET core web application.*

All dependencies are in the same file in reverse order. When you are looking for a definition of a dependency it is directly above where you are at in the file. This should save lots of time searching through files or looking for definitions of members or types.

## Libraries Used For Each Workflow

The same infrastrucure and concepts are used in each workflow.

### Brighter 

Brighter is a .NET Library used for executing "Tasks" or commands. Those tasks may be handled locally or remotely. The tasks can be automatically retried, the can use circuitbreakers, logging, command sourcing and any other type of cross cutting infrastructure concerns you have. In normal use there are 2 parts to Brighter a command and a command handler. The command specifies what you want to do an what arguments or inputs passed along to do that thing. 

### Command - Brigher

A command is something you would like excuted that can succeed or fail but has no return value. If it fails then it throws and exception. This can be any arbitrary code execution you would like to have. 
The Command type in Brighter mere is for specifying what you want to do and providing an identifier for the command request.

Below is the AddTaskCommand. It inherits from Paramore.Brighter.Command and requires a Guid to be passed to it base constructor to uniquely identify the request. The AddTaskCommand also requires a TaskId and a title. In this application the database does not generate ids on insert. The reason for this will be implemented and discussed at a later date. The AddTaskComand by itself does absoultely nothing. It is used as the contractural input for the AddTaskCommandHandlerAsync

```C#
    public sealed class AddTaskCommand : Command
    {
        public string TaskId { get; }
        public string Title { get; }
        public string? Description { get; }
        public DateTime? DueDate { get; }

        public AddTaskCommand(string taskId, string title, string? description, DateTime? dueDate) : base(new Guid())
        {
            TaskId = taskId;
            Title = title;
            Description = description;
            DueDate = dueDate;
        }
    }

```

### RequestHander - Brighter

Brighter has both Commands and Events. The difference between a Command and Event is that a Command is something that you would like to happen and Event is something that has happend. There are separate base classes for those types, but the handler for both of them is RequestHandler or RequestHandlerAsync depending on how they get executed. Below is the AddTaskCommandHandlerAsync class. RequestHandlers are usually registered with the DI container so you are able to inject anything you want into their constructors, in this case a connection string is needed so an interface that knows how to get the connection string is injected. In order to handle a Command or Event you must inherit from RequestHandler(Async) and over ride the Handle(Async) method. You are given the command which contais the data to execute the command, and you can do what you like as long as you call the base.Handle(Async) method. Brighter has a pipeline which gets executed by calling the base.Handle(Async) method.

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

### Sql Execution - Dapper 

In the above AddTaskCommandHandlerAsync class there is a sql query being executed. It might be easy to miss since you don't see a bunch of ADO happening. It's using Dapper. Dapper provides several extension methods off of SqlConnection for mapping sql results to specfied object. It also maps query parameters to properties on a supplied object. The AddTaskCommand has values for TaskId, and Title which get directly mapped to @TaskId and @Title.  This save a lot of typing. There are ways to override this behavior if you need to.


### Validators - FluentValidation

### Requst Type

### Response Type

### ASP.NET ApiController

```C#
    [ApiController] //This the ApiController attribute
    [Route("api/Tasks/Add")]
    public sealed class AddTaskController : ControllerBase
    {
         // other code here

        [HttpPost]
        public async Task<IActionResult> AddTask(AddTaskRequest request)
        {
            //handle add task
        }
    }
```

The ApiController attribute is used to handle automatic request validation, and automatic model binding. When we have created an 