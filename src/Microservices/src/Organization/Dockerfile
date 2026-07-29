FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./

COPY src/SharedKernel/ src/SharedKernel/
COPY src/Organization/ src/Organization/

RUN dotnet publish src/Organization/Api/Api.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

COPY src/Organization/Api/org-private.pem /app/keys/org-private.pem

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
