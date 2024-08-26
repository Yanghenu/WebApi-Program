运行条件：
1、Redis 7.0(需要有Redis Stream)，用于实现CAP的消息队列，使用其他消息队列也可以(例如RabbitMQ);
2、AgileConfig可有可没有，没有的话则使用本地配置文件;
3、数据库使用PostgreSQL;
