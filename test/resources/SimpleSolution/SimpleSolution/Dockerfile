FROM mcr.microsoft.com:443/dotnet/aspnet AS base
WORKDIR /app/

EXPOSE 80/tcp
EXPOSE 6060/udp
FROM mcr.microsoft.com:443/dotnet/sdk AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "SimpleSolution/SimpleSolution.csproj"
COPY . .
WORKDIR "/src/SimpleSolution/"
RUN dotnet build "SimpleSolution.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimpleSolution.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app/
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SimpleSolution.dll"]
