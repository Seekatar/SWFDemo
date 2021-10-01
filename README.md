# Workflow2 Backend Service

[[_TOC_]]

> Note this sample is designed to get you up and running quickly with code to cover most scenarios. Before committing your repo, make sure you remove any code you do not intend to use such as sample controllers and consumers. Your reviewers will thank you.

This is a backend service project project created from the Casualty [dotnet template](https://dev.azure.com/CCC-Casualty/Reliance/_git/CAS-Template-Service).

This service is consumes ActiveMQ messages for Workflow2 objects. The included TestClient project calls it, and if you create a matching Workflow2 API service, it will call it.



## Getting Started with Your Service

Make sure ActiveMQ is running with `./tools/Start-ActiveMqDocker.ps1`. From your root folder, use the `run.ps1` script to build and run in Docker (See [below](#using-podman-in-place-of-docker) for using [Podman](https://docs.podman.io/) instead of Docker)

``` PowerShell
# You can set $env:nexusUser and $env:nexusPass to avoid passing it in each time
.\run.ps1 -NexusUser myuser -NexusPass mypass
```

Now you can hit the Swagger UI at [http://localhost:58340/swagger](http://localhost:58340/swagger/index.html) for health checks. To exercise the service run the TestClient or create a matching API service with the template.

### Using with Visual Studio

* Open the sln in VisualStudio and build it.
* Run the Unit tests
* Run ActiveMQ locally. `.\tools\Start-ActiveMqDocker.ps1` will start one (see below if you have your own).
* It may be easiest to set the service and TestClient projects to start via the sln's `Set Startup Projects...` dialog. That way everything will start up for testing.
* If you enable SQL Proxy, it will have to be running for those features to work. You can get it from its [repo here](https://dev.azure.com/CCC-Casualty/Reliance/_git/sql-proxy-sample), which has directions for running it.
* From Swagger, or the browser, make sure the site reports 200 from http://localhost:58340/health/ready with output like this:

``` json
{
"description": "CCC.CAS.*Service",
"version": "1",
"releaseId": "1.0.0.0",
"status": "pass",
...
```

## Using run.ps1

`.\run.ps1 <taskname>` has several task for building and running the project. You can run multiple tasks in order by passing them in on the command line.

To use DockerBuild, you must pass in your Nexus creds so the container access it as shown above.

| TaskName             | Description                                                                                  |
| -------------------- | -------------------------------------------------------------------------------------------- |
| default (or nothing) | Does default tasks for building and running locally in Docker                                |
| ci                   | Does ci build tasks, currently just DockerBuild                                              |
| DockerBuild          | Runs docker build                                                                            |
| DockerRun            | Runs docker run detached                                                                     |
| DockerInteractive    | Runs docker run with interactive flags, for debugging                                        |
| DockerStop           | Stops docker if started with DockerRun                                                       |
| DotnetBuild          | Runs dotnet build on the solution                                                            |
| DotnetTest           | Runs dotnet test on the solution                                                             |
| DumpVars             | Helper to dump out variables and environment. Useful for debugging                           |
| HelmInstall          | Run helm install locally on the service. Can dry run and wait                                |
| HelmUninstall        | Run helm uninstall locally on the service                                                    |
| BuildMessage         | Build the Message nuget package used by the service                                          |
| OpenSln              | Opens the sln in the associated app (VS)                                                     |
| UpdateRunPs1         | Helper for updating the run.ps1 if you add Tasks to psakefile.ps1                            |
| StartBack            | Starts the service in the background                                                         |
| StopBack             | Stops the service started with StartBack -- Must be in the same prompt that called StartBack |
| TestClient           | Runs the TestClient                                                                          |

## Tour of the Code

```text
├───build                            # files supporting the build and deploy
├───src
│   ├───CCC.CAS.Workflow2Messages     # Shared Message and Models.
│   │   ├───build                    # files supporting the build and deploy for Messages
│   │   ├───Messages                 # Commands and Events
│   │   └───Models                   # Classes sent in Commands and Event
│   ├───CCC.CAS.Workflow2Service      # Backend service that talks AMQ and SQL Proxy
│   │   ├───Consumers                # Command and Event consumers
│   │   ├───Installers               # DI Service installers
│   │   ├───Interfaces               # Repository Interfaces
│   │   └───Repositories
│   ├───IntegrationTest              # Integration tests, code also used by TestClient
│   ├───TestClient                   # CLI app for triggering Commands and dumping Events
│   └───UnitTest
│       └───ServiceBus               # Unit tests for MassTransit since it can be tricky
│   └───packages                     # output folder for Messages nuget package
└───tools                            # Scripts for calling REST, starting ActiveMQ
```


## Troubleshooting Builds

* `=> ERROR [internal] load metadata for mcr.microsoft.com/dotnet/sdk:5.0-focal` sometime this transient error will occur when building in Docker. Check your network and try again.

## The Messages Project and NuGet Package

The Workflow2Messages project builds a NuGet package of messages used by the backend service and other services that talk to the backend service (like the API). When you use `.\run.ps1 BuildMessage -Version 1.0.0.0` command will create a local NuGet source and build the NuGet package.

> You can build Messages in VisualStudio, but the script handles creating and updating a local NuGet source for the package, as well as updating the reference in the backend service.

Once you have it working locally fine, create an Azure DevOps pipeline with ./CCC.CAS.Workflow2Messages/build/build.yml. When built in Azure DevOps, you can then update packages from the Nexus repo.

### Updating Messages

As you make changes to messages, you'll have to update references to them. Getting the correct version number will make life easier. Use this process to determine the value.

1. If published, get version number from last AzDO build (e.g. 1.0.5)
2. If only localm get the version referenced by the csproj the uses message (Service or API)
3. Use a higher version than that, preferably using the forth level, e.g. 1.0.5.1

Then build the local nuget package with `.\run.ps1 BuildMessage -Version 1.0.5.1`. This will automatically update a service project in the same sln. You'll have to manually update any other services such as the API with one of these methods:

* VS GUI: Select the CAS-Workflow2-Messages source and update
* VS Package Manager console: `update-package -source CAS-Workflow2-Messages`
* dotnet cli from csproj folder: `dotnet add package CCC.CAS.Workflow2Messages --source CAS-Workflow2-Messages`

Once tested, commit and push the message changes and create a PR -- DO NOT push the service changes since they have a local nuget reference. When the PR's CI build is finished, update the service to use the remote nuget package, and commit.

## Configuration

Configuration settings are applied from multiple sources. Each of the following sections describe each source with the subsequent section overriding any previous ones.

### Shared Settings

A CCC-specific source used only for local development, the services use `shared_appsettings.json` to store passwords and secrets outside of any repo. See [this](https://dev.azure.com/CCC-Casualty/Reliance/_wiki/wikis/Reliance.wiki/307/Workflow2-based-Projects-Updates) wiki page for details about getting access to the default file. The code looks in parent folders until it finds a file. These settings should be in that file.

| Name               | Default   |
| ------------------ | --------- |
| ActiveMq: Host     | localhost |
| ActiveMq: Username | service   |
| ActiveMq: Password | secret    |

## AppSettings.json

Values in `appsettings.json` are ones that usually are set at deploy time since they are the same across all environments. In addition to these there are ones for auth. See the Workflow2's API settings files, and Common library's [README.md](https://dev.azure.com/CCC-Casualty/Reliance/_git/CAS-Common) for details.

| Name                       | Description                           | Default                 |
| -------------------------- | ------------------------------------- | ----------------------- |
| ActiveMq: Disabled         | Set to true to turn off core ActiveMQ | false                   |
| ActiveMq: RetryCount       | For retry of consumers                | 5                       |
| ActiveMq: RetryInterval    | For retry of consumers                | 2000                    |
| SqlProxy: Disabled         | Set to true if turn off core SqlProxy | true                    |
| SqlProxy: BaseUri          |                                       | [http://localhost:5000] |
| SqlProxy: HttpRetry        | Polly retry policy                    | 3                       |
| SqlProxy: HttpRetryDelayMs | Polly retry policy                    | 3000                    |

**IMPORTANT** if you disable ActiveMQ, be sure to remove the classes that inject `IBusControl` , otherwise you will DI errors when those classes are instantiated.

## AppSettings.Development.json

Only for development to override `appsettings.json`. The sample has overrides for logging to make it more human readable.

### Properties/LaunchSettings.json

Since we deploy to Kubernetes, all parameters that may be different in an environment (Dev, QA, Prod, etc.) are set via the environment variables. To make sure things work in VS, use the `environmentVariables` section of `launchsettings.json` instead of `appsettings.json`.

Any parameters which do not need to be different per environment, can be set in `appsettings.json` (and of course can be overridden by environment variables at runtime).

### Command Line

By default, the last config source is the command line, but since we use environment variables, this is not used.

## CI/CD

After you get the initial code running locally, you'll want to build and deploy it -- baby steps. I _highly_ recommend that you start CI immediately after your initial commit to the repo, and CD to Dev also.

## Building

1. Create a Git Repo in AzDO for your new project, e.g. `CAS-Workflow2-Service`
1. Commit the code locally, add the remote and push per the AzDO directions.
1. In AzDO, create a new pipeline
1. Select `Azure Repos Git` for your code source.
1. Select your new repo
1. Pick `Existing Azure Pipelines with YAML file`
1. Select `/build/build.yml`
1. Save the yml, then click Edit and then Validate to make sure it's ok
1. IMPORTANT - Rename the pipeline to CAS-Workflow2-Service-Build so the deploy will be triggered by it.
1. Click Run and enjoy!
1. After things building and deploying ok, you'll want to create a policy on your `main` (yes, main) branch to avoid commits directly to that branch.

The `/build/build.yml` should work as-is. As you update your project you may need to alter it. The yml uses DevOps templates that call same `.\run.ps1` script that you can run locally so you have higher confidence that it will work.

## Deploying in Azure DevOps to Kubernetes

This is very similar to the previous step. You will want to update the `deploy.yml` for your environment variables and secrets. By default this deploys to the RBR AWS Kubernetes Environments all the way through prod. The Environments in the deploy are configured in AzDO with approvers. Adjust the yaml as necessary.

1. In AzDO, create a new pipeline
1. Select `Azure Repos Git` for your code source.
1. Select your new repo
1. Pick `Existing Azure Pipelines with YAML file`
1. Select `/build/deploy.yml`
1. Save the yml, then click Edit and then Validate to make sure it's ok
1. Click Run and enjoy!
1. By default, this will also be triggered by the build pipeline above, which is why naming it is important.

### Creating a Variable Group in the Azure DevOps Library

Each environment you have will have different settings, such as connection strings, ports, passwords, etc. The Release pipeline will use one or more Libraries for each Stage that have those environment-specific values.

1. Click the Library icon in AzDO
2. Create your a Variable Group with your variables, such as connection strings, etc. For naming, it's best to use all caps with underscores so the name match generated environment variables. For values to override in `appSettings.json` use double underscore to separate levels in the JSON. E.g `{ActiveMq: {Host: "value"}}` would be `ACTIVEMQ__HOST = "value"`

These are the values you'll use in the `deploy.yml`, `configMapProperties` and `secretProperties`

## Running in Local Kubernetes

Although Docker usually suffices for validating containers, it's possible to run locally in Kubernetes. The `build/helm` folder has a chart for running the service locally in Kubernetes. You will need to also run ActiveMQ in Kubernetes and currently Docker and Kubernetes deployments don't play together due to separate network stacks. `run.ps1` has `helmInstall` and `helmUninstall` tasks. As with Docker, helm will look for `shared_appsettings.json` for secrets like the ActiveMQ password.

If you don't want to use helm, the deploy.yml uses the [DevOps-Templates](https://dev.azure.com/CCC-Casualty/Reliance/_git/DevOps-Workflow2s) repo to create the manifests that can be deployed.

## Etc


### Disabling ActiveMQ

By default the sample uses ActiveMQ. To disable, turn if off in `appsettings.json` and remove the respective repositories files.

### Running an Existing ActiveMQ

`shared_appsettings.json` file will have ActiveMQ settings. If you have a different ActiveMQ, adjust those settings as needed

### Using Podman In Place of Docker

[Podman](https://docs.podman.io/) is a drop-in replacement for Docker that runs on WSL2. This project supports using podman, but you must create a PowerShell function to redirect `docker` to podman in WSL to docker as below. Then everything will work.

You can add that to you PowerShell Profile so it's always there. Edit that file via `notepad $Profile`.

```PowerShell
function docker { wsl podman @args }
```