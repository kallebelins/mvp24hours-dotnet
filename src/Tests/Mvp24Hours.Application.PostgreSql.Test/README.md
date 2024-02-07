#Command 
docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=MyPass@word -d onjin/alpine-postgres

#ConnectionString
Host=localhost;Port=5432;Pooling=true;Database=MyTestDb;User Id=postgres;Password=MyPass@word;

#Tools
https://dbeaver.io/download/