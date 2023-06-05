## Apache Kafka Notes



- We currently only support JSON messages. Support for Avro and Protobuf is planned.
- We check the Kafka messages for the presence of the Confluent Schema Registry framing bytes and discard them.

### Confluent Schema Registry Framing

Confluent Schema Registry framing is a protocol used by the Confluent Schema Registry to encapsulate Avro-encoded messages for schema evolution and compatibility. It provides a way to serialize and deserialize Avro-encoded data with schema information, enabling consumers and producers to handle data in a consistent and compatible manner.

When messages are produced to Kafka using the Confluent Schema Registry, the message payload includes additional framing information along with the Avro-encoded data. This framing information consists of a 5-byte header followed by the Avro-encoded payload.

The 5-byte header contains the following information:

1. Magic Byte (1 byte): A value indicating the version of the framing protocol. The current version is 0.

2. Schema ID (4 bytes): A unique identifier that references the Avro schema used for encoding the message. This ID corresponds to the schema registered in the Schema Registry.

By including the schema ID in the message framing, the consumer can retrieve the corresponding schema from the Schema Registry and decode the Avro-encoded message using the correct schema. This allows for schema evolution, where different versions of the schema can be used by producers and consumers while ensuring compatibility and backward compatibility.

The Confluent Schema Registry framing provides a way to decouple the schema from the message payload, enabling schema evolution and centralized schema management. It allows for seamless integration of Avro-encoded messages with the Schema Registry, ensuring consistent data serialization and deserialization across different systems and versions.


Notes: 

- The Confluent Schema Registry framing is not part of the Avro specification. It is a protocol used by the Confluent Schema Registry to encapsulate Avro-encoded messages for schema evolution and compatibility.
- Some companies also use schema framing with JSON messages. 
