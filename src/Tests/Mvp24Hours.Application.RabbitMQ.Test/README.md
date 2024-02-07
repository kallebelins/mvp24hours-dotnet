#Command 
docker run -d --name my-rabbit -p 5672:5672 -p 5673:5673 -p 15672:15672 rabbitmq:3-management

#ConnectionString
amqp://guest:guest@localhost:5672

#Tools
http://localhost:15672/

