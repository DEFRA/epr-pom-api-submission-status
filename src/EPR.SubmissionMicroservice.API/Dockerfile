FROM defradigital/dotnetcore-development:dotnet8.0 AS build-env

# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

# Copy everything
COPY --chown=dotnet EPR.SubmissionMicroservice.API/. ./EPR.SubmissionMicroservice.API/.
COPY --chown=dotnet EPR.SubmissionMicroservice.Application/. ./EPR.SubmissionMicroservice.Application/.
COPY --chown=dotnet EPR.SubmissionMicroservice.Data/. ./EPR.SubmissionMicroservice.Data/.
COPY --chown=dotnet stylecop.ruleset ./
COPY --chown=dotnet NuGet.Config ./
COPY --chown=dotnet Directory.Build.props ./

# Use Build Argument to insert PAT into Nuget file
ARG PAT=localhost
RUN sed -i "s|</configuration>|<packageSourceCredentials><epr-packaging-common><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></epr-packaging-common></packageSourceCredentials></configuration>|" NuGet.Config

# Restore as distinct layers
RUN dotnet restore "EPR.SubmissionMicroservice.API/EPR.SubmissionMicroservice.API.csproj" --configfile "NuGet.Config"

# Build and publish a release
WORKDIR /home/dotnet/EPR.SubmissionMicroservice.API
RUN dotnet publish -c Release -o out

# Build runtime image
FROM defradigital/dotnetcore:dotnet8.0

# Switch to the non-root user
USER dotnet

COPY --from=build-env --chown=dotnet /home/dotnet/EPR.SubmissionMicroservice.API/out .

# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

CMD dotnet EPR.SubmissionMicroservice.API.dll