FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG TARGETARCH
WORKDIR /app

COPY DIME.csproj ./
RUN dotnet restore -a $TARGETARCH

COPY . ./
RUN dotnet publish -a $TARGETARCH --no-restore -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080
EXPOSE 8081
EXPOSE 8082
EXPOSE 9998
EXPOSE 9999
ENTRYPOINT ["dotnet", "DIME.dll"]