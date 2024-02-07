#Command 
1: docker run --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=123456 mongo
2: docker run -d --name mongo -p 27017:27017 mvertes/alpine-mongo

#ConnectionString
1: mongodb://user:123456@localhost:27017
2: mongodb://localhost:27017

#Tools
https://robomongo.org/

