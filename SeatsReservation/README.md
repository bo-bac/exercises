# Movies challenge
This ejercicio is tech challenge for Backend developers candidates.

---

## Context

We want a C# .Net Core Web Application API project that meets folowing requirements. 

1. The application represents a Cinema. We want to manage the showtimes of the cinema, getting some data from the external **[Provided API](http://localhost:7172/swagger/index.html).**

2. The test includes a docker-compose with Redis and the provided Api, you will need Docker to be able run them.

3. We only want the following features:
	- Create showtimes.
	- Reserve seats.
	- Buy seats.

4. Note that we also expect you to provide test coverage for the implemented features.

5. The Data layer should be implemented as an In-Memory Database. 

---

## Implementation instructions

### Commands and queries

- **Create showtime**
    
    Should create showtime and should grab the movie data from the ProvidedApi.
    
- **Reserve seats**
    - Reserving the seat response will contain a GUID of the reservation, also the number of seats, the auditorium used and the movie that will be played.
    - After 10 minutes after the reservation is created, the reservation is considered expired by the system.
    - It should not be possible to reserve the same seats two times in 10 minutes.
    - It shouldn't be possible to reserve an already sold seat.
    - All the seats, when doing a reservation, need to be contiguous.
- **Buy seats**
    - We will need the GUID of the reservation, it is only possible to do it while the seats are reserved.
    - It is not possible to buy the same seat two times.
    - Expired reservations (older than 10 minutes) cannot be confirmed.
    - We are not going to use a Payment abstraction for this case, just have an Endpoint which I can use to Confirm a Reservation.

### Starting the API

- You will need Docker in order to use this API and then run the next command:

```powershell
docker-compose up
```

- By default, the provided API will run on [*http://localhost:7172/swagger/index.html*](http://localhost:7172/swagger/index.html) , [https://localhost:7443/swagger/index.html](https://localhost:7443/swagger/index.html)
- For GRPC use the **HTTPS** port
- And Redis in the default port.
- When you end the test

```powershell
docker-compose down
```
    
### API communication with ProvidedApi

You can use the HTTP API or the GRPC API, you should check the [Swagger](http://localhost:7172/swagger/index.html) for more info. We know that the GRPC implementation is faster. 

The solution includes the proto of Provided API and a small piece of code that tries to connect with the GRPC API, but it does not work. You can fix it and use it.

### Cache

We will like to have a cache layer to cache the response from the Provided API because the API is slow and fails a lot. We will like to call the API and in case of failure try to use the cached response. The cache should use the Redis container provided in the docker-compose.yaml

### Execution Tracking

We want to track the execution time of each request done to the service and log the time in the Console.
By default, we set the loggers to log in to the Console, so you only need to worry where to put the Logger in the code.

### Provided API

We know that [**Provided API**](http://localhost:7172/swagger/index.html) may have some configuration issues, and we will like them to be found and fixed, if possible.

### Add the Request to cUrls file

We added a file next to this readme named `cUrls.txt`.
Please add a curl command for each of the commands and queries that you implemented to this file.
