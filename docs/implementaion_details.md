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

### Command Execution - Brighter

Brighter uses an instance of IAmACommandProcessor to execute commands. It works in a similar way to the IQueryProcessor in Darker. You will typically inject it in to the class that needs to execute a command. Then you call Send(Aync) or Post(Async).

If you have a handler in the same running process you would call Send(Async). If you intend to have your Command sent outside of your process for processing (like to a queue) then you would use Post(Async). The details of post are discussed in [brighter's documenation](https://paramore.readthedocs.io/en/latest/ImplementingDistributedTaskQueue.html) and no further comments will be made here.

If Send(Async) or Post(Async) fails an exception will be thrown. Brighter has several policy types for handling exception cases including retry,circuit breaker, and fallback policies. These are all in their documentation. In this case the handler is running in the same applicatina and process so it calls SendAsync.

When calling SendAsync you pass in the instance of a Command or Event that you want your system to hand. You are only specifying your intent. In this example I want you to Add a new Task and please do it asynchronously. We don't have to know how the handle was implemented. The caller of ExeucteAsync may not have even written the GetTaskHandlerAsync, it could be in a library. All they need to know is they want to Add a Task and they need to Send the AddTaskCommand in order to achieve this. This is the power of this type of separation. 

Side Note: *The IAmA prefix is silly and a clever spin on the I prefix for interfaces in .NET. It shows up in a few other well known projects as well*

```C# 
    //This is simplified for this example
    public sealed class AddTaskController : ControllerBase
    {
        private readonly IAmACommandProcessor _commandProcessor;

        public AddTaskController
        (
            IAmACommandProcessor commandProcessor,
        )
        {
            _commandProcessor = commandProcessor;
        }

        [HttpPost]
        public async Task<IActionResult> AddTask(AddTaskRequest request)
        {
            await _commandProcessor.SendAsync(
                new AddTaskCommand(request.TaskId,request.Title,request.Description,request.DueDate));
        }
    }
```

### Darker

Darker is a library built to abstract how queries are performed. Its the counter part to Brighter.

A query in its strictest sense is a function that returns a value. This is the opposite of a command which never returns a value. If you need to return a value use a query. If you don't then use a command.

You define the query intent and parameters seperatly from how the query is executed. A simple example of when this might come in handy is if you want to query a database vs querying an api. In this case you are not stuck mixing your meddling with your querying infrastructure to achieve both goals. This provides a uniform easy to learn and use abstraction for all types of queries.

### Query - Darker

The GetTaskQuery defines it's intent in the name. GetTaskQuery is named so because it wants to Get a Task from somewhere.

The GetTaskQuery class inherits from IQuery<T> where T is the class representing the return Type of the query. In this case T is GetTaskQueryResult. 

When using darker you are required to explicitly tell it what type to expect. This helps with the DI registration and mapping handlers to queries. It also allows you to have one single generic interface IQuery<T> be used for a many queries as you like as long as the have a distinct T. The Query and QueryResult classes you define should be one to one, this will avoid many problems in the future.

DO NOT TRY TO SHARE QUERY RESULT TYPES. We all too often want to share classes in C#. As a general rule we want to avoid the tempation of reuse of business classes because this introduces coupling which and have rippling effects on your software. You can and should share abstractions and behaviors (functions/methods) like interfaces with methods and their implementations. 

```C#
    public sealed class GetTaskQuery: IQuery<GetTaskQueryResult>
    {
        public string TaskId { get; }

        public GetTaskQuery(string taskId)
        {
            TaskId = taskId;
        }
    }
```

### QueryHandler - Darker

QueryHandlers are exactly what you think they are they accept a query allow you to take action on them and then they return the expected result.

In the example below you can see the naming convention. The name GetTaskQueryHandlerAsync should give away its purpse. It handles GetTaskQuery using an asynchronous request. The inherited class QueryHandlerAsync<TQuery,TQueryResult> once again makes it easy for mapping queries to their handlers. In this case GetTaskQuery is the query we want to handle and GetTaskQueryResult is the expected output. 
This also allows the method ExecuteAsync to understand which types are expected as input and output as ExecuteAsync is defined as follows.

``` C#
Task<TQueryResult> ExecuteAsync<TQuery,TQueryResult>(TQuery query, CancellationToken cancellationToken)
```

When using QueryHandler(Async) you override the Execute(Async) method with whatever code you want to perform the query.  This could be a sql query, a in memory cache lookup, and api call or just a value that returns a new instance of the TQueryResult type, or even null.

In the example below Dapper is used to execute a sql query and map the results to the QueryResult. Its not always that simple, but often times it is. 

```C#
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
```

### Query Execution - Darker

Executing a Darker query is straightforward.  It requires and instance of Paramore.Darker.IQueryProcessor

Typically IQueryProcessor is injected in to the class that needs to execute queries. It has both a Execute<TQuery>(TQuery query) and Execute<TQuery>(TQuery query) methods so that you can choose how to execute your queries. Deciding which to use depends on the handlers that you have created for the query you are trying to execute. Typically I stick with Async because I'm performing some form of IO. Calling Execute(Async) will end up executing the registered handler for the query and calling its predefined Execute(Async) method returning its predefined QueryResult or throwing an exception.

```C#
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
            //Execute the Query
            var result = await _queryProcessor.ExecuteAsync(new GetTaskQuery(taskId));

            //result is of the type GetTaskQueryResult
        }
    }
```

### Registering Darker Queries with the DI container

This library is typically used in Asp.Net Core and therefore has an extension method for registering query handlers.

```C#
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDarker() //registers al the darker mapping stuff
                .AddHandlersFromAssemblies(typeof(GetTaskQuery).Assembly) // registers the query handlers async or sync.
                .AddDefaultPolicies(); //registers Polly policies.
    }

```

### Sql Execution - Dapper

In the below example there is a sql query being executed. It might be easy to miss since you don't see a bunch of ADO happening. Its using Dapper. Dapper provides several extension methods off of SqlConnection for mapping sql results to specfied object. It also maps query parameters to properties on a supplied object. The AddTaskCommand has values for TaskId, and Title which get directly mapped to @TaskId and @Title.  This save a lot of typing. There are ways to override this behavior if you need to.

```C#
    var addTaskQuery = @"INSERT INTO TASKS (TaskId,Title, Description,DueDate)VALUES(@TaskId, @Title,@Description, @DueDate);";

    await using (var sqlConnection = new SqlConnection(_connectionString))
    {
        await sqlConnection.ExecuteAsync(new CommandDefinition(addTaskQuery, command,
            cancellationToken: cancellationToken));
    }

```

### Request Type

For every post request in this application I created a specific class to accept the inputs that would match with the expected JSON payload for this exact action. The advantage of creating a class is if you need to add an input to the payload then you don't have to change the action method decleration.

This class is ONLY used for this endpoint. We don't have a shared Resource Model that is used by other actions. This allows it to evolve independently of all other endpoints.

This example includes the use of nullable reference types (indicated by the ? suffix to the type declerations) to indicate to the compiler a caller may not send me all of this data. The runtime validation of this request will happen later in the AddTaskRequestValidator. If non-nullable refernece types were used here the compiler would expect these properties to be initialied with defaults or to have a constructor that requires them.

```C#
public sealed class AddTaskRequest
    {
        public string? TaskId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
    }
```

### Response Type

In any scenario where custom data is returned to the caller I will create a specific response class. Like the Request class, this allows the response to have its own shape and change independently of any other actions. The only class that depends on this should be the matching controller action.

You hopefully will notice in the response the TaskId and Title are non-nullable reference types. That means the compiler will at the very least issue a warning if any code that references them could assign a null value to them.

You probably also notice this class has a constructor. This is an example of the fundamental reason for constructors. It tells the user what values are required in order to create an instance of this class. In this example description and due date may not have a value, so they are allowed to be null.

```C#
 public sealed class AddTaskResponse
    {
        public string TaskId { get; }
        public string Title { get; }
        public string? Description { get; }
        public DateTime? DueDate { get; }

        public AddTaskResponse(string id, string title, string? description, DateTime? dueDate)
        {
            TaskId = id;
            Title = title;
            Description = description;
            DueDate = dueDate;
        }
    }

```

### Http Request Validation - FluentValidation

The choice to use the FluentValidation library was simple. It keeps validation logic separate from all other logic.

The example below is creating a class that will be used to validate the AddTaskRequest. The way you setup the validation is by creating a constructor for your validator and then calling the RuleFor methods in it.  RuleFor<T> to be more exact where T is the Type you are validating.  This gives it the ability to access the properties on the T type. This style give you a lot of flexibility in the rules that you can execute as oppposed to the DataAnnotations built into the framework. You can run any logic here that you want, and if you are using MVC many of these validations can be run on the client and the server. 
There are several examples below. You can see several of the built in validation extensions below checking for empty or specific lengths even greather than.

When you wire this up to your DI container in ASP.NET core this will automatically send 400 reponses with explanations to the caller when any of these rules are violated.

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

            RuleFor(task => task.DueDate)
                .GreaterThan(DateTime.Today.AddDays(-1));
        }
    }
```

### Registering Fluent Validation In a DI Container

Registering your validators is pretty simple as well. Here is an example of how to do that in ASP.NET Core. The AddControllersWithViews follows the builder pattern it returns an IMVCBuilder interface which AddFluentValidation hooks makes the controller buliding process extensible. In this case AddFluentValidation adds its changes to the Builder and then returns the IMVCBuilder in case somethings wants to also particpate in the build process. 

The v in the AddFluentValiation lambda expression is a configuration object used for FluentValidation to allow you to configure its options. In this case we are telling it where to find our AbstractValidators. The are in the assembly that contains the type AddTaskRequestValiator. This will scan all the assemblies and hook them up in the IMVCBuilder pipeline. 

```C#
     public void ConfigureServices(IServiceCollection services)
     {
         services.AddControllersWithViews()
                .AddFluentValidation(v =>
                    v.RegisterValidatorsFromAssembly(typeof(AddTaskRequestValidator).Assembly)); // register all AbstractValidators in the Assembly
     }

```

### ASP.NET ApiController

In the example below there are severl interesting things.

The ApiController attribute is th e first. It allows us to get our model validation (what FluentValidation above is doing) with out any additional code in the controller. It also allows us to omit adding a FromBody attribute to the AddTaskRequest parameter used in model binding. It can also help with generating swagger.  If you want more details about the ApiController attribute look [here](https://www.strathweb.com/2018/02/exploring-the-apicontrollerattribute-and-its-features-for-asp-net-core-mvc-2-1/)

The Route attriubte used at the class level establishes the base route for the controller, any routes specified in HttpGet or HttpPost attributes on actions are assumed to be extra path segmants off of the route defined here. In this case since the HttpPost attribute doesn't have arguments the endpoint to post to is /api/Tasks/Add

HttpPost and HttpGet do accept a string representing the further path segments as arguments.

The inherited ControllerBase gives you simplified methods for returning different status codes with our without additional data. In the example below Created is a method defined on the ControllerBase which wraps the object passed to it and creates a 201 CREATED response message.  There are many more of these methods like BadRequest,InternalServerError, Unauthorized, etc.

The return type in this context uses ActionResult<T> where T is the expected type returned in an 200 OK scenario. Using ActionResult<T> allows the compier and runtime to understand the expected type so things like swagger generation will understand the response. This also allow you to return either an HttpResponseMessage or just a T with out having to specify the 200 OK response.


```C#
    [ApiController] //This the ApiController attribute
    [Route("api/Tasks/Add")]
    public sealed class AddTaskController : ControllerBase
    {
         // other code here

        [HttpPost]
        public async Task<ActionResult<AddTaskResponse>> AddTask(AddTaskRequest request)
        {
            //handle add task

              return Created(newUrl,
                new AddTaskResponse(request.TaskId, request.Title, request.Description, request.DueDate));   
        }
    }
```

## Creating API Clients

## Testing Web Apis

## Database Integration Testing