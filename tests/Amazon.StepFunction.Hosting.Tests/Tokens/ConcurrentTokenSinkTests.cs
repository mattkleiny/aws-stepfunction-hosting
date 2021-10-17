using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Tokens
{
  public class ConcurrentTokenSinkTests
  {
    [Test]
    public void it_should_return_default_status_if_token_not_set()
    {
      var sink = new ConcurrentTokenSink();

      Assert.AreEqual(TokenStatus.Waiting, sink.GetTokenStatus("test1"));
      Assert.AreEqual(TokenStatus.Failure, sink.GetTokenStatus("test1", TokenStatus.Failure));
      Assert.AreEqual(TokenStatus.Success, sink.GetTokenStatus("test1", TokenStatus.Success));
    }
  }
}