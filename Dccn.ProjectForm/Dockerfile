FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY . .
WORKDIR /src/Dccn.ProjectForm
RUN dotnet publish Dccn.ProjectForm.csproj -c Release -o /app

FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Dccn.ProjectForm.dll"]