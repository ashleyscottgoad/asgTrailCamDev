#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ClassificationWorker/ClassificationWorker.csproj", "ClassificationWorker/"]
COPY ["ClassificationTrainer/ClassificationTrainer.csproj", "ClassificationTrainer/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "ClassificationWorker/ClassificationWorker.csproj"
COPY . .
WORKDIR "/src/ClassificationWorker"
RUN dotnet build "ClassificationWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClassificationWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClassificationWorker.dll"]