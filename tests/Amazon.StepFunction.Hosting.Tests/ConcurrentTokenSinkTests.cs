using NUnit.Framework;

namespace Amazon.StepFunction.Hosting
{
  public class ConcurrentTokenSinkTests
  {
    [Test]
    public void it_should_return_default_status_if_token_not_set()
    {
      var sink = new ConcurrentTaskTokenSink();

      Assert.AreEqual(TaskTokenStatus.Waiting, sink.GetTokenStatus("test1"));
      Assert.AreEqual(TaskTokenStatus.Failure, sink.GetTokenStatus("test1", TaskTokenStatus.Failure));
      Assert.AreEqual(TaskTokenStatus.Success, sink.GetTokenStatus("test1", TaskTokenStatus.Success));
    }
  }
}