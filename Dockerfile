# build the app using the microsoft .net core build container
FROM mcr.microsoft.com/dotnet/core/sdk:5.0 AS build
WORKDIR /subnet-server
COPY . .
RUN dotnet publish -r linux-musl-x64 --self-contained false

# run the app using the microsoft asp.net runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:5.0-alpine
EXPOSE 5656/tcp
EXPOSE 5657/tcp
WORKDIR /app
COPY --from=build /subnet-server/bin/Debug/netcoreapp5.0/publish ./
ENTRYPOINT ["dotnet", "SubnetServer.dll"]
