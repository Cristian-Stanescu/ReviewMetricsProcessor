# ReviewMetricsProcessor

### Overview
This project is a .NET 8.0 application that processes code review metrics. It is designed to analyze pull requests and generate reports based on various criteria such as the lines of code changed and the time taken for reviews.

### Setup Instructions
The project can be run through docker-compose which provisions a local postgres database, runs the Migrations project to apply DB migrations, and starts the application in a container. The event generator can be executed after the application is running.


### Assumptions and Design decisions
The project uses a modular architecture with separate projects for the application, data, and migrations. This allows for the migration project to be containerized and run independently of the application. Additional layers (such as Services/Application to contain the business logic separately from the Endpoints) can be added to the architecture as needed, but it seemed overkill to add an extra layer for this small application. This is also the reason for having a single test project that contains tests for both the application and the data layer.
The storage is done using Entity Framework Core with a PostgreSQL database since that is what I am most familiar with. 
To allow scaling of the ingestion of events, the application uses MassTransit with an in-memory queue to handle incoming events asynchronously. The initial implementation that was storing events as part of the processing of the HTTP request was not performant enough, so the events are now processed in the background by the respective consumers.
Due to the way that the generator workers call the reviews endpoint it is not guaranteed that review start and completion events are processed in the correct order. If the review hasn't started then the processing of the completed event will fail and go into the retry queue of MassTransit. Eventually the start event is processed and the retry will succeed.

### Possible Improvements
A document DB could simplify the storage of the metrics and the aggregation of the review events, since it would store JSON objects.
Performance could be improved by batching the processing of events, as currently each event is processed individually. 