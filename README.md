# MerxPOS

Distributed Smart POS & Settlement System

## Architecture

Client
  |
API Gateway
  |
----------------------------------------------
|            |             |                |
Auth       POS         Settlement        Reporting
             |              |
             |          RabbitMQ
             |
        Outbox Table

## Goals

- Practice microservice architecture
- Implement Outbox Pattern
- Implement async communication
- Separate reporting & OLTP database
- Practice failure handling & retry strategy