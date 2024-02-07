#Command 
docker run --name sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@word" -p 1433:1433 -d mcr.microsoft.com/mssql/server

#ConnectionString
Data Source=.,1433;Initial Catalog=MyTestDb;Persist Security Info=True;User ID=sa;Password=MyPass@word;Pooling=False;

#Tools
https://dbeaver.io/download/