# Kafka Tools  

Kafka Tools is a simple, lightweight application for inspecting Kafka topics and messages, designed primarily for local development. While the project hasn't been updated recently, it remains fully functional for basic Kafka message viewing and troubleshooting.  

## Features  

- Connect to a Kafka environment and list available topics  
- View messages in selected topics  
- Display message keys, offsets, and timestamps  
- Currently supports **JSON** messages only  

## Apache Kafka Notes  

- **Supported Formats**: At present, **only JSON messages** are supported.  
- **Avro & Protobuf**: Support for Avro and Protobuf is planned but not yet implemented.  
- **Schema Registry Handling**: Kafka Tools checks for the **Confluent Schema Registry framing bytes** in messages and discards them.  

## Usage  

1. Select your **Kafka environment** (local, development, etc.).  
2. Choose a **topic** from the list.  
3. View messages, including key, offset, and timestamp.  

## Known Limitations  

- The UI is minimalistic and intended for basic local development needs.  
- Advanced message filtering and search capabilities are not available yet.  
- No built-in support for Avro/Protobuf message deserialization.  

## License  

MIT

---
