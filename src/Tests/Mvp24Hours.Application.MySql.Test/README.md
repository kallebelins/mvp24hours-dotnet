#Command 
docker run --name mysql -v /mysql/data/:/var/lib/mysql -d -p 3306:3306 -e MYSQL_ROOT_PWD=MyPass@word -e MYSQL_USER=user -e MYSQL_USER_PWD=MyPass@word leafney/docker-alpine-mysql

#ConnectionString
server=localhost;user=root;password=MyPass@word;database=MyTestDb

#Tools
https://dbeaver.io/download/