# Início
Cada solução arquitetural deve ser construída baseada nas necessidades técnicas e/ou de negócio.
O objetivo dessa biblioteca é garantir agilidade na construção de produtos digitais através de estruturas, mecanismos e ferramentas que, combinados corretamente, oferecem robustez, segurança, desempenho, monitoramento, observabilidade, resiliência e consistência.
Abaixo estão as principais referências para API RESTful para persistência e integração de serviços.

## Banco de dados relacional
É um banco de dados que permite criar relacionamentos entre si com o objetivo de garantir consistência e integridade dos dados.

* [SQL Server](pt-br/database/relational?id=sql-server)
* [PostgreSql](pt-br/database/relational?id=postgresql)
* [MySql](pt-br/database/relational?id=mysql)

## Banco de dados NoSql
NoSQL é um termo genérico que representa um banco de dados não relacional.

### Orientado a documentos
> É um tipo de banco de dados não relacional projetado para armazenar e consultar dados como documentos do tipo JSON. [O que é um banco de dados de documentos](https://aws.amazon.com/pt/nosql/document/)

[MongoDb](pt-br/database/nosql?id=mongodb)

### Orientado a chave-valor
É uma estrutura de dados do tipo mapa ou dicionário, onde utilizamos uma chave como identificador do registro.

[Redis](pt-br/database/nosql?id=redis)

## Message Broker
Um message broker é um software que possibilita que aplicativos, sistemas e serviços se comuniquem e troquem informações.

[RabbitMQ](pt-br/broker.md)

## Pipeline
É um padrão de projeto que representa um tubo com diversas operações (filtros), executadas de forma sequencial, com o intuito de trafegar, integir e/ou manuear um pacote/mensagem.

[Pipeline](pt-br/pipeline.md)

## Documentação
O hábito de documentar interfaces e classes de dados (value objects, dtos, entidades, ...) pode contruibuir para facilitar a manutenção de código. O Swagger permite você documentar facilmente sua API RESTful compartilhando com outros desenvolvedores a forma como poderão consumir os recursos disponíveis.

[Swagger](pt-br/swagger.md)

## Mapeamento
Com a prática de desenvolvimento de API RESTful com foco em mobile, temos como referência oferecer o mínimo de dados possível ou necessários em cada recurso da API. Sendo assim, surge a necessidade de criarmos objetos específicos para tráfeto (DTOs).
O AutoMapper auxilia na atribuição de valores de propriedades de objetos relacionados (Pessoa => PessoaDto), ou melhor, mapeamento de um objeto para outro.

[AutoMapper](pt-br/automapper.md)

## Padrões
Os padrões são modelos que usamos como referência ou base para resolução de um problema. Além dos padrões que apresentaremos nas definições arquiteturais exploradas por esta biblioteca, exploramos as seguintes referências:

* [Unidade de Trabalho](pt-br/database/use-unitofwork.md)
* [Repositório](pt-br/database/use-repository.md)
* [Serviço de Repositório](pt-br/database/use-service.md): Usamos para aplicar regras de negócio e encapsular o repositório
* [Validação de dados](pt-br/validation.md): Validação de dados usando fluent ou anotações
* [Notificação](pt-br/notification.md): Troca de mensagens em um contexto de notificações
* [Especificação](pt-br/specification.md): Filtro de dados