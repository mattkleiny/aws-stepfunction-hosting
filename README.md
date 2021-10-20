# AWS Step Function Hosting

A utility host for executing and integration testing AWS StepFunctions entirely in-process.

## Overview

The core assembly converts the AWS State Machines language into executable machinery, and manages the runtime state and transitions. The component has the following important pieces:

* `StepFunctionDefinition` and `StepDefinition` - Define the JSON-form of an entire state machine and an individual state respectively. Definition and execution are single responsibilities and each `StepDefinition` can be constructed into an equivalent `Step` for runtime evaluation.
* `StepFunctionHost` - The primary facade for executing State Machines. A host can be constructed from it's raw JSON definition and executed through it's entire lifecycle using the `ExecuteAsync` method; cancellation is cooperative across the entire machine and all sub-steps and exceptions are automatically caught and encapsulated into the `ExecutionResult`.
* `StepFunctionData` - An abstraction on top of the [input/output JSON transformation and query process](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-input-output-filtering.html) for Step Function evaluation, which is potentially quite complicated depending on the underlying use case.
* `StepHandler` and `StepHandlerFactory` - The interop mechanism that allows a host to execute arbitrary .NET code in place of a particular AWS resource, for example a Lambda or some other service interaction. A handler is little more than a function that takes `StepFunctionData` as input and returns it as output.

## Impositions

The primary use case for this tool is to allow Step Function execution inside of Integration Tests or a local development environment. In these cases, you'd usually like to interrupt or modify the usual processing rules for a State Machine to allow specific testing scenarios or to increase
the inner loop frequency.

This is mainly achieved through the `Impositions` type, which provide a set of high-level configuration settings that imply changes across the entire machine. For example you can disable or modify the default `Wait` timeout such that a State Machine would execute more frequently, or disable `Task Token`s if they're not important for your scenario.

## Inter-Process Communication

There are some use cases that might require communication between processes, particularly for Task Token support and for direct State Machine execution from another service.

These are implemented through the `InterProcessHost` and `InterProcessClient` types, which provide a small inter-machine IPC bridge that allows one process to notify the other that a task is complete or that one should start.

## Visualizer

A tiny utility Daemon-like application for Windows. It helps to visualize state updates for diagnostics and debugging, which can be sometimes unwieldy inside of a console terminal.

This is just a simple tool that makes the local development experience for the `StepFunctionHost` slightly more user friendly, it implements a simple graph-like canvas that visualizes the State Machine, it's transitions and where the execution is up to or how it succeeded/failed/etc.
