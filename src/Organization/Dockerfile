FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./

COPY src/SharedKernel/ src/SharedKernel/
COPY src/Organization/ src/Organization/

RUN dotnet publish src/Organization/Api/Api.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .
# org-private.pem 不是 MSBuild 的 Content item,dotnet publish 不會自動帶出去,
# 所以要另外複製到 image 裡,RSA_PEM_PATH 再指到這個路徑。
COPY src/Organization/Api/org-private.pem /app/keys/org-private.pem

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
