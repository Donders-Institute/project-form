# project-form

## Installation instructions

Please following the instructions below to run the project-form application locally.

To obtain the source code, use the following commands:

```
$ git clone https://github.com/Donders-Institute/project-form.git project-form
$ cd project-form/
```

Copy `secrets.json` with the following contents in the main folder:

```
{
  "ConnectionStrings": {
    "ProjectsDatabase": "Server=INSERT_SERVER_NAME;Database=INSERT_DATABASE_NAME;Uid=INSERT_USERNAME;Pwd=INSERT_PASSWORD;ConvertZeroDatetime=True;TreatTinyAsBoolean=True"
  },
  "DondersRepositoryApi": {
    "UserName": "INSERT_USERNAME",
    "Password": "INSERT_PASSWORD"
  },
  "ExceptionReporter": {
    "Address": "INSERT_EMAIL_ADDRESS",
    "DisplayName": "INSERT_NAME"
  },
  "Form": {
    "Authorities": {
      "Funding": "INSERT_USERNAME",
      "Ethics": "INSERT_USERNAME",
      "LabMri": "INSERT_USERNAME",
      "LabOther": "INSERT_USERNAME",
      "Privacy": "INSERT_USERNAME",
      "Director": "INSERT_USERNAME",
      "Administration": "INSERT_USERNAME"
    }
  }
}
```

Add `docker-compose.override.yml` with the following contents in the main folder:

```
version: "3.7"

services:
  web:
    image: docker-registry.dccn.nl:5000/project-form
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__ProposalsDatabase: Server=db;Database=ProjectProposalForms;User=SA;Password=INSERT_PASSWORD
    volumes:
      - "./secrets.json:/root/.microsoft/usersecrets/INSERT_USER_SECRETS_ID/secrets.json:ro"
      # - "./INSERT_CERTIFICATE_NAME.crt:/etc/ssl/certs/INSERT_CERTIFICATE_NAME.crt:ro"
  db:
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: INSERT_PASSWORD
```

Start the database and web service:
```
$ docker-compose build
$ docker-compose up
```

Open a web browser:

```
http://localhost:8080
```
