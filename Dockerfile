FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/KurApi/KurApi.csproj", "src/KurApi/"]
RUN dotnet restore "src/KurApi/KurApi.csproj"
COPY . .
WORKDIR "/src/src/KurApi"
RUN dotnet build "KurApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KurApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KurApi.dll"] 