using AutoFixture;
using AutoFixture.AutoNSubstitute;

/// <summary>Common base class for all test cases in the project.</summary>
public abstract class TestCase
{
  protected TestCase()
  {
    Fixture = new Fixture();

// ReSharper disable once VirtualMemberCallInConstructor
    Configure(Fixture);
  }

  /// <summary>The <see cref="IFixture"/> for this test case.</summary>
  protected IFixture Fixture { get; }

  /// <summary>Configures the <see cref="IFixture"/>.</summary>
  protected virtual void Configure(IFixture fixture)
  {
    fixture.Customize(new AutoNSubstituteCustomization());
  }
}