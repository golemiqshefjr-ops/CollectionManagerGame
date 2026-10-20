# Колекционерски свят

ASP.NET Core 8 app with a static frontend and SQL Server backend.

## Render deployment

This repository includes a Dockerfile and render.yaml. Configure ConnectionStrings__CollectionDb in Render with a reachable SQL Server connection string. Run the SQL scripts in CollectionManager.Api/SQL in numerical order to initialize the database.

Do not commit local appsettings.json, uploaded images, or secrets.
