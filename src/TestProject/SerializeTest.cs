using MessageLibrary;
using NebulaBus;
using System.Text.Json;

namespace TestProject
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestSerializeTestMessage()
        {
            var message = new TestMessage() { Message = "test" };
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message);
            var deserializedData = JsonSerializer.Deserialize<TestMessage>(bytes);
            Assert.That(deserializedData!.Message, Is.EqualTo(message.Message));
        }

        [Test]
        public void TestSerializeNebulaHeader()
        {
            var message = new NebulaHeader()
            {
                { NebulaHeader.RequestId, Guid.NewGuid().ToString() }
            };
            var jsonText = JsonSerializer.Serialize(message);
            var deserializedData = JsonSerializer.Deserialize<NebulaHeader>(jsonText);
            Assert.That(message.GetRequestId(), Is.EqualTo(deserializedData!.GetRequestId()));
        }
    }
}