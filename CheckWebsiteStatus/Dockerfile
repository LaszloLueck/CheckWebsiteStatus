FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
LABEL MAINTAINER="Laszlo Lueck <llueck@gmail.com>"
WORKDIR /app
COPY /CheckWebsiteStatus.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "CheckWebsiteStatus.dll"]