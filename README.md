# NebulaBus - 高性能的 .NET 分布式事件总线框架，让开发者专注开发

### **🏠 Repo**

![GitHub Repo stars](https://img.shields.io/github/stars/JiewitTech/NebulaBus?logoSize=30&link=https%3A%2F%2Fgithub.com%2FJiewitTech%2FNebulaBus)
[![star](https://gitee.com/jiewit-tech/NebulaBus/badge/star.svg?theme=dark)](https://gitee.com/jiewit-tech/NebulaBus/stargazers)

### **🔑 Release**

| Nuget Package | Description |
| ---- | ---- |
| ![NuGet Version](https://img.shields.io/nuget/v/NebulaBus?style=plastic&label=NebulaBus&color=blue&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FNebulaBus%2F) | NebulaBus's Core |
| ![NuGet Version](https://img.shields.io/nuget/v/NebulaBus.Store.Memory?style=plastic&label=NebulaBus.Store.Memory&color=blue&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FNebulaBus.Store.Memory%2F) | NebulaBus Memory Store Provider |
| ![NuGet Version](https://img.shields.io/nuget/v/NebulaBus.Store.Redis?style=plastic&label=NebulaBus.Store.Redis&color=blue&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FNebulaBus.Store.Redis%2F) | NebulaBus Redis Store Provider |
| ![NuGet Version](https://img.shields.io/nuget/v/NebulaBus.Store.Sql?style=plastic&label=NebulaBus.Store.Sql&color=blue&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FNebulaBus.Store.Sql%2F) | NebulaBus Sql Store Provider |
| ![NuGet Version](https://img.shields.io/nuget/v/NebulaBus.Transport.Memory?style=plastic&label=NebulaBus.Transport.Memory&color=blue&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FNebulaBus.Transport.Memory%2F) | NebulaBus Memory Transport Provider |
| ![NuGet Version](https://img.shields.io/nuget/v/NebulaBus.Transport.Rabbitmq?style=plastic&label=NebulaBus.Transport.Rabbitmq&color=blue&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FNebulaBus.Transport.Rabbitmq%2F) | NebulaBus Rabbitmq Transport Provider |


### **📔 文档完善**
- [中文文档](https://nebulabus.jiewit.com/zh/)
- [English Document](https://nebulabus.jiewit.com/)

###  **✨ 全场景消息驱动** 

支持即时/延迟的广播、定向推送（如微服务间精准通信），内置Quartz.Net和失败重试机制，完美适配电商秒杀、物流追踪等高并发场景。

###  **⚡ 弹性架构设计** 

- 双引擎驱动：基于 RabbitMQ 的毫秒级传输 + Redis 的高性能存储，动态负载均衡
- 精确延迟发送：内置Quartz.net 进行精确延迟发送并支持多节点部署
- 分布式部署：自动节点发现、故障转移，支持横向扩展
- 轻量级内核：核心包仅 50KB

###  **🔧 开发者友好特性** 
配置简单，快速上手，让开发者专注开发

安装 
https://www.nuget.org/packages/NebulaBus/ 
```
dotnet add package NebulaBus.Store.Redis
dotnet add package NebulaBus.Transport.Rabbitmq
```

注入
```
//配置
builder.Services.AddNebulaBus(options =>
{
    options.ClusterName = "TestCluster";
    options.UseRedisStore("localhost:6379,password=****,defaultDatabase=0,prefix=prefix_");
    options.UseRabbitmqTransport(rabbitmq =>
    {
        rabbitmq.HostName = “localhost”;
        rabbitmq.UserName = “guest”;
        rabbitmq.Password = “guest”;
        rabbitmq.VirtualHost = "/";
    });
});
//注入订阅者
builder.Services.AddNebulaBusHandler<TestHandlerV1, TestMessage>();
builder.Services.AddNebulaBusHandler<TestHandlerV2, TestMessage>();
//批量注入订阅者
builder.Services.AddNebulaBusHandler(typeof(TestHandlerV1).Assembly);
```
订阅

```
//实现NebulaHandler<> 抽象类即可
 public class TestHandlerV1 : NebulaHandler<TestMessage>
    {
        //订阅者唯一标识，用于定向发送
        public override string Name => "NebulaBus.TestHandler.V1";
        //订阅者组，用于广播，相同组的订阅都将收到消息
        public override string Group => "NebulaBus.TestHandler";
        //重试延迟，用于配置首次失败后多久重试，若不重写则默认10秒
        public override TimeSpan RetryDelay => TimeSpan.FromSeconds(10);
        //最大重试次数，若不重写则默认10次
        public override int MaxRetryCount => 5;
        //重试间隔，若不重写则默认10秒
        public override TimeSpan RetryInterval => TimeSpan.FromSeconds(10);

        protected override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} Received Message {Name}:{message.Message} Header:{header["customHeader"]} RetryCount:{header[NebulaHeader.RetryCount]}");
            //TODO: your code
        }
    }
```
发布

```
//注入INebulaBus接口
private readonly INebulaBus _bus;
//广播，广播传入的是订阅者Group，所有相同Group的订阅者都将收到消息
_bus.PublishAsync("NebulaBus.TestHandler", new TestMessage { Message = "Hello World" });

//延迟广播
_bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler", new TestMessage { Message = "Hello World" });

//定向发送，定向发送传入的是订阅者Name，只有该Name的订阅者会收到消息
_bus.PublishAsync("NebulaBus.TestHandler.V1", new TestMessage { Message = "Hello World" });

//延迟定向发送，传入延迟的TimeSpan即可
_bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler.V1",new TestMessage { Message = "Hello World" });

//自定义消息请求头，不管是发送还是广播都支持自定义请求头
_bus.PublishAsync("NebulaBus.TestHandler", new TestMessage { Message = "Hello World" },
    new Dictionary<string, string>()
    {
        { "customHeader", "123456" },
        { NebulaHeader.RequestId, "8889999" },
    });
```
示例请参考：src/WebApplicationSample

###  **🌐 适用场景** 

微服务解耦 | IoT 设备指令分发 | 金融交易异步结算 | 游戏服务器状态同步

项目定位: 比 MassTransit 更易扩展和轻量化的 .NET 消息中间件解决方案。

