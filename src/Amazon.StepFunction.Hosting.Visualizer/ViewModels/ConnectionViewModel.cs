﻿namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class ConnectionViewModel : ViewModel
  {
    private StepViewModel? source;
    private StepViewModel? target;

    public StepViewModel? Source
    {
      get => source;
      set => SetProperty(ref source, value);
    }

    public StepViewModel? Target
    {
      get => target;
      set => SetProperty(ref target, value);
    }
  }
}