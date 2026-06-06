FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/PrintBridge.Domain/PrintBridge.Domain.csproj", "PrintBridge.Domain/"]
COPY ["src/PrintBridge.Application/PrintBridge.Application.csproj", "PrintBridge.Application/"]
COPY ["src/PrintBridge.Infrastructure/PrintBridge.Infrastructure.csproj", "PrintBridge.Infrastructure/"]
COPY ["src/PrintBridge.WebApi/PrintBridge.WebApi.csproj", "PrintBridge.WebApi/"]

RUN dotnet restore "PrintBridge.WebApi/PrintBridge.WebApi.csproj"

COPY src/ .
RUN dotnet publish "PrintBridge.WebApi/PrintBridge.WebApi.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p logs
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5160
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5160

ENTRYPOINT ["dotnet", "PrintBridge.WebApi.dll"]
