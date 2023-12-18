#!/usr/bin/env bash

dotnet test src/EPR.SubmissionMicroservice.API.Tests/EPR.SubmissionMicroservice.API.Tests.csproj --logger "trx;logfilename=testResults.Api.trx"
dotnet test src/EPR.SubmissionMicroservice.Application.Tests/EPR.SubmissionMicroservice.Application.Tests.csproj --logger "trx;logfilename=testResults.Application.trx"