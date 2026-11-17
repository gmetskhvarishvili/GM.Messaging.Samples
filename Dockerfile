# One image recipe for the sample's four hosts; PROJECT picks which one (see docker-compose.yml):
#   GM.Messaging.Sample.API, GM.Messaging.Sample.Producer.Worker,
#   GM.Messaging.Sample.ConsumerA.Worker or GM.Messaging.Sample.ConsumerB.Worker
ARG PROJECT

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY . .
# With its native launcher (apphost), so the container starts the app directly, not through a shell.
RUN dotnet publish "$PROJECT/$PROJECT.csproj" -c Release -o /app/publish -p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT
# Npgsql looks for the Kerberos (GSSAPI) library when it connects; without it every start logs an error.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# A fixed name for whichever host this image runs, started directly (a shell would drop environment
# variables whose names aren't shell identifiers, such as ConnectionStrings__ApplicationDatabase).
RUN ln -s "/app/$PROJECT" /app/entrypoint
# The claim-check directory (large event bodies), owned by the app user so a new named volume mounted here is writable.
RUN mkdir -p /claim-checks && chown $APP_UID /claim-checks
USER $APP_UID
# Every host listens on 8080 in the container (the workers for their health and admin endpoints).
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["/app/entrypoint"]
