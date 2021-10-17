using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Tokens
{
  public class TokenSinkIpcTests
  {
    [Test]
    public void it_should_should_communicate_via_ipc()
    {
      const string pipeName = "test-ipc-channel";

      using var host   = new TokenSinkHost(new ConcurrentTokenSink(), pipeName);
      using var client = new TokenSinkClient(pipeName);

      client.Sink.SetTokenStatus("task1", TokenStatus.Success);
      client.Sink.SetTokenStatus("task2", TokenStatus.Failure);

      Assert.AreEqual(TokenStatus.Success, host.Sink.GetTokenStatus("task1"));
      Assert.AreEqual(TokenStatus.Failure, host.Sink.GetTokenStatus("task2"));
      Assert.AreEqual(TokenStatus.Waiting, host.Sink.GetTokenStatus("task3"));
    }
  }
}