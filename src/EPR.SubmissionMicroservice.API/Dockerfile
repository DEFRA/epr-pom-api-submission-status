FROM defradigital/dotnetcore-development:dotnet6.0 AS build-env
USER root
# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

# Copy everything
COPY EPR.SubmissionMicroservice.API/. ./EPR.SubmissionMicroservice.API/.
COPY EPR.SubmissionMicroservice.Application/. ./EPR.SubmissionMicroservice.Application/.
COPY EPR.SubmissionMicroservice.Data/. ./EPR.SubmissionMicroservice.Data/.
COPY stylecop.ruleset ./
COPY NuGet.Config ./
COPY Directory.Build.props ./

# Use Build Argument to insert PAT into Nuget file
ARG PAT=localhost
RUN sed -i "s|</configuration>|<packageSourceCredentials><epr-packaging-common><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></epr-packaging-common></packageSourceCredentials></configuration>|" NuGet.Config

# Restore as distinct layers
RUN dotnet restore "EPR.SubmissionMicroservice.API/EPR.SubmissionMicroservice.API.csproj" --configfile "NuGet.Config"

# Build and publish a release
WORKDIR /home/dotnet/EPR.SubmissionMicroservice.API
RUN dotnet publish -c Release -o out

# Build runtime image
FROM defradigital/dotnetcore:dotnet6.0

# Switch to the non-root user
USER dotnet

COPY --from=build-env --chown=dotnet /home/dotnet/EPR.SubmissionMicroservice.API/out .

# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

CMD dotnet EPR.SubmissionMicroservice.API.dll