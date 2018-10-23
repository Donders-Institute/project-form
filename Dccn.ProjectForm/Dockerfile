FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Dccn.ProjectForm/Dccn.ProjectForm.csproj", "Dccn.ProjectForm/"]
RUN dotnet restore "Dccn.ProjectForm/Dccn.ProjectForm.csproj"
COPY . .
WORKDIR "/src/Dccn.ProjectForm"
RUN dotnet build "Dccn.ProjectForm.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Dccn.ProjectForm.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Dccn.ProjectForm.dll"]