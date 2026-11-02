# Runtime only: CI publishes the host into publish/ first (see .github/workflows/release.yml).
#   dotnet publish PlotboekGenerator.Host/PlotboekGenerator.Host.csproj -c Release -o publish -p:PublishReadyToRun=false
#   docker build -t plotboek-generator .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "PlotboekGenerator.Host.dll"]
