# Distance.Api
This ejercicio was requested by CTeleport

>REST service to measure distance in miles between two airports. Airports are identified by 3-letter IATA code.

By design (in sake of demo purposes) this code produces executable with self-hosted API

**Thus to get distance between two airports** 
1. Build
2. Run exe
3. Make HTTP GET call to API 

For example to measure distance between AMS and VLC airports it should be:
```
GET http://localhost:62803/between/AMS&VLC HTTP/1.1
```

## Tags
`.net core` `c#` `web api` `external api` `exe` `self-hosted` `xunit`

## Go Ahead
...