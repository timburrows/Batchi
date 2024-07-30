
## Overview

This library is a lightweight implementation of a processing pipeline for micro-batching tasks 
that are otherwise expensive to run individually. The library exposes a simple API that should feel familiar 
if you've worked with other event streaming APIs.

Interaction with the library is fairly straightforward, you will need to
1. Configure your Job with a size and the frequency of batches.
2. Publish a message to a Channel<JobContext<TMessage>> which the service reads from to begin processing.
3. Implement IJobExecutor<TMessage> to be invoked by the batch service.

By running requests through the .NET Channel API, integration with a message queue like 
Azure ServiceBus and GCP Pub/Sub makes perfect sense, and will allow an application to scale horizontally with ease.
It also means multiple types of jobs can be processed in parallel, as each IHostedService is isolated 
in its own thread. It's also possible to multi-thread the execution of batches (although not yet supported).

## Pre-requisites
- Install the latest version of Postgres
- Run the migrations for the project

`$ dotnet ef database update --startup-project ./Batchi.Example/Batchi.Example.csproj --project ./Batchi.Migrations/Batchi.Migrations.csproj`

- Run the Batchi.Example project

`$ dotnet run --project ./Batchi.Example`

