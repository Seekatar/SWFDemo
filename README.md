# AWS Workflow Sample

Based off older blog post [here](https://www.red-gate.com/simple-talk/development/dotnet-development/using-awss-simple-workflow-service-swf-c/)

It runs two scenarios initially

Scenario 1 runs activity 2 and 3 in parallel
```
Start -> DemoActivity1 -+-> DemoActivity2 -+-> DemoActivity4 -> End
                        |                  |
                        +-> DemoActivity3 -+
```

Scenario 2 runs activity 1 twice with a timer
```
Start -> DemoActivity1 -+-> End
        ^               |
        +---------------+
```

