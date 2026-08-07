FROM defradigital/dotnetcore-development:dotnet10.0 AS build-env
USER root

# Copy only the files restore needs, so the restore layer survives source changes
COPY EPR.SubmissionMicroservice.API/EPR.SubmissionMicroservice.API.csproj ./EPR.SubmissionMicroservice.API/
COPY EPR.SubmissionMicroservice.Application/EPR.SubmissionMicroservice.Application.csproj ./EPR.SubmissionMicroservice.Application/
COPY EPR.SubmissionMicroservice.Data/EPR.SubmissionMicroservice.Data.csproj ./EPR.SubmissionMicroservice.Data/
COPY stylecop.ruleset ./
COPY NuGet.Config ./
COPY Directory.Build.props ./

# Use Build Argument to insert PAT into Nuget file
ARG PAT=localhost
RUN sed -i "s|</configuration>|<packageSourceCredentials><epr-packaging-common><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></epr-packaging-common></packageSourceCredentials></configuration>|" NuGet.Config

# Restore as distinct layers
RUN dotnet restore "EPR.SubmissionMicroservice.API/EPR.SubmissionMicroservice.API.csproj" --configfile "NuGet.Config"

# Copy the remaining source
COPY EPR.SubmissionMicroservice.API/. ./EPR.SubmissionMicroservice.API/.
COPY EPR.SubmissionMicroservice.Application/. ./EPR.SubmissionMicroservice.Application/.
COPY EPR.SubmissionMicroservice.Data/. ./EPR.SubmissionMicroservice.Data/.

# Build and publish a release
WORKDIR /home/dotnet/EPR.SubmissionMicroservice.API
RUN dotnet publish -c Release --no-restore -o out

# Build runtime image
FROM defradigital/dotnetcore:dotnet10.0

# Switch to the non-root user
USER dotnet

ARG BUILD_NUMBER
ARG GIT_SHA
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV GIT_SHA=$GIT_SHA

COPY --from=build-env /home/dotnet/EPR.SubmissionMicroservice.API/out .

# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

CMD dotnet EPR.SubmissionMicroservice.API.dll