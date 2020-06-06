
## Data Access Comparison between N-Tier and Vertical Slices

### N-Tier Data Access

Below is an example of a somewhat typical repository interface you might see in a normal n-ttier interface.  Below are some questions to consider when using this pattern for data access in the typical n-tier style.

```C#
    public interface ITaskRepository
    {
         ListTask GetTask(string id);
         List<ListTask> ListTasks();
         Task AddTask(AddTaskCommand command);
         Task SetDueDate(ChangeDueDateCommand command)
         Task CompleteTask(CompleteTaskCommand command)
    }
```

Given the scenario: "I want to add the ability to delete a task."

Q: How much of the existing code would have to change? 

Q: What other components would this likely impact?

Q: How would source control be affected by multiple developers adding new capabilities to tasks? 

Q: Would adding a new capability introduce the risk of breaking other existing actions like "complete task" or "Get Task"?

### Vertical Slices Data Access

In typical vertical slices architecture you probably wouldn't have a repository but for the sake of comparison here is what it might look like.
*note: ListTask is a task on a Task List the name is used to not conflict with the System.Threading.Tasks.Task*

```C#
    public interface IGetTask
    {
         ListTask GetTask(string id);
    }
```

Given the scenario: "I want to add the ability to delete a task."

Answer the following questions.

Q: How much of the existing code would have to change?

A: *You would create a separate inteface for Deleting a task*

Q: What other components would this likely impact?

A: *Adding a Delete Task action wouldn't affect any existing components.*

Q: How would source control be affected by multiple developers adding new capabilities to tasks?

A: *There would just be files added when adding compoenents for Deleting a task.*

Q: Would adding a new capability introduce the risk of breaking other existing actions like "complete task" or "Get Task"?

A: *There would be zero risk, "Delete Task" components would not depend on existing components.*