# Producer-Consumer
This ejercicio from Emsisoft.

---

Create two applications that will generate and process the data using RabbitMQ

---

## Implementation instructions

### Technology stack

- C#
- .NET 6.0
- SQL Server Express LocalDB or SQL Server in Docker container
- RabbitMQ

### Application 1 - Producer (API)

- REST API application
- Endpoint POST '/hashes' will generate 40.000 random SHA1 hashes and send them one by one to RabbitMQ queue for further processing
- Endpoint GET '/hashes' will return a number of hashes from the database grouped by day in JSON

Example of GET response
```
{
	"hashes": [
		{
			"date": "2022-06-25",
			"count": 471255631
		},
		{
			"date": "2022-06-26",
			"count": 822365413
		},
		{
			"date": "2022-06-27",
			"count": 1973565411
		}
	]
}
```

### Application 2 - Consumer (Processor) 

- Background worker application.
- Connect to RabbitMQ queue and process messages in parallel using 4 threads.
- Save message into database table 'hashes' (id, date, sha1).

    
### Optional features to implement 

- Split generated hashes into batches and send them into RabbitMQ in parallel.
- Retrieve hashes from the database without recalculating data on the fly.

## How To Run It

1. You will need Docker in order to run our infrastructure
```
powershell
docker-compose up -d
```

When you end

```
powershell
docker-compose down
```


## Tags
`.net core` `c#` `asp.net core` `web api` `minimal api` `background worker` `rabbitmq` `producer-consumer` `batch producing` `parallel consuming` `ef` `model first`

## Go Ahead
!!!
Dont forget about DB connection string in the secrets
