FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./

COPY src/SharedKernel/ src/SharedKernel/
COPY src/MessageContract/ src/MessageContract/
COPY src/Ordering/ src/Ordering/

RUN dotnet publish src/Ordering/Api/Api.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
