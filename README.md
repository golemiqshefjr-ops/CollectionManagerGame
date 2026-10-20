# Колекционерски свят

ASP.NET Core 8 application with a static frontend and SQL Server backend.

## Render

This repository uses the included Dockerfile and `render.yaml`. Configure `ConnectionStrings__CollectionDb` in Render with a reachable SQL Server connection string. Set up the database using the SQL scripts in `CollectionManager.Api/SQL` in numerical order.

Do not commit local `appsettings.json`, uploaded images, or secrets.