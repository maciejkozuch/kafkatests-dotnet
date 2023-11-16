# TasksConsumerWithKTableConsoleApp

The project to test how operate with KTable with [Streamiz.Kafka.Net](https://lgouellec.github.io/kafka-streams-dotnet/index.html).

The test is based on [How to convert a Kafka Streams KStream to a KTable](https://developer.confluent.io/tutorials/kafka-streams-convert-to-ktable/confluent.html).

---
**Disclaimer:** 
When running on a local installation of Kafka (based on [kafka-standalone-compose.yaml](../Docker/kafka-standalone-compose.yaml)) 
the outgoing topic created from Ktable has all events send to incoming topic. The same situation occurs when the 
Katble stream was created by Java API - [CreateKTableStreamApp](https://github.com/maciejkozuch/kafkatests-java/blob/main/src/main/java/kafkatests/CreateKTableStreamApp.java). In the
[How to convert a Kafka Streams KStream to a KTable](https://developer.confluent.io/tutorials/kafka-streams-convert-to-ktable/confluent.html) 
article the KTable outgoing has only the latest events for individual keys.  
