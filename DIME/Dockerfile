FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["dime.csproj", "dime/"]
RUN dotnet restore "dime.csproj"
COPY . .
WORKDIR "/src/dime"
RUN dotnet build "dime.csproj" \
	-c Release \
	-o /app/build
FROM build AS publish
RUN dotnet publish "dime.csproj" \
	-c Release \
	-o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/Configs
RUN mkdir -p /app/Lua
COPY Configs/* /app/Configs
COPY Lua/* /app/Lua
EXPOSE 8080
EXPOSE 5000
ENTRYPOINT ["dotnet", "dime.dll"]