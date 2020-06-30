# build the app using the microsoft .net core build container
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /subnet-server
COPY . .
RUN dotnet publish

# run the app using the microsoft asp.net runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "SubnetServer.dll"]