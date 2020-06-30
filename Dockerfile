# build the app using the microsoft .net core build container
FROM mcr.microsoft.com/dotnet/core/sdk:5.0 AS build
WORKDIR /subnet-server
COPY . .
RUN dotnet publish

# run the app using the microsoft asp.net runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:5.0
WORKDIR /app
COPY --from=build /subnet-server/bin/Debug/netcoreapp5.0 ./
ENTRYPOINT ["dotnet", "SubnetServer.dll"]
