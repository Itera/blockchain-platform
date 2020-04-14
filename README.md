# blockchain-platform
This repository is a work in progress, and has a goal to create a Blockchain platform based on Azure technologies.

Currently this repository consists of one project, “mining node server”. This project is a .NET Core Web API that runs a Blockchain Mining Server. 

To run the server on your local machine, use the docker compose file. This file starts a container for running the .NET Core application and a container for running MongoDB. You need to change the following values in the docker compose before you start.

**BlockchainNodeSettings:NodeName**, the unique name for your Node/Server.

**ConnectionStrings:AzureServiceBus**, the connection string to the Azure Service Bus namespace that connects all mining nodes together. 

To run the mining node, use this command.

_docker-compose up --build_
