#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM local_dotnet_aspnet AS base

WORKDIR /app

RUN apt-get update
RUN apt-get install -y curl
RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs

FROM local_dotnet_sdk AS build
RUN apt-get update
RUN apt-get install -y curl
RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["dc_app.client/nuget.config", "dc_app.client/"]
COPY ["dc_app.Server/dc_app.Server.csproj", "dc_app.Server/"]
COPY ["dc_app.client/dc_app.client.esproj", "dc_app.client/"]
COPY ["dc_app.ServiceLibrary/dc_app.ServiceLibrary.csproj", "dc_app.ServiceLibrary/"]
RUN dotnet restore "./dc_app.Server/./dc_app.Server.csproj"
COPY . .
WORKDIR "/src/dc_app.Server"
RUN dotnet build "./dc_app.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./dc_app.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dc_app.Server.dll"]