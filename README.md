# EPR Submission Status API
## Overview
Submission Status API is a database wrapper for file upload services

## How To Run

### Prerequisites
In order to run the service you will need the following dependencies
- .NET 8

#### epr-packaging-common
##### Developers working for a DEFRA supplier
In order to restore and build the source code for this project, access to the `epr-packaging-common` package store will need to have been setup.
 - Login to Azure DevOps
 - Navigate to [Personal Access Tokens](https://dev.azure.com/defragovuk/_usersSettings/tokens)
 - Create a new token
   - Enable the `Packaging (Read)` scope
Add the following to your `src/Nuget.Config`
```xml
<packageSourceCredentials>
  <epr-packaging-common>
    <add key="Username" value="<email address>" />
    <add key="ClearTextPassword" value="<personal access token>" />
  </epr-packaging-common>
</packageSourceCredentials>
```
##### Members of the public
Clone the [epr_common](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr_common) repository and add it as a project to the solution you wish to use it in. By default the repository will reference the files as if they are coming from the NuGet package. You simply need to update the references to make them point to the newly added project.

### Run

- Complete `appsettings.json` file (or `appsettings.Development.json` if exists) with correct details
- On `EPR.SubmissionMicroservice.API` directory, execute:
```
dotnet run
```

### Docker

[Generate Personal Access Token](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows#create-a-pat). Then run in terminal at the solution src root (`epr_pom_api_submission_status/src`)
```
docker build -t submissionmicroservice -f EPR.SubmissionMicroservice.API/Dockerfile --build-arg PAT={YOUR PAT HERE} .
```

Then after that command has completed run
```
docker run -p 5167:3000 --name submissionmicroservicecontainer submissionmicroservice
```

Do a GET Request to `http://localhost:5167/admin/health` to confirm that the service is running

## How To Test

### Unit tests

On root directory, execute
```
dotnet test
```

### Pact tests
N/A

### Integration tests
N/A

## How To Debug
Use debugging tools in your chosen IDE

## Environment Variables - deployed environments
The structure of the `appsettings.json` file can be found in the repository.
Example configurations for the different environments can be found in
[epr-app-config-settings](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr-app-config-settings).

| Variable Name                         | Description                                                |
|---------------------------------------|------------------------------------------------------------|
| Database__AccountKey                  | The database account key                                   |
| Database__ConnectionString            | The database connection string                             |
| Database__Name                        | The database name                                          |
| Database__MaxRetryCount               | The database maximum number of retry attempts              |
| Database__MaxRetryDelayInMilliseconds | The database maximum delay between retries in milliseconds |
| LoggingApi__BaseUrl                   | The base URL for the Logging API WebApp                    |


## Additional Information
[ADR-012: PoM Data Upload](https://eaflood.atlassian.net/wiki/spaces/MWR/pages/4251418625/ADR-012.A+EPR+Phase+1+-+Compliance+Scheme+PoM+Data+Upload#Submission-Status-API.1)

[ADR-021: Registration Data Upload](https://eaflood.atlassian.net/wiki/spaces/MWR/pages/4291657945/ADR-021+Registration+Data+Upload#Submission-Status-API)

### Logging into Azure
N/A

### Usage
N/A

### Monitoring and Health Check
Health check - `{environment}/admin/health`

## Directory Structure
### Source files
- `src/EPR.SubmissionMicroservice.API` - API .NET source files
- `src/EPR.SubmissionMicroservice.API.IntegrationTests` - API .NET integration test files
- `src/EPR.SubmissionMicroservice.API.UnitTests` - API .NET unit test files
- `src/EPR.SubmissionMicroservice.Application` - Application .NET source files
- `src/EPR.SubmissionMicroservice.Application.UnitTests` - Application .NET unit test files
- `src/EPR.SubmissionMicroservice.Data` - Data .NET source files
- `src/EPR.SubmissionMicroservice.Data.UnitTests` - Data .NET unit test files
- `src/TestSupport` - .NET test support files

## Contributing to this project

Please read the [contribution guidelines](CONTRIBUTING.md) before submitting a pull request.

## Licence

[Licence information](LICENCE.md).