using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  public class ChoiceStepTests
  {
    [Test]
    public void it_should_follow_default_path_if_no_choice_succeeds()
    {
      Assert.Fail();
    }

    [Test]
    public void it_should_follow_decided_path_if_choice_succeeds()
    {
      Assert.Fail();
    }
  }
}