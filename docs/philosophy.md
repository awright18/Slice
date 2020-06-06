# Philosophy

Vertical Slices embodies the principals of loose coupling an high cohesion.

Loose coupling means that your software components don't depend on other components that they don't use.
High cohesion means that components that often change together should be grouped together.

When designing a "Vertical Slices" architecture we create small components that work together to accomplish a single task. Each of these components are focused on doing their part of the workflow.
In most cases there is at least one component in the UI, Applicatoin, Domain, and Data layers.

## Benefits

1. Way Less Bugs
   a. Enable new features without breaking others.
   b. Modify any existing feature without breaking others.
   c. Multiple developers can add features without merge conflicts
   d. Components could be packaged and distributed independently if needed.
2. Easy to understand
   a. Done correctly you have a few simple patterns that are used everywhere.
   b. Low concept count in implementation and low noise of surrounding workflow code. 
3. Easy to find code for a slice
   a. Slices of code in an application live near each other.
4. Spend less time sifting through irrelevant code
   a. All the code you are looking at should support the slice.
5. Spend less time in file explorer
   a. Navigate to a single file or folder to find all the code you need living together
6. Easy to test
   a. You can create slices in your tests too test a single workflow at a time
   b. Same benefits as above in your test projects
