# Architecture v1

## Services

### Auth Service
- JWT authentication

### POS Service
- Create transaction
- Insert outbox event

### Settlement Service
- Consume event
- Aggregate settlement

### Reporting Service
- Read settlement data

## Communication

- HTTP via API Gateway
- Async via RabbitMQ

## Data Ownership

- auth_db
- pos_db (Transactions + Outbox)
- settlement_db
- reporting_db